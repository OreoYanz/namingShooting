"""Scene Engine — SceneElement, anchors, depth, templates, layout, static compose."""
from __future__ import annotations

import json
from dataclasses import asdict, dataclass, field
from pathlib import Path
from typing import Any, Dict, List, Optional, Sequence, Tuple

from PIL import Image

from . import ROOT


ANCHORS: Dict[str, Tuple[float, float]] = {
    # normalized center points on 1080 canvas
    "top_left": (0.18, 0.16),
    "top_center": (0.50, 0.14),
    "top_right": (0.82, 0.16),
    "middle_left": (0.20, 0.48),
    "center": (0.50, 0.50),
    "middle_right": (0.80, 0.48),
    "bottom_left": (0.22, 0.82),
    "bottom_center": (0.50, 0.84),
    "bottom_right": (0.78, 0.82),
    "left_frame": (0.16, 0.55),
    "right_frame": (0.84, 0.55),
    "hero_center": (0.50, 0.58),
    "hero_left": (0.38, 0.60),
    "hero_right": (0.62, 0.60),
    "back_center": (0.50, 0.62),
}

DEPTH_ORDER = {
    "background": 0,
    "environment": 1,
    "midground": 2,
    "hero": 3,
    "foreground": 4,
    "text": 5,
}

DEPTH_STYLE = {
    "background": {"shadow_opacity": 0.08, "shadow_offset": (4, 6), "motion": 1.5, "base_scale": 1.05},
    "environment": {"shadow_opacity": 0.12, "shadow_offset": (6, 8), "motion": 2.5, "base_scale": 0.95},
    "midground": {"shadow_opacity": 0.14, "shadow_offset": (8, 10), "motion": 3.5, "base_scale": 0.88},
    "hero": {"shadow_opacity": 0.22, "shadow_offset": (12, 16), "motion": 6.0, "base_scale": 1.0},
    "foreground": {"shadow_opacity": 0.28, "shadow_offset": (10, 14), "motion": 8.0, "base_scale": 0.82},
    "text": {"shadow_opacity": 0.18, "shadow_offset": (8, 10), "motion": 0.0, "base_scale": 1.0},
}

# Shared 4-slot layout: Background + Plants×3 + Hero edge + full-bleed particle Accent overlay
# No Nature / 前景
_SLOT_LAYOUT = {
    "hero_anchors": ["hero_right", "hero_left"],
    "hero_edge_x": {"hero_right": 0.78, "hero_left": 0.22},
    "nature_anchors": [],
    "plant_slots": [
        {"anchor": "bottom_left", "size": "large"},
        {"anchor": "bottom_center", "size": "small"},
        {"anchor": "bottom_right", "size": "medium"},
    ],
    "accent_anchors": ["center"],
    "accent_count_range": [1, 1],
    "accent_scatter": False,  # one full-canvas particle PNG, not stamped clumps
    "accent_full_bleed": True,
    "accent_scatter_count": 1,
    "accent_scatter_scale_range": [1.0, 1.0],
    "plant_max_height_ratio": 0.28,
    "plant_size_scale": {"large": 1.0, "medium": 0.72, "small": 0.48},
    "accent_scale": 1.0,
    "nature_pref": [],
    "plant_pref": ["bottom_left", "bottom_center", "bottom_right"],
    "accent_pref": ["center"],
    "season_pref": [],
}


def _template(hero_anchor: str = "hero_right", hero_y_bias: float = 40) -> Dict[str, Any]:
    cfg = dict(_SLOT_LAYOUT)
    cfg["hero_anchor"] = hero_anchor
    cfg["hero_y_bias"] = hero_y_bias
    return cfg


TEMPLATES = {
    "fortune_scene": _template("hero_right", 40),
    "career_scene": _template("hero_right", 20),
    "love_scene": _template("hero_left", 50),
    "balanced_scene": _template("hero_right", 30),
    "bg_only": {
        **_template("hero_right", 30),
        "plant_slots": [],
        "accent_full_bleed": False,
        "accent_scatter": False,
        "bg_only": True,
    },
}



@dataclass
class SceneElement:
    asset_id: str
    category: str
    role: str
    cutout: str
    name: str = ""
    x: float = 540
    y: float = 540
    scale: float = 1.0
    rotation: float = 0.0
    depth: str = "midground"
    visual_weight: int = 50
    animation_type: str = "slow_reveal"
    start_time: float = 0.0
    duration: float = 2.0
    anchor: str = "center"
    base_w: int = 400
    base_h: int = 400
    tags: List[str] = field(default_factory=list)

    @property
    def depth_index(self) -> int:
        return DEPTH_ORDER.get(self.depth, 2)

    def to_dict(self) -> Dict[str, Any]:
        return asdict(self)


def _anchor_xy(anchor: str, size: Tuple[int, int], y_bias: float = 0) -> Tuple[float, float]:
    nx, ny = ANCHORS.get(anchor, ANCHORS["center"])
    return nx * size[0], ny * size[1] + y_bias


def _choose_anchor(preferred: Sequence[str], allowed: Sequence[str], used: set, seed: int) -> str:
    for a in preferred:
        if a in allowed and a not in used:
            used.add(a)
            return a
    for a in allowed:
        if a not in used:
            used.add(a)
            return a
    if preferred:
        return preferred[seed % len(preferred)]
    return allowed[0] if allowed else "center"


def _anim_for(category: str, role: str, name: str) -> str:
    n = (name or "").lower()
    if role == "hero" or category == "Hero":
        return "hero_reveal"
    if category == "Background":
        return "slow_reveal"
    if category == "Plants":
        if "left" in n:
            return "slide_left"
        return "rise"
    if category == "Nature":
        if any(k in n for k in ("雲", "cloud")):
            return "slide_right"
        if any(k in n for k in ("水", "波", "湖", "海")):
            return "parallax"
        if any(k in n for k in ("星", "光")):
            return "float"
        return "slow_reveal"
    if category == "Accent":
        return "particle_twinkle"
    if category == "Fortune":
        return "particle_gather"
    if category == "Career":
        return "rise"
    if category == "Love":
        return "gentle_sway"
    if category == "Season":
        return "float"
    return "slow_reveal"


def _base_size_for(role: str, depth: str, canvas: Tuple[int, int]) -> Tuple[int, int]:
    w = canvas[0]
    if depth == "background" or (role == "environment" and depth == "background"):
        return int(w * 1.02), int(w * 1.02)
    if role == "hero":
        return int(w * 0.52), int(w * 0.52)
    if role == "frame":
        return int(w * 0.38), int(w * 0.55)
    if role == "decorative":
        return int(w * 0.28), int(w * 0.28)
    if depth == "environment":
        return int(w * 0.42), int(w * 0.42)
    return int(w * 0.34), int(w * 0.34)


WARM_FILLS = [
    (255, 214, 170, 255),  # warm peach
    (255, 196, 140, 255),  # soft apricot
    (248, 210, 120, 255),  # golden
    (255, 180, 140, 255),  # coral cream
    (236, 188, 120, 255),  # amber
]


def _make_element(
    item: Dict[str, Any],
    *,
    cutout: str,
    anchor: str,
    canvas_size: Tuple[int, int],
    role: str,
    depth: str,
    start: float,
    duration: float,
    scale: float,
    base_w: int,
    base_h: int,
    y_bias: float = 0,
    rotation: float = 0,
    visual_weight: int = 50,
) -> SceneElement:
    x, y = _anchor_xy(anchor, canvas_size, y_bias=y_bias)
    return SceneElement(
        asset_id=item.get("id") or f"el_{anchor}",
        category=item.get("category") or "",
        role=role,
        cutout=cutout,
        name=item.get("name") or item.get("id") or "",
        x=x,
        y=y,
        scale=scale,
        rotation=rotation,
        depth=depth,
        visual_weight=visual_weight,
        animation_type=_anim_for(item.get("category") or "", role, item.get("name") or ""),
        start_time=start,
        duration=duration,
        anchor=anchor,
        base_w=base_w,
        base_h=base_h,
        tags=list(item.get("tags") or []),
    )


def build_fortune_scene_elements(
    materials: Dict[str, Any],
    cutouts_by_id: Dict[str, str],
    canvas_size: Tuple[int, int] = (1080, 1080),
) -> List[SceneElement]:
    """Backward-compatible alias."""
    return build_slot_scene_elements(materials, cutouts_by_id, canvas_size, "fortune_scene")


def build_slot_scene_elements(
    materials: Dict[str, Any],
    cutouts_by_id: Dict[str, str],
    canvas_size: Tuple[int, int] = (1080, 1080),
    template_name: str = "fortune_scene",
) -> List[SceneElement]:
    """Shared layout. `bg_only` = single full-bleed background (no plants/hero/accent)."""
    tmpl = TEMPLATES.get(template_name) or TEMPLATES["balanced_scene"]
    seed = sum(ord(c) for c in (materials.get("hero") or materials.get("background") or {}).get("id", "x"))
    w, h = canvas_size
    elements: List[SceneElement] = []

    def resolve_path(item: Optional[Dict[str, Any]]) -> str:
        if not item:
            return ""
        cid = item.get("id") or ""
        path = cutouts_by_id.get(cid) or ""
        if not path and item.get("file"):
            path = str(ROOT / item["file"])
        return path

    bg = materials.get("background") or {}
    elements.append(
        _make_element(
            bg,
            cutout=resolve_path(bg),
            anchor="center",
            canvas_size=canvas_size,
            role="environment",
            depth="background",
            start=0.0,
            duration=1.2,
            scale=1.05,
            base_w=int(w * 1.02),
            base_h=int(h * 1.02),
            visual_weight=20,
        )
    )

    if tmpl.get("bg_only") or template_name == "bg_only":
        return validate_and_fix_layout(elements, canvas_size, template_name=template_name)

    max_h = int(h * float(tmpl.get("plant_max_height_ratio") or 0.28))
    size_map = tmpl.get("plant_size_scale") or {"large": 1.0, "medium": 0.72, "small": 0.48}
    plants = materials.get("plants") or []
    if isinstance(plants, dict):
        plants = [plants]
    plant_src = plants[0] if plants else None
    for i, slot in enumerate(tmpl.get("plant_slots") or []):
        if not plant_src:
            break
        use = dict(plant_src)
        use["id"] = f"{plant_src.get('id')}_p{i}"
        sz = slot.get("size") or "medium"
        ratio = float(size_map.get(sz, 0.72))
        bh = max(40, int(max_h * ratio))
        elements.append(
            _make_element(
                use,
                cutout=resolve_path(plant_src),
                anchor=slot["anchor"],
                canvas_size=canvas_size,
                role="frame",
                depth="foreground" if sz == "large" else "midground",
                start=2.0 + i * 0.35,
                duration=2.2,
                scale=1.0,
                base_w=bh,
                base_h=bh,
                rotation=(-4 + i * 3) * 0.5,
                visual_weight=55 if sz == "large" else (35 if sz == "small" else 45),
            )
        )

    hero = materials.get("hero")
    hero_anchors = tmpl.get("hero_anchors") or ["hero_right", "hero_left"]
    preferred = tmpl.get("hero_anchor") or hero_anchors[0]
    ordered = [preferred] + [a for a in hero_anchors if a != preferred]
    hero_anchor = ordered[seed % len(ordered)]
    if hero:
        el = _make_element(
            hero,
            cutout=resolve_path(hero),
            anchor=hero_anchor,
            canvas_size=canvas_size,
            role="hero",
            depth="hero",
            start=8.5,
            duration=2.8,
            scale=1.0,
            base_w=int(w * 0.42),
            base_h=int(h * 0.42),
            y_bias=float(tmpl.get("hero_y_bias") or 0),
            rotation=((seed % 5) - 2) * 0.35,
            visual_weight=92,
        )
        edge_x = (tmpl.get("hero_edge_x") or {}).get(hero_anchor)
        if edge_x is not None:
            el.x = float(edge_x) * w
        elements.append(el)

    accents = materials.get("accents") or []
    if not accents:
        one = materials.get("accent") or materials.get("fortune")
        accents = [one] if one else []
    accent_item = next((a for a in accents if a), None)
    if accent_item:
        el = _make_element(
            accent_item,
            cutout=resolve_path(accent_item),
            anchor="center",
            canvas_size=canvas_size,
            role="decorative",
            depth="foreground",
            start=4.5,
            duration=4.0,
            scale=1.0,
            base_w=int(w * 1.0),
            base_h=int(h * 1.0),
            rotation=0,
            visual_weight=22,
        )
        el.x = w * 0.5
        el.y = h * 0.5
        el.animation_type = "particle_twinkle"
        elements.append(el)

    return validate_and_fix_layout(elements, canvas_size, template_name=template_name)


def build_scene_elements(
    materials: Dict[str, Any],
    cutouts_by_id: Dict[str, str],
    canvas_size: Tuple[int, int] = (1080, 1080),
) -> List[SceneElement]:
    template_name = materials.get("template") or "bg_only"
    if not materials.get("hero") and not materials.get("plants") and not materials.get("accent"):
        template_name = "bg_only"
    if template_name in TEMPLATES:
        return build_slot_scene_elements(materials, cutouts_by_id, canvas_size, template_name)
    return build_slot_scene_elements(materials, cutouts_by_id, canvas_size, "bg_only")


def validate_and_fix_layout(
    elements: List[SceneElement],
    canvas_size: Tuple[int, int],
    template_name: str = "",
) -> List[SceneElement]:
    """Composition checks: hero clarity, avoid plant center steal, reduce overlap."""
    w, h = canvas_size
    hero = next((e for e in elements if e.role == "hero"), None)

    # All four theme templates use the shared 4-slot layout rules
    if template_name in TEMPLATES:
        for e in elements:
            if e.depth == "background":
                e.x, e.y = w / 2, h / 2
            if e.role == "decorative" and hero and e.visual_weight >= hero.visual_weight:
                e.scale *= 0.75
                e.visual_weight = min(e.visual_weight, hero.visual_weight - 15)
            if e.role == "frame":
                max_h = int(h * 0.28)
                if e.base_h > max_h:
                    e.base_h = max_h
                    e.base_w = min(e.base_w, max_h)
        return sorted(elements, key=lambda e: (e.depth_index, e.visual_weight))

    for e in elements:
        if e.role == "frame" and abs(e.x - w / 2) < w * 0.15:
            e.x = w * 0.16 if e.x <= w / 2 else w * 0.84
            e.anchor = "left_frame" if e.x < w / 2 else "right_frame"
            e.scale = min(e.scale, 0.95)
        if e.role == "decorative" and hero and e.visual_weight >= hero.visual_weight:
            e.scale *= 0.82
            e.visual_weight = min(e.visual_weight, hero.visual_weight - 10)
        if e.depth == "background":
            e.x, e.y = w / 2, h / 2
            e.scale = max(e.scale, 1.0)
    if hero:
        hero.x = min(max(hero.x, w * 0.32), w * 0.68)
        hero.y = min(max(hero.y, h * 0.42), h * 0.72)
        for e in elements:
            if e is hero or e.depth == "background":
                continue
            # full-bleed particle overlay stays centered
            if e.role == "decorative" and e.base_w >= w * 0.9:
                e.x, e.y = w / 2, h / 2
                continue
            dx, dy = e.x - hero.x, e.y - hero.y
            dist = (dx * dx + dy * dy) ** 0.5
            if dist < w * 0.14 and e.role != "frame":
                e.x += w * 0.12 if dx >= 0 else -w * 0.12
                e.scale *= 0.9
    left = sum(e.visual_weight for e in elements if e.x < w / 2 and e.depth != "background" and not (e.role == "decorative" and e.base_w >= w * 0.9))
    right = sum(e.visual_weight for e in elements if e.x >= w / 2 and e.depth != "background" and not (e.role == "decorative" and e.base_w >= w * 0.9))
    if left > right * 1.6:
        for e in elements:
            if e.role == "decorative" and e.base_w < w * 0.9 and e.x < w / 2:
                e.x = w - e.x
                break
    elif right > left * 1.6:
        for e in elements:
            if e.role == "decorative" and e.base_w < w * 0.9 and e.x > w / 2:
                e.x = w - e.x
                break
    return sorted(elements, key=lambda e: (e.depth_index, e.visual_weight))


def _parse_fill_color(el: SceneElement) -> Optional[Tuple[int, int, int, int]]:
    for t in el.tags or []:
        if isinstance(t, str) and t.startswith("fill:"):
            parts = t[5:].split(",")
            try:
                vals = [int(x) for x in parts[:4]]
                while len(vals) < 4:
                    vals.append(255)
                return tuple(vals)  # type: ignore
            except Exception:
                return None
    return None


def load_element_base(el: SceneElement) -> Optional[Image.Image]:
    fill = _parse_fill_color(el)
    if fill is not None:
        return Image.new("RGBA", (max(1, el.base_w), max(1, el.base_h)), fill)
    path = Path(el.cutout) if el.cutout else None
    if not path or not path.exists():
        return None
    im = Image.open(path).convert("RGBA")
    tw = max(1, int(el.base_w * el.scale))
    th = max(1, int(el.base_h * el.scale))
    return im.resize((tw, th), Image.Resampling.BILINEAR)


def paste_element(
    canvas: Image.Image,
    el: SceneElement,
    *,
    opacity: float = 1.0,
    dx: float = 0.0,
    dy: float = 0.0,
    scale_mul: float = 1.0,
    rotation_add: float = 0.0,
    base_image: Optional[Image.Image] = None,
) -> None:
    fill = _parse_fill_color(el)
    if fill is not None and el.depth == "background":
        # full-bleed warm fill
        plate = Image.new("RGBA", canvas.size, fill)
        if opacity < 0.999:
            r, g, b, a = plate.split()
            a = a.point(lambda x, o=opacity: int(x * max(0.0, min(1.0, o))))
            plate = Image.merge("RGBA", (r, g, b, a))
        canvas.alpha_composite(plate, (0, 0))
        return

    im = base_image
    if im is None:
        im = load_element_base(el)
    if im is None:
        return
    if abs(scale_mul - 1.0) > 0.01:
        tw = max(1, int(im.size[0] * scale_mul))
        th = max(1, int(im.size[1] * scale_mul))
        im = im.resize((tw, th), Image.Resampling.BILINEAR)
    else:
        im = im.copy()
    rot = el.rotation + rotation_add
    if abs(rot) > 0.05:
        im = im.rotate(rot, expand=True, resample=Image.Resampling.BILINEAR)
    if opacity < 0.999:
        r, g, b, a = im.split()
        a = a.point(lambda x, o=opacity: int(x * max(0.0, min(1.0, o))))
        im = Image.merge("RGBA", (r, g, b, a))
    style = DEPTH_STYLE.get(el.depth, DEPTH_STYLE["midground"])
    ox, oy = style["shadow_offset"]
    cx = int(el.x + dx - im.size[0] / 2)
    cy = int(el.y + dy - im.size[1] / 2)
    if el.depth != "background" and style["shadow_opacity"] > 0.01:
        shadow = Image.new("RGBA", im.size, (0, 0, 0, 0))
        alpha = im.split()[-1].point(lambda a, s=style["shadow_opacity"]: int(a * s))
        shadow.putalpha(alpha)
        canvas.alpha_composite(shadow, (cx + int(ox), cy + int(oy)))
    canvas.alpha_composite(im, (cx, cy))


def compose_scene_from_elements(
    elements: List[SceneElement],
    scene_path: Path,
    size: Tuple[int, int] = (1080, 1080),
) -> Path:
    scene_path.parent.mkdir(parents=True, exist_ok=True)
    # start from warm paper; fill-bg element may overwrite
    canvas = Image.new("RGBA", size, (247, 244, 239, 255))
    has_fill = any(_parse_fill_color(e) for e in elements if e.depth == "background")
    if not has_fill:
        for i, alpha in enumerate((255, 230, 200)):
            plate = Image.new("RGBA", (size[0] - 80 - i * 20, size[1] - 100 - i * 24), (255, 252, 245, alpha))
            canvas.alpha_composite(plate, (40 + i * 8, 50 + i * 12))
    cache = {el.asset_id: load_element_base(el) for el in elements}
    for el in sorted(elements, key=lambda e: e.depth_index):
        paste_element(canvas, el, opacity=1.0, base_image=cache.get(el.asset_id))
    canvas.convert("RGB").save(scene_path.with_suffix(".jpg"), quality=92)
    canvas.save(scene_path)
    return scene_path


def compose_scene(
    cutouts: List[Dict[str, str]],
    scene_path: Path,
    size: tuple = (1080, 1080),
    materials: Optional[Dict[str, Any]] = None,
) -> Path:
    """Backward-compatible entry. Prefer materials+cutouts → SceneElements."""
    if materials:
        by_id = {c["id"]: c["cutout"] for c in cutouts if c.get("id")}
        elements = build_scene_elements(materials, by_id, size)
        return compose_scene_from_elements(elements, scene_path, size)

    # legacy fallback: treat cutouts as ordered layers near center
    canvas = Image.new("RGBA", size, (247, 244, 239, 255))
    for i, item in enumerate(cutouts):
        p = Path(item.get("cutout") or "")
        if not p.exists():
            continue
        im = Image.open(p).convert("RGBA").resize((int(size[0] * 0.45), int(size[1] * 0.45)), Image.Resampling.LANCZOS)
        x = int(size[0] * (0.25 + 0.1 * (i % 3)))
        y = int(size[1] * (0.25 + 0.1 * (i % 2)))
        canvas.alpha_composite(im, (x, y))
    canvas.convert("RGB").save(scene_path.with_suffix(".jpg"), quality=92)
    canvas.save(scene_path)
    return scene_path


def save_scene_json(path: Path, elements: List[SceneElement], meta: Dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    data = {"meta": meta, "elements": [e.to_dict() for e in elements]}
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")


# Legacy constant kept so old imports do not crash
LAYER_BOX = {
    "background": (0, 0, 1080, 1080),
    "nature": (40, 320, 1040, 1040),
    "plants": (40, 420, 480, 1000),
    "animals": (240, 180, 840, 820),
    "fortune": (680, 40, 1040, 360),
}
