"""Material selection — Theme Profile + Hero + supporting cast (usage history preserved)."""
from __future__ import annotations

from datetime import date, datetime, timedelta
from pathlib import Path
from typing import Any, Dict, List, Optional, Sequence
import random

from . import ROOT, load_json, load_settings, save_json
from .material_pool import ready_items
from .theme_profiles import resolve_theme_profile

# Slot folders under materials/assets/ (template-aligned)
VISUAL_LAYERS = [
    ("background", "Background"),
    ("nature", "Nature"),
    ("plants", "Plants"),
    ("animals", "Hero"),      # legacy key "animals" → Hero slot
    ("fortune", "Accent"),    # legacy key "fortune" → Accent slot
]

BACKGROUND_ASSETS_DIR = ROOT / "materials" / "assets" / "Background"
_BACKGROUND_IMAGE_EXTS = {".png", ".jpg", ".jpeg", ".webp"}


def list_background_asset_files() -> List[Path]:
    """All raster backgrounds under materials/assets/Background/."""
    if not BACKGROUND_ASSETS_DIR.is_dir():
        return []
    out: List[Path] = []
    for p in BACKGROUND_ASSETS_DIR.iterdir():
        if p.is_file() and p.suffix.lower() in _BACKGROUND_IMAGE_EXTS:
            out.append(p)
    return sorted(out, key=lambda x: x.name.lower())


def _background_item_from_file(path: Path) -> Dict[str, Any]:
    rel = path.relative_to(ROOT).as_posix()
    mid = path.stem
    item = {
        "id": mid,
        "category": "Background",
        "name": mid,
        "tags": [],
        "roleHints": ["environment", "background"],
        "file": rel,
        "method": "background_folder_random",
        "role": "environment",
        "depth": "background",
        "visualWeight": 20,
        "anchor": ["center"],
        "scaleRange": [1.0, 1.08],
    }
    return _enrich(item)


def pick_random_background_from_folder(as_of: date, theme: str) -> Dict[str, Any]:
    """Randomly pick one image from materials/assets/Background (seeded by date + theme)."""
    files = list_background_asset_files()
    if not files:
        return _enrich(_placeholder("Background", "background"))

    settings = load_settings()
    rules = settings.get("reuseRules") or {}
    bg_gap = max(2, int(rules.get("sameBackgroundMinDays", 2)))
    blocked_bg = recently_used_backgrounds(as_of, bg_gap)

    candidates = [p for p in files if p.stem not in blocked_bg]
    if not candidates:
        candidates = files

    seed = as_of.toordinal() + sum(ord(c) for c in (theme or ""))
    path = random.Random(seed).choice(candidates)
    bg = _background_item_from_file(path)
    bg.pop("fillColor", None)
    return bg


def _history_path() -> Path:
    settings = load_settings()
    p = Path(settings["paths"]["historyPath"])
    if not p.is_absolute():
        p = ROOT / p
    return p


def load_history() -> Dict[str, Any]:
    path = _history_path()
    if not path.exists():
        return {"version": 1, "entries": []}
    return load_json(path)


def save_history(data: Dict[str, Any]) -> None:
    save_json(_history_path(), data)


def _parse_day(s: str) -> date:
    return datetime.strptime(s, "%Y%m%d").date()


def recently_used_ids(as_of: date, min_days: int) -> set:
    hist = load_history()
    cutoff = as_of - timedelta(days=min_days)
    used = set()
    for e in hist.get("entries", []):
        try:
            d = _parse_day(e["date"])
        except Exception:
            continue
        if cutoff <= d < as_of:
            for mid in e.get("materialIds", []):
                used.add(mid)
    return used


def recently_used_themes(as_of: date, min_days: int) -> set:
    hist = load_history()
    cutoff = as_of - timedelta(days=min_days)
    themes = set()
    for e in hist.get("entries", []):
        try:
            d = _parse_day(e["date"])
        except Exception:
            continue
        if cutoff <= d < as_of:
            t = e.get("mainTheme")
            if t:
                themes.add(t)
    return themes


def recently_used_heroes(as_of: date, min_days: int) -> set:
    hist = load_history()
    cutoff = as_of - timedelta(days=min_days)
    heroes = set()
    for e in hist.get("entries", []):
        try:
            d = _parse_day(e["date"])
        except Exception:
            continue
        if cutoff <= d < as_of:
            h = e.get("heroId")
            if h:
                heroes.add(h)
    return heroes


def recently_used_backgrounds(as_of: date, min_days: int) -> set:
    hist = load_history()
    cutoff = as_of - timedelta(days=min_days)
    out = set()
    for e in hist.get("entries", []):
        try:
            d = _parse_day(e["date"])
        except Exception:
            continue
        if cutoff <= d < as_of:
            b = e.get("backgroundId")
            if b:
                out.add(b)
    return out


def _placeholder(category: str, key: str) -> Dict[str, Any]:
    return {
        "id": f"placeholder_{key}",
        "category": category,
        "name": category,
        "tags": [],
        "roleHints": [],
        "file": None,
        "method": "missing_category_placeholder",
        "role": "support",
        "depth": "midground",
        "visualWeight": 40,
        "anchor": ["center"],
        "scaleRange": [0.85, 1.05],
    }


def _enrich(item: Dict[str, Any]) -> Dict[str, Any]:
    """Fill optional layout metadata without breaking library.json."""
    it = dict(item)
    cat = it.get("category") or ""
    hints = it.get("roleHints") or []
    name = (it.get("name") or "") + " " + " ".join(it.get("tags") or [])

    if "role" not in it:
        if cat == "Hero":
            it["role"] = "hero"
        elif cat == "Background":
            it["role"] = "environment"
        elif cat == "Plants":
            it["role"] = "frame"
        elif cat == "Accent" or cat in ("Fortune", "Career", "Love", "Season"):
            it["role"] = "decorative"
        elif cat == "Nature":
            it["role"] = "environment"
        elif "hero" in hints:
            it["role"] = "hero"
        else:
            it["role"] = "support"

    if "depth" not in it:
        depth_map = {
            "Background": "background",
            "Nature": "environment",
            "Plants": "midground",
            "Hero": "hero",
            "Accent": "foreground",
            # legacy
            "Animals": "hero",
            "Fortune": "foreground",
            "Career": "foreground",
            "Love": "foreground",
            "Season": "midground",
        }
        if it.get("role") == "hero":
            it["depth"] = "hero"
        else:
            it["depth"] = depth_map.get(cat, "midground")

    if "visualWeight" not in it:
        base = {
            "Background": 20, "Nature": 35, "Plants": 45, "Hero": 90, "Accent": 70,
            "Animals": 90, "Fortune": 70, "Career": 75, "Love": 70, "Season": 40,
        }.get(cat, 50)
        if it.get("role") == "hero":
            base = max(base, 88)
        it["visualWeight"] = base

    if "anchor" not in it:
        anchors = _default_anchors(cat, name, it.get("role"))
        it["anchor"] = anchors

    if "scaleRange" not in it:
        if it.get("role") == "hero":
            it["scaleRange"] = [0.85, 1.15]
        elif cat == "Background":
            it["scaleRange"] = [1.0, 1.08]
        elif cat == "Plants":
            it["scaleRange"] = [0.75, 1.05]
        else:
            it["scaleRange"] = [0.8, 1.1]
    return it


def _default_anchors(category: str, name: str, role: Optional[str]) -> List[str]:
    n = name.lower()
    if role == "hero" or category == "Hero":
        return ["hero_center", "hero_left", "hero_right", "center"]
    if category == "Background":
        return ["center"]
    if category == "Plants":
        return ["left_frame", "right_frame", "bottom_left", "bottom_right"]
    if category == "Nature":
        if any(k in n for k in ("太陽", "月亮", "sun", "moon", "星", "雲", "虹", "光")):
            return ["top_left", "top_right", "top_center"]
        if any(k in n for k in ("水", "湖", "海", "波", "waterfall", "river")):
            return ["bottom_center", "bottom_left", "bottom_right"]
        if any(k in n for k in ("山", "mountain", "峰")):
            return ["center", "bottom_center"]
        return ["top_center", "middle_left", "middle_right", "center"]
    if category == "Accent" or category in ("Fortune", "Career", "Love", "Season"):
        return ["hero_left", "hero_right", "bottom_center", "top_right", "top_left"]
    if category == "Animals":
        return ["hero_center", "center", "bottom_center"]
    return ["center"]


def _color_family(item: Optional[Dict[str, Any]]) -> str:
    """Heuristic color family from name/tags (no pixel analysis required)."""
    if not item:
        return "neutral"
    blob = " ".join(
        [str(item.get("name") or ""), *[str(t) for t in (item.get("tags") or [])]]
    ).lower()
    if any(k in blob for k in ("紅", "赤", "绯", "rose", "red", "桃", "牡丹", "愛心")):
        return "red"
    if any(k in blob for k in ("金", "黃", "元寶", "金幣", "gold", "yellow", "銅")):
        return "gold"
    if any(k in blob for k in ("綠", "竹", "蓮", "青", "green", "翠")):
        return "green"
    if any(k in blob for k in ("藍", "水", "湖", "海", "blue", "indigo", "波")):
        return "blue"
    return "neutral"


def _score_item(item: Dict[str, Any], preferred_tags: Sequence[str], seed: int) -> int:
    tags = [str(t) for t in (item.get("tags") or [])]
    blob = " ".join([item.get("name") or "", *tags])
    score = 0
    for i, pref in enumerate(preferred_tags):
        if pref and pref in blob:
            score += 20 - min(i, 10)
    # light deterministic jitter
    score += (seed + sum(ord(c) for c in item.get("id", ""))) % 7
    if "hero" in (item.get("roleHints") or []):
        score += 5
    return score


def _pick(
    categories: Sequence[str],
    seed: int,
    blocked: set,
    preferred_tags: Sequence[str],
    seen: set,
    force_role: Optional[str] = None,
) -> Optional[Dict[str, Any]]:
    pool: List[Dict[str, Any]] = []
    for cat in categories:
        pool.extend(ready_items(category=cat))
    # prefer unused
    candidates = [it for it in pool if it["id"] not in blocked and it["id"] not in seen]
    if not candidates:
        candidates = [it for it in pool if it["id"] not in seen]
    if not candidates:
        candidates = list(pool)
    if not candidates:
        return None
    ranked = sorted(
        candidates,
        key=lambda it: _score_item(it, preferred_tags, seed),
        reverse=True,
    )
    # take from top band with seed
    band = ranked[: max(3, min(8, len(ranked)))]
    chosen = band[seed % len(band)]
    out = _enrich(chosen)
    if force_role:
        out["role"] = force_role
        if force_role == "hero":
            out["depth"] = "hero"
            out["visualWeight"] = max(int(out.get("visualWeight") or 50), 88)
    return out


def pick_materials(as_of: date, theme: str) -> Dict[str, Any]:
    """Daily cast: one random Background from materials/assets/Background/."""
    profile = resolve_theme_profile(theme)
    background = pick_random_background_from_folder(as_of, theme)

    return {
        "themeProfile": {**profile, "template": "bg_only"},
        "template": "bg_only",
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
        "layerOrder": [k for k, _ in VISUAL_LAYERS],
        "animals": None,
        "fortune": None,
        "supports": [],
        "environment": None,
        "auspicious": None,
        "allIds": [background["id"]],
        "colorNote": {"heroFamily": None},
        "backgroundSource": "materials/assets/Background",
    }


def record_usage(
    yyyymmdd: str,
    main_theme: str,
    hero_id: str,
    material_ids: Sequence[str],
    background_id: Optional[str] = None,
) -> None:
    hist = load_history()
    entries = [e for e in hist.get("entries", []) if e.get("date") != yyyymmdd]
    entry = {
        "date": yyyymmdd,
        "mainTheme": main_theme,
        "heroId": hero_id,
        "materialIds": list(material_ids),
        "recordedAt": datetime.now().isoformat(timespec="seconds"),
    }
    if background_id:
        entry["backgroundId"] = background_id
    entries.append(entry)
    hist["entries"] = sorted(entries, key=lambda x: x["date"])
    save_history(hist)
