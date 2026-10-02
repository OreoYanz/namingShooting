"""Theme background generation: theme → one full paper-cut Background → library.

Daily flow generates ONLY a Background image (saved under materials/assets/Background/).
Legacy slot prompts (Plants/Hero/Accent) remain for manual pool tools.
"""
from __future__ import annotations

import base64
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

from . import ROOT, load_settings, load_style_bible, save_json
from .material_pool import (
    _LIBRARY_LOCK,
    _openai_client,
    category_color,
    discover_categories,
    ensure_category_folder,
    load_library,
    next_id,
    save_library,
)
from .theme_profiles import resolve_theme_profile

THEME_SLOTS: List[Tuple[str, str, str]] = [
    ("Background", "environment", "背景"),
]

_SUBJECT_BANKS: Dict[str, Dict[str, List[Dict[str, Any]]]] = {
    "財運": {
        "Background": [
            {"name": "金色湖畔晨光", "en": "golden lakeside morning mist with koi and soft gold paper layers", "tags": ["財運", "水", "金", "晨光"]},
            {"name": "錦鯉蓮池全景", "en": "koi lotus pond panorama with warm gold accents", "tags": ["財運", "水", "蓮"]},
            {"name": "秋日豐收田野", "en": "autumn harvest field soft hills with rice and gold light", "tags": ["財運", "豐收", "秋"]},
        ],
    },
    "事業": {
        "Background": [
            {"name": "高山日出全景", "en": "mountain peaks at sunrise with rising paper clouds", "tags": ["事業", "山", "上升"]},
            {"name": "雲海階梯遠景", "en": "stairway into cloud sea panorama with hopeful light", "tags": ["事業", "雲", "階梯"]},
        ],
    },
    "愛情": {
        "Background": [
            {"name": "月下花園全景", "en": "moonlit peach blossom garden panorama", "tags": ["愛情", "月", "桃花"]},
            {"name": "柔粉雲海全景", "en": "soft pink cloudscape garden panorama", "tags": ["愛情", "雲", "粉"]},
        ],
    },
    "綜合": {
        "Background": [
            {"name": "柔和四季庭院", "en": "soft four-season courtyard panorama", "tags": ["綜合", "庭院"]},
        ],
    },
}


def _style_core() -> str:
    style = load_style_bible()
    return (
        f"Art style: {style.get('styleZh') or style.get('style')}. "
        f"Material: layered colored paper with visible fiber texture, soft studio lighting, "
        f"soft layered drop shadows, contemporary colorful Oriental paper-cut sculpture "
        f"(NOT traditional red lattice window cutouts). "
        f"No black thick outlines, no readable text, no letters, no watermark, "
        f"no photorealistic human face. "
        f"{style.get('promptSuffix', '')}"
    )


def build_slot_prompt(
    *,
    theme_label: str,
    profile_key: str,
    category: str,
    role: str,
    subject: Dict[str, Any],
    palette_hint: str,
) -> str:
    name = subject["name"]
    en = subject.get("en") or name
    core = _style_core()
    shared = (
        f"Theme: 「{theme_label}」 ({profile_key}). "
        f"Color mood: {palette_hint}. "
    )
    if category == "Background" or category not in ("Plants", "Hero", "Accent", "Nature"):
        return (
            f"{shared}"
            f"Create ONE complete FULL-BLEED paper-cut SCENE as a single background image: "
            f"「{name}」 ({en}). "
            f"The image itself is the finished daily scene — include atmosphere, soft depth, "
            f"and thematic motifs that match the theme. Fill the entire square canvas edge-to-edge. "
            f"Do NOT leave empty margins for overlays. Opaque paper background OK. "
            f"No separate cutout layers — everything is painted into this one scene. "
            f"{core}"
        )
    if category == "Plants":
        return (
            f"{shared}"
            f"Create FRAME / BORDER plant cutouts ONLY: 「{name}」 ({en}). "
            f"TRUE TRANSPARENT BACKGROUND. {core}"
        )
    if category == "Hero":
        return (
            f"{shared}"
            f"Create the HERO subject ONLY: 「{name}」 ({en}). "
            f"TRUE TRANSPARENT BACKGROUND. {core}"
        )
    return (
        f"{shared}"
        f"Create particle overlay: 「{name}」 ({en}). TRUE TRANSPARENT BACKGROUND. {core}"
    )


def _palette_for_profile(profile_key: str) -> str:
    return {
        "財運": "warm gold, soft teal water, cream paper, limited crimson accents",
        "事業": "sunrise gold, mountain teal, cream, restrained orange accents",
        "愛情": "soft peach pink, cream, muted teal, gentle gold highlights",
        "綜合": "cream paper, soft teal, warm gold accents",
    }.get(profile_key, "cream paper, soft teal, warm gold accents")


def plan_theme_set(theme: str, seed: int = 0) -> Dict[str, Any]:
    """Plan a single Background generation for the daily theme."""
    profile = resolve_theme_profile(theme)
    key = profile.get("profileKey") or "綜合"
    bank = _SUBJECT_BANKS.get(key) or _SUBJECT_BANKS["綜合"]
    palette = _palette_for_profile(key)
    set_id = f"bg_{key}_{datetime.now().strftime('%Y%m%d_%H%M%S')}_{seed % 1000:03d}"
    options = bank.get("Background") or _SUBJECT_BANKS["綜合"]["Background"]
    subject = options[seed % len(options)]
    # Prefer embedding the actual daily theme wording into the subject title
    display_name = f"{theme}・{subject['name']}" if theme and theme not in subject["name"] else subject["name"]
    subject_use = {
        **subject,
        "name": display_name,
        "en": f"{theme}: {subject.get('en') or subject['name']}",
        "tags": list(subject.get("tags") or []) + [key, "daily_bg", set_id, theme],
    }
    slot = {
        "category": "Background",
        "role": "environment",
        "labelZh": "背景",
        "name": subject_use["name"],
        "en": subject_use.get("en"),
        "tags": subject_use["tags"],
        "prompt": build_slot_prompt(
            theme_label=theme or profile.get("label") or display_name,
            profile_key=key,
            category="Background",
            role="environment",
            subject=subject_use,
            palette_hint=palette,
        ),
    }
    return {
        "setId": set_id,
        "theme": theme,
        "profileKey": key,
        "profileLabel": profile.get("label") or theme,
        "template": "bg_only",
        "style": load_style_bible().get("styleZh") or load_style_bible().get("style"),
        "palette": palette,
        "slots": [slot],
    }


def _write_image_bytes(
    abs_path: Path,
    client,
    settings,
    prompt: str,
    *,
    transparent: bool = False,
) -> str:
    model = settings["openai"].get("imageModel") or "gpt-image-1"
    kwargs: Dict[str, Any] = {
        "model": model,
        "prompt": prompt,
        "size": "1024x1024",
        "n": 1,
    }
    if transparent:
        kwargs["background"] = "transparent"
        kwargs["output_format"] = "png"
    try:
        result = client.images.generate(**kwargs)
    except TypeError:
        kwargs.pop("background", None)
        kwargs.pop("output_format", None)
        result = client.images.generate(**kwargs)
    except Exception:
        if transparent and ("background" in kwargs or "output_format" in kwargs):
            kwargs.pop("background", None)
            kwargs.pop("output_format", None)
            result = client.images.generate(**kwargs)
        else:
            raise
    data0 = result.data[0]
    b64 = getattr(data0, "b64_json", None)
    if not b64 and getattr(data0, "url", None):
        import requests

        r = requests.get(data0.url, timeout=180)
        r.raise_for_status()
        abs_path.write_bytes(r.content)
    else:
        abs_path.write_bytes(base64.b64decode(b64))
    return f"openai:{model}{':alpha' if transparent else ''}"


def generate_slot_item(slot: Dict[str, Any], *, set_meta: Dict[str, Any], use_placeholder: bool = False) -> Dict[str, Any]:
    category = slot["category"]
    name = slot["name"]
    tags = list(slot.get("tags") or [])
    role_hints = ["environment", "background"] if category == "Background" else ["support"]

    ensure_category_folder("Background" if category == "Background" else category)
    save_category = "Background" if category == "Background" else category

    with _LIBRARY_LOCK:
        lib = load_library()
        existing = {x["id"] for x in lib.get("items", [])}
        mid = next_id(save_category, name, existing)
        rel = f"materials/assets/{save_category}/{mid}.png"
        abs_path = ROOT / rel
        abs_path.parent.mkdir(parents=True, exist_ok=True)
        if not abs_path.exists():
            abs_path.write_bytes(b"")

    settings = load_settings()
    prompt = slot.get("prompt") or ""
    if use_placeholder or not settings["openai"].get("enableImageGeneration"):
        from .ai_image import _pillow_placeholder

        _pillow_placeholder(abs_path, name, category_color(save_category))
        method = "placeholder"
    else:
        try:
            client, settings = _openai_client()
            method = _write_image_bytes(
                abs_path, client, settings, prompt, transparent=False
            )
        except Exception as e:
            from .ai_image import _pillow_placeholder

            _pillow_placeholder(abs_path, name, category_color(save_category))
            method = f"placeholder_fallback:{e}"

    item: Dict[str, Any] = {
        "id": mid,
        "category": save_category,
        "name": name,
        "tags": tags,
        "roleHints": role_hints,
        "file": rel.replace("\\", "/"),
        "createdAt": datetime.now().isoformat(timespec="seconds"),
        "method": method,
        "slot": "Background",
        "themeSetId": set_meta.get("setId"),
        "themeProfile": set_meta.get("profileKey"),
        "theme": set_meta.get("theme"),
        "template": "bg_only",
        "style": set_meta.get("style"),
        "prompt": prompt,
        "role": "environment",
        "depth": "background",
        "visualWeight": 20,
    }

    with _LIBRARY_LOCK:
        lib = load_library()
        if not any(x.get("id") == mid for x in lib.get("items", [])):
            lib.setdefault("items", []).append(item)
        lib["categories"] = discover_categories() or ["Accent", "Background", "Hero", "Nature", "Plants"]
        save_library(lib)
    return item


def generate_theme_set(
    theme: str,
    *,
    seed: int = 0,
    use_placeholder: bool = False,
    workers: int = 1,
    save_dir: Optional[Path] = None,
    force_hero_index: Optional[int] = None,
) -> Dict[str, Any]:
    """Generate one thematic paper-cut Background (Background folder only)."""
    plan = plan_theme_set(theme, seed=seed)
    slot = plan["slots"][0]
    errors: List[str] = []
    try:
        background = generate_slot_item(slot, set_meta=plan, use_placeholder=use_placeholder)
    except Exception as e:
        errors.append(f"Background:{e}")
        raise

    materials = {
        "themeProfile": {
            "profileKey": plan["profileKey"],
            "label": plan["profileLabel"],
            "template": "bg_only",
            "sourceTheme": theme,
        },
        "template": "bg_only",
        "themeSetId": plan["setId"],
        "style": plan["style"],
        "palette": plan["palette"],
        "hero": None,
        "background": background,
        "nature": None,
        "plants": [],
        "accent": None,
        "accents": [],
        "season": None,
        "layers": {
            "background": background,
            "nature": None,
            "plants": None,
            "animals": None,
            "fortune": None,
        },
        "animals": None,
        "fortune": None,
        "allIds": [background["id"]],
        "generation": {
            "mode": "theme_background_only",
            "setId": plan["setId"],
            "errors": errors,
            "slots": plan["slots"],
            "items": {
                "Background": {
                    "id": background["id"],
                    "file": background["file"],
                    "method": background.get("method"),
                }
            },
        },
    }
    out_dir = save_dir or (ROOT / "preview" / "theme_sets" / plan["setId"])
    out_dir.mkdir(parents=True, exist_ok=True)
    save_json(out_dir / "theme_set.json", {"plan": plan, "materials": materials})
    (out_dir / "prompts.txt").write_text(
        f"## 背景 / Background — {slot['name']}\n{slot['prompt']}",
        encoding="utf-8",
    )
    materials["setDir"] = str(out_dir)
    return materials


def materials_to_cutout_map(materials: Dict[str, Any]) -> Dict[str, str]:
    by_id: Dict[str, str] = {}
    for key in ("background", "hero", "accent", "nature"):
        it = materials.get(key) or {}
        if isinstance(it, dict) and it.get("id") and it.get("file"):
            by_id[it["id"]] = str(ROOT / it["file"])
    for p in materials.get("plants") or []:
        if p and p.get("id") and p.get("file"):
            by_id[p["id"]] = str(ROOT / p["file"])
    for a in materials.get("accents") or []:
        if a and a.get("id") and a.get("file"):
            by_id[a["id"]] = str(ROOT / a["file"])
    return by_id


def compose_theme_set_preview(
    materials: Dict[str, Any],
    out_path: Path,
    size: Tuple[int, int] = (1080, 1080),
) -> Path:
    from .scene import compose_scene

    cutouts = []
    bg = materials.get("background") or {}
    if bg.get("id") and bg.get("file"):
        cutouts.append({"id": bg["id"], "cutout": str(ROOT / bg["file"]), "layer": "background"})
    compose_scene(cutouts, out_path, size=size, materials=materials)
    return out_path
