"""ShortsLayout — 1080×1920 normalized zones, anchors, hero sizing (V3).

Does not change square GIF layout (`scene.ANCHORS`). Shorts remaps SceneElements
onto a true 9:16 paper-cut world.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Dict, List, Optional, Sequence, Tuple

from .scene import SceneElement


# Normalized region bands (fractions of height)
ZONES = {
    "hook": (0.05, 0.18),
    "hero": (0.18, 0.58),
    "fortune": (0.56, 0.68),
    "info": (0.66, 0.90),
    "brand": (0.91, 0.97),
}

# Safe area — keep critical text inside
SAFE = {
    "left": 0.08,
    "right": 0.08,   # → usable x ≤ 0.92; avoid x > 0.88 for text
    "top": 0.05,
    "bottom": 0.08,  # → usable y ≤ 0.92
}

SHORTS_ANCHORS: Dict[str, Tuple[float, float]] = {
    "hook_center": (0.50, 0.10),
    "hero_left": (0.38, 0.38),
    "hero_center": (0.50, 0.36),
    "hero_right": (0.62, 0.38),
    "hero_back": (0.50, 0.30),
    "frame_left": (0.14, 0.42),
    "frame_right": (0.86, 0.42),
    "scene_left": (0.20, 0.48),
    "scene_right": (0.80, 0.48),
    "nature_top_left": (0.18, 0.22),
    "nature_top_right": (0.82, 0.22),
    "nature_top_center": (0.50, 0.20),
    "water_bottom": (0.50, 0.52),
    "fortune_accent_left": (0.28, 0.50),
    "fortune_accent_right": (0.72, 0.50),
    "season_left": (0.22, 0.54),
    "season_right": (0.78, 0.54),
    "fortune_center": (0.50, 0.61),
    "info_center": (0.50, 0.73),
    "brand_center": (0.50, 0.945),
    "bottom_left": (0.18, 0.55),
    "bottom_right": (0.82, 0.55),
    "center": (0.50, 0.40),
}


@dataclass
class ShortsLayout:
    width: int = 1080
    height: int = 1920
    zones: Dict[str, Tuple[float, float]] = None  # type: ignore
    safe: Dict[str, float] = None  # type: ignore
    anchors: Dict[str, Tuple[float, float]] = None  # type: ignore
    # typography / card stack Y (center of card area)
    hook_y: float = 0.10
    fortune_y: float = 0.61
    info_y: float = 0.74
    brand_y: float = 0.945
    # hero
    hero_y: float = 0.36
    hero_width_min: float = 0.45
    hero_width_max: float = 0.62

    def __post_init__(self) -> None:
        if self.zones is None:
            self.zones = dict(ZONES)
        if self.safe is None:
            self.safe = dict(SAFE)
        if self.anchors is None:
            self.anchors = dict(SHORTS_ANCHORS)

    @property
    def size(self) -> Tuple[int, int]:
        return (self.width, self.height)

    def px(self, nx: float, ny: float) -> Tuple[float, float]:
        return nx * self.width, ny * self.height

    def clamp_text_x(self, nx: float) -> float:
        return max(self.safe["left"], min(1.0 - self.safe["right"], min(nx, 0.88)))

    def clamp_text_y(self, ny: float) -> float:
        return max(self.safe["top"], min(1.0 - self.safe["bottom"], min(ny, 0.92)))

    def content_width(self) -> int:
        return int(self.width * (1.0 - self.safe["left"] - self.safe["right"]))

    def to_dict(self) -> Dict[str, Any]:
        return {
            "size": [self.width, self.height],
            "zones": self.zones,
            "safe": self.safe,
            "hook_y": self.hook_y,
            "fortune_y": self.fortune_y,
            "info_y": self.info_y,
            "brand_y": self.brand_y,
            "hero_y": self.hero_y,
        }


def default_layout(size: Tuple[int, int] = (1080, 1920)) -> ShortsLayout:
    return ShortsLayout(width=int(size[0]), height=int(size[1]))


def _map_anchor(el: SceneElement, seed: int) -> str:
    """Pick a Shorts anchor from category / role / name."""
    cat = (el.category or "").lower()
    role = el.role or ""
    name = (el.name or "").lower()
    anchor = el.anchor or ""

    if role == "hero" or cat == "hero":
        if "left" in anchor:
            return "hero_left"
        if "right" in anchor:
            return "hero_right"
        return "hero_center"

    if el.depth == "background" or cat == "background":
        return "center"

    if cat == "plants" or role == "frame":
        if "left" in anchor or "left" in name:
            return "frame_left"
        if "right" in anchor or "right" in name:
            return "frame_right"
        if "bottom" in anchor:
            return "bottom_left" if seed % 2 == 0 else "bottom_right"
        return "frame_left" if seed % 2 == 0 else "frame_right"

    if cat == "nature":
        if any(k in name for k in ("太陽", "月亮", "sun", "moon", "星", "雲", "虹", "光", "霧", "霞")):
            return "nature_top_left" if seed % 2 == 0 else "nature_top_right"
        if any(k in name for k in ("水", "湖", "海", "波", "浪")):
            return "water_bottom"
        if any(k in name for k in ("山", "峰", "mountain", "遠山")):
            return "hero_back"
        return "nature_top_center"

    if cat == "season":
        return "season_left" if seed % 2 == 0 else "season_right"

    if cat in ("fortune", "career", "love", "accent") or role == "decorative":
        if any(k in name for k in ("粒子", "光點", "twinkle", "particle")):
            return "center"
        return "fortune_accent_left" if seed % 2 == 0 else "fortune_accent_right"

    return "scene_left" if seed % 2 == 0 else "scene_right"


def _hero_base_size(layout: ShortsLayout, aspect: float = 1.0) -> Tuple[int, int]:
    """Hero visual size ~45–65% of canvas width, preserve aspect."""
    frac = (layout.hero_width_min + layout.hero_width_max) / 2.0
    bw = int(layout.width * frac)
    bh = int(bw / max(0.5, aspect)) if aspect > 0 else bw
    # keep hero inside hero zone height
    zone_h = int(layout.height * (ZONES["hero"][1] - ZONES["hero"][0]) * 0.92)
    if bh > zone_h:
        bh = zone_h
        bw = int(bh * max(0.5, aspect))
    return max(120, bw), max(120, bh)


def apply_shorts_layout(
    elements: Sequence[SceneElement],
    size: Tuple[int, int] = (1080, 1920),
    *,
    timeline: Optional[Any] = None,
) -> List[SceneElement]:
    """Remap square SceneElements onto 9:16 ShortsLayout positions & beat timing."""
    layout = default_layout(size)
    w, h = layout.size
    out: List[SceneElement] = []
    seed = sum(ord(c) for c in (elements[0].asset_id if elements else "x"))

    # Import timings lazily to avoid circular import at module load
    from .shorts_timeline import ShortsTimeline, default_shorts_timeline

    tl: ShortsTimeline = timeline or default_shorts_timeline()

    hero = next((e for e in elements if e.role == "hero"), None)
    used_anchors: set = set()

    for i, src in enumerate(elements):
        el = SceneElement(**{**src.to_dict()})
        key = _map_anchor(el, seed + i * 17)
        # avoid stacking identical decorative anchors
        if key in used_anchors and el.role != "hero" and el.depth != "background":
            alt = {
                "frame_left": "frame_right",
                "frame_right": "frame_left",
                "fortune_accent_left": "fortune_accent_right",
                "fortune_accent_right": "fortune_accent_left",
                "nature_top_left": "nature_top_right",
                "nature_top_right": "nature_top_left",
                "bottom_left": "bottom_right",
                "bottom_right": "bottom_left",
            }.get(key)
            if alt and alt not in used_anchors:
                key = alt
        used_anchors.add(key)
        nx, ny = layout.anchors.get(key, layout.anchors["hero_center"])
        # keep decorative / frame away from right UI strip for text safety (elements OK to 0.88)
        if el.role != "hero" and el.depth != "background":
            nx = min(nx, 0.88)
        el.anchor = key
        el.x = nx * w
        el.y = ny * h

        if el.depth == "background":
            el.x, el.y = w / 2, h / 2
            el.base_w = int(w * 1.06)
            el.base_h = int(h * 1.06)
            el.scale = 1.0
            el.start_time = tl.scene_start
            el.duration = max(1.5, tl.bg_build_end - tl.scene_start)
            el.animation_type = "bg_slow_zoom"
            el.visual_weight = min(el.visual_weight, 20)
        elif el.role == "hero":
            aspect = (el.base_w / el.base_h) if el.base_h else 1.0
            el.base_w, el.base_h = _hero_base_size(layout, aspect)
            el.y = layout.hero_y * h
            el.x = layout.anchors.get(key, (0.5, layout.hero_y))[0] * w
            el.start_time = tl.hero_start
            el.duration = tl.hero_duration
            el.animation_type = "hero_place"
            el.visual_weight = 100
            el.scale = 1.0
        elif el.role == "frame" or el.category == "Plants":
            el.start_time = tl.plants_start + (i % 3) * 0.35
            el.duration = 2.2
            # frame size relative to vertical canvas
            el.base_h = min(el.base_h, int(h * 0.22))
            el.base_w = min(el.base_w, int(w * 0.32))
            el.visual_weight = min(el.visual_weight, 55)
            if el.animation_type not in ("rise", "slide_left", "slide_right", "gentle_sway"):
                el.animation_type = "rise"
        elif el.category == "Nature":
            el.start_time = tl.nature_start + (i % 4) * 0.25
            el.duration = 2.0
            el.base_w = min(el.base_w, int(w * 0.38))
            el.base_h = min(el.base_h, int(h * 0.18))
            el.visual_weight = min(el.visual_weight, 40)
        elif el.category in ("Fortune", "Career", "Love", "Accent") or el.role == "decorative":
            el.start_time = tl.accent_start + (i % 3) * 0.3
            el.duration = 2.4
            # full-bleed particles stay large but lower weight
            if el.base_w >= w * 0.85:
                el.base_w, el.base_h = w, h
                el.x, el.y = w / 2, h * 0.40
                el.visual_weight = min(el.visual_weight, 22)
            else:
                el.base_w = min(el.base_w, int(w * 0.28))
                el.base_h = min(el.base_h, int(h * 0.14))
                el.visual_weight = min(max(el.visual_weight, 15), 45)
        elif el.category == "Season":
            el.start_time = tl.season_start + (i % 2) * 0.4
            el.duration = 2.0
            el.base_w = min(el.base_w, int(w * 0.26))
            el.base_h = min(el.base_h, int(h * 0.14))
            el.visual_weight = min(el.visual_weight, 35)
        else:
            el.start_time = tl.nature_start
            el.duration = 2.0
            el.visual_weight = min(el.visual_weight, 40)

        out.append(el)

    # Protect hero: shrink tall heroes; push decorative below hero midline
    hero = next((e for e in out if e.role == "hero"), None)
    if hero:
        max_hero_bottom = h * ZONES["hero"][1] * 0.98
        half = hero.base_h * hero.scale / 2
        if hero.y + half > max_hero_bottom:
            hero.y = max_hero_bottom - half
        # ensure info cards sit below hero visual
        for e in out:
            if e is hero or e.depth == "background":
                continue
            if e.role == "decorative" and e.base_w >= w * 0.85:
                continue
            if abs(e.x - hero.x) < w * 0.12 and abs(e.y - hero.y) < h * 0.08:
                e.y = min(h * 0.52, e.y + h * 0.08)
                e.scale *= 0.9
                e.visual_weight = min(e.visual_weight, hero.visual_weight - 20)

    return sorted(out, key=lambda e: (e.depth_index, e.visual_weight))
