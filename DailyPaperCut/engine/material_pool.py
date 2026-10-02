"""Material pool: generate once into materials/assets, reuse for daily packs.

Categories are discovered dynamically from subfolders under materials/assets/.
Adding or removing a folder updates menus/filters on the next request.
"""
from __future__ import annotations

import base64
import os
import re
import threading
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List, Optional

from . import ROOT, load_json, load_settings, load_style_bible, save_json


# Soft defaults only used when assets/ has no folders yet (first boot).
_FALLBACK_CATEGORIES = [
    "Accent", "Background", "Hero", "Nature", "Plants",
]

_CATEGORY_COLORS = {
    "Hero": (90, 150, 200),
    "Plants": (120, 170, 110),
    "Accent": (210, 170, 70),
    "Nature": (160, 180, 190),
    "Background": (200, 190, 175),
}

_LIBRARY_LOCK = threading.Lock()


def library_path() -> Path:
    return ROOT / "materials" / "library.json"


def assets_root() -> Path:
    return ROOT / "materials" / "assets"


def discover_categories() -> List[str]:
    """Source of truth: immediate child folders under materials/assets/."""
    root = assets_root()
    if not root.exists():
        return []
    return sorted(
        p.name
        for p in root.iterdir()
        if p.is_dir() and not p.name.startswith((".", "_"))
    )


def get_categories(*, sync_library: bool = False) -> List[str]:
    cats = discover_categories()
    if not cats:
        cats = list(_FALLBACK_CATEGORIES)
    if sync_library:
        with _LIBRARY_LOCK:
            lib = load_library()
            if lib.get("categories") != cats:
                lib["categories"] = cats
                save_library(lib)
    return cats


# Backward-compatible name: callers should prefer get_categories().
CATEGORIES = _FALLBACK_CATEGORIES


def category_color(category: str) -> tuple:
    return _CATEGORY_COLORS.get(category, (150, 150, 150))


def load_library() -> Dict[str, Any]:
    path = library_path()
    if not path.exists():
        return {"version": 1, "categories": get_categories(), "items": []}
    return load_json(path)


def save_library(data: Dict[str, Any]) -> None:
    save_json(library_path(), data)


def slugify(text: str) -> str:
    s = re.sub(r"[^a-zA-Z0-9\u4e00-\u9fff]+", "_", (text or "").strip())
    s = s.strip("_").lower()
    return s or "item"


def _id_prefix(category: str) -> str:
    special = {
        "Hero": "hero",
        "Plants": "plant",
        "Accent": "accent",
        "Nature": "nature",
        "Background": "bg",
        # legacy aliases
        "Animals": "animal",
        "Fortune": "fortune",
    }
    if category in special:
        return special[category]
    return re.sub(r"[^a-z0-9]+", "", category.lower()) or "item"


def next_id(category: str, name: str, existing_ids: set) -> str:
    prefix = _id_prefix(category)
    i = 1
    while True:
        cand = f"{prefix}_{slugify(name) or 'item'}_{i:03d}"
        if cand not in existing_ids:
            return cand
        i += 1


def list_pool(category: Optional[str] = None) -> List[Dict[str, Any]]:
    known = set(discover_categories()) or set(_FALLBACK_CATEGORIES)
    items = load_library().get("items", [])
    items = [x for x in items if x.get("category") in known]
    if category:
        if category not in known:
            return []
        items = [x for x in items if x.get("category") == category]
    return items


def ready_items(role: Optional[str] = None, category: Optional[str] = None) -> List[Dict[str, Any]]:
    """Items that already have a file on disk — usable without API."""
    known = set(discover_categories()) or set(_FALLBACK_CATEGORIES)
    out = []
    for it in load_library().get("items", []):
        if it.get("disabled") or it.get("ready") is False:
            continue
        if it.get("category") not in known:
            continue
        if category and it.get("category") != category:
            continue
        f = it.get("file")
        if not f:
            continue
        # skip parked / unused assets
        norm = str(f).replace("\\", "/")
        if "/_unused/" in norm or "/_disabled/" in norm:
            continue
        p = ROOT / f if not Path(f).is_absolute() else Path(f)
        if not p.exists():
            continue
        # Prefer files that still live under assets/<Category>/
        try:
            p.resolve().relative_to(assets_root().resolve())
        except ValueError:
            continue
        if role and role not in (it.get("roleHints") or []):
            continue
        out.append(it)
    return out


def sync_library_categories() -> Dict[str, Any]:
    """Rewrite library.json categories to match asset folders."""
    cats = get_categories(sync_library=True)
    return {"categories": cats, "count": len(cats)}


def sync_assets_into_library() -> Dict[str, Any]:
    """Register PNG files under assets/<Category>/ that are missing from library.json."""
    cats = discover_categories() or list(_FALLBACK_CATEGORIES)
    added = []
    with _LIBRARY_LOCK:
        lib = load_library()
        by_file = { (it.get("file") or "").replace("\\", "/"): it for it in lib.get("items", []) }
        existing_ids = {it["id"] for it in lib.get("items", [])}
        for cat in cats:
            folder = assets_root() / cat
            if not folder.exists():
                continue
            for png in sorted(folder.glob("*.png")):
                rel = f"materials/assets/{cat}/{png.name}".replace("\\", "/")
                if rel in by_file:
                    continue
                stem = png.stem
                mid = stem if stem not in existing_ids else next_id(cat, stem, existing_ids)
                existing_ids.add(mid)
                item = {
                    "id": mid,
                    "category": cat,
                    "name": stem,
                    "tags": [],
                    "roleHints": [],
                    "file": rel,
                    "createdAt": datetime.now().isoformat(timespec="seconds"),
                    "method": "synced_from_assets",
                }
                lib.setdefault("items", []).append(item)
                by_file[rel] = item
                added.append(mid)
        lib["categories"] = cats
        save_library(lib)
    return {"categories": cats, "added": added, "addedCount": len(added)}


def ensure_category_folder(category: str) -> Path:
    """Validate category name and ensure its asset folder exists."""
    cat = (category or "").strip()
    if not cat or any(ch in cat for ch in ("/", "\\", "..")):
        raise ValueError("類別名稱不合法")
    path = assets_root() / cat
    path.mkdir(parents=True, exist_ok=True)
    return path


def _openai_client():
    try:
        from dotenv import load_dotenv
        load_dotenv(ROOT / ".env")
    except ImportError:
        pass
    settings = load_settings()
    key = os.environ.get(settings["openai"]["apiKeyEnv"], "").strip()
    if not key:
        raise RuntimeError(
            "未設定 OPENAI_API_KEY，無法生成素材圖像。"
            "請確認 DailyPaperCut/.env 存在且已填入金鑰，並重新啟動工作台。"
        )
    from openai import OpenAI
    base = os.environ.get("OPENAI_BASE_URL") or settings["openai"].get("baseUrl")
    return OpenAI(api_key=key, base_url=base), settings


def build_image_prompt(name: str, category: str, tags: List[str]) -> str:
    """Legacy single-item prompt. Prefer theme_set_generator.build_slot_prompt for daily sets."""
    from .theme_set_generator import build_slot_prompt

    style = load_style_bible()
    profile_guess = "綜合"
    for k in ("財運", "事業", "愛情"):
        if any(k in str(t) for t in (tags or [])) or k in (name or ""):
            profile_guess = k
            break
    subject = {"name": name, "en": name, "tags": tags or []}
    role = {
        "Background": "environment",
        "Plants": "frame",
        "Nature": "environment",
        "Hero": "hero",
        "Accent": "decorative",
    }.get(category, "support")
    return build_slot_prompt(
        theme_label=name,
        profile_key=profile_guess,
        category=category if category in ("Background", "Plants", "Nature", "Hero", "Accent") else "Accent",
        role=role,
        subject=subject,
        palette_hint=style.get("color") or "cream paper, soft teal, warm gold",
    )


def generate_pool_item(
    category: str,
    name: str,
    tags: Optional[List[str]] = None,
    role_hints: Optional[List[str]] = None,
    use_placeholder: bool = False,
) -> Dict[str, Any]:
    """Generate one pool item. Safe for parallel calls (library writes are locked)."""
    tags = tags or []
    role_hints = role_hints or ["support"]
    ensure_category_folder(category)

    # Reserve unique id quickly under lock, then do slow image I/O outside.
    with _LIBRARY_LOCK:
        lib = load_library()
        existing = {x["id"] for x in lib.get("items", [])}
        mid = next_id(category, name, existing)
        existing.add(mid)
        cat_dir = assets_root() / category
        cat_dir.mkdir(parents=True, exist_ok=True)
        rel = f"materials/assets/{category}/{mid}.png"
        abs_path = ROOT / rel
        if not abs_path.exists():
            abs_path.write_bytes(b"")

    settings = load_settings()
    if use_placeholder or not settings["openai"].get("enableImageGeneration"):
        from .ai_image import _pillow_placeholder
        _pillow_placeholder(abs_path, name, category_color(category))
        method = "placeholder"
    else:
        client, settings = _openai_client()
        model = settings["openai"].get("imageModel") or "gpt-image-1"
        prompt = build_image_prompt(name, category, tags)
        try:
            result = client.images.generate(
                model=model,
                prompt=prompt,
                size="1024x1024",
                n=1,
                **(
                    {"background": "transparent", "output_format": "png"}
                    if category in ("Plants", "Hero", "Accent", "Nature")
                    else {}
                ),
            )
            b64 = result.data[0].b64_json
            if not b64 and getattr(result.data[0], "url", None):
                import requests
                r = requests.get(result.data[0].url, timeout=120)
                r.raise_for_status()
                abs_path.write_bytes(r.content)
            else:
                abs_path.write_bytes(base64.b64decode(b64))
            method = f"openai:{model}"
        except TypeError:
            try:
                result = client.images.generate(
                    model=model,
                    prompt=prompt,
                    size="1024x1024",
                    n=1,
                )
                b64 = result.data[0].b64_json
                if not b64 and getattr(result.data[0], "url", None):
                    import requests
                    r = requests.get(result.data[0].url, timeout=120)
                    r.raise_for_status()
                    abs_path.write_bytes(r.content)
                else:
                    abs_path.write_bytes(base64.b64decode(b64))
                method = f"openai:{model}"
            except Exception as e:
                from .ai_image import _pillow_placeholder
                _pillow_placeholder(abs_path, name, category_color(category))
                method = f"placeholder_fallback:{e}"
        except Exception as e:
            from .ai_image import _pillow_placeholder
            _pillow_placeholder(abs_path, name, category_color(category))
            method = f"placeholder_fallback:{e}"

    item = {
        "id": mid,
        "category": category,
        "name": name,
        "tags": tags,
        "roleHints": role_hints,
        "file": rel.replace("\\", "/"),
        "createdAt": datetime.now().isoformat(timespec="seconds"),
        "method": method,
    }
    with _LIBRARY_LOCK:
        lib = load_library()
        if not any(x.get("id") == mid for x in lib.get("items", [])):
            lib.setdefault("items", []).append(item)
        lib["categories"] = discover_categories() or list(_FALLBACK_CATEGORIES)
        save_library(lib)
    return item


def copy_to_day(cutout_dir: Path, item: Dict[str, Any]) -> Path:
    cutout_dir.mkdir(parents=True, exist_ok=True)
    src = ROOT / item["file"]
    dst = cutout_dir / f"{item['id']}.png"
    if src.exists():
        dst.write_bytes(src.read_bytes())
    return dst
