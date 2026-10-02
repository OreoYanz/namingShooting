"""Daily cutouts: copy pool assets for Scene Engine cast (compat with legacy layers)."""
from __future__ import annotations

from pathlib import Path
from typing import Any, Dict, List, Optional

from . import ROOT, load_style_bible
from .ai_image import _pillow_placeholder
from .material_pool import category_color, copy_to_day
from .materials import VISUAL_LAYERS


def _copy_one(
    item: Optional[Dict[str, Any]],
    cutout_dir: Path,
    source_dir: Path,
    *,
    layer_key: str,
    style: Dict[str, Any],
    seen: set,
) -> Optional[Dict[str, str]]:
    if not item or not item.get("id"):
        return None
    mid = item["id"]
    if mid in seen:
        return None
    seen.add(mid)
    category = item.get("category") or layer_key
    out = cutout_dir / f"{mid}.png"
    src_meta = source_dir / f"{mid}_from_pool.txt"
    pool_file = item.get("file")
    pool_path = (ROOT / pool_file) if pool_file else None
    if pool_path and pool_path.exists():
        copy_to_day(cutout_dir, item)
        src_meta.write_text(
            f"copied_from={pool_file}\nlayer={layer_key}\nrole={item.get('role')}\n",
            encoding="utf-8",
        )
    elif not out.exists():
        _pillow_placeholder(out, item.get("name", mid), category_color(category))
        src_meta.write_text(f"fallback=placeholder\nlayer={layer_key}\n", encoding="utf-8")
    return {
        "id": mid,
        "layer": layer_key,
        "role": item.get("role") or layer_key,
        "category": category,
        "name": item.get("name", mid),
        "cutout": str(out),
        "source": str(pool_path or src_meta),
        "style": style.get("style"),
    }


def ensure_cutouts(
    materials: Dict[str, Any],
    cutout_dir: Path,
    source_dir: Path,
) -> List[Dict[str, str]]:
    style = load_style_bible()
    pack: List[Dict[str, str]] = []
    seen: set = set()

    source_dir.mkdir(parents=True, exist_ok=True)
    cutout_dir.mkdir(parents=True, exist_ok=True)

    # New Scene Engine cast order
    cast = [
        ("background", materials.get("background")),
        ("nature", materials.get("nature")),
        ("hero", materials.get("hero") or materials.get("animals")),
        ("accent", materials.get("accent") or materials.get("fortune")),
        ("season", materials.get("season")),
    ]
    plants = materials.get("plants") or []
    if isinstance(plants, dict):
        plants = [plants]
    for i, p in enumerate(plants):
        cast.append((f"plants_{i}", p))

    for layer_key, item in cast:
        row = _copy_one(item, cutout_dir, source_dir, layer_key=layer_key, style=style, seen=seen)
        if row:
            pack.append(row)

    # Legacy VISUAL_LAYERS fill (if any missing from layers map)
    layers = materials.get("layers") or {}
    for layer_key, category in VISUAL_LAYERS:
        item = layers.get(layer_key) or materials.get(layer_key)
        if isinstance(item, list):
            item = item[0] if item else None
        row = _copy_one(item, cutout_dir, source_dir, layer_key=layer_key, style=style, seen=seen)
        if row:
            # ensure category from layer table when missing
            row["category"] = row.get("category") or category
            pack.append(row)

    return pack
