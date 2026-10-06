"""ShortsTimeline — centralized YouTube Shorts beat sheet (V3).

Independent of GIF `build_timeline` / `card_pace`. short_builder must use this
module only for MP4 timing — do not scatter seconds across builders.
"""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import Dict, List, Optional, Sequence


@dataclass
class ShortsTimeline:
    """20s default; extends to ≤22s when yi/ji need longer holds."""

    duration: float = 20.0

    # Scene formation
    hook_start: float = 0.2
    hook_date_end: float = 0.8
    hook_title_end: float = 1.2
    hook_end: float = 1.5

    scene_start: float = 1.0
    bg_build_end: float = 3.5
    nature_start: float = 1.0
    nature_end: float = 3.5
    plants_start: float = 2.5
    plants_end: float = 5.5

    hero_start: float = 4.0
    hero_duration: float = 2.4
    hero_end: float = 7.0

    accent_start: float = 6.0
    accent_end: float = 8.5
    season_start: float = 7.5
    season_end: float = 10.0

    scene_complete: float = 10.0
    scene_hold_end: float = 11.5  # ≥1.5s living hold

    # Info beats
    fortune_start: float = 11.5
    fortune_end: float = 14.0
    good_start: float = 14.0  # 宜
    good_end: float = 16.5
    bad_start: float = 16.5  # 忌
    bad_end: float = 19.0
    brand_start: float = 19.0
    brand_end: float = 20.0

    # Idle micro-motion window
    idle_start: float = 10.0

    def to_dict(self) -> Dict:
        return {
            "duration": self.duration,
            "beats": {
                "hook": [self.hook_start, self.hook_end],
                "scene_bg_nature": [self.scene_start, self.bg_build_end],
                "plants": [self.plants_start, self.plants_end],
                "hero": [self.hero_start, self.hero_end],
                "accent": [self.accent_start, self.accent_end],
                "season": [self.season_start, self.season_end],
                "scene_hold": [self.scene_complete, self.scene_hold_end],
                "fortune": [self.fortune_start, self.fortune_end],
                "good": [self.good_start, self.good_end],
                "bad": [self.bad_start, self.bad_end],
                "brand": [self.brand_start, self.duration],
            },
            "version": "shorts_v3",
        }


def default_shorts_timeline(
    yi_items: Optional[Sequence[str]] = None,
    ji_items: Optional[Sequence[str]] = None,
    base: float = 20.0,
) -> ShortsTimeline:
    """Build V3 timeline. Extends to 22s max if text needs longer holds — never compress below 2s for 宜/忌."""
    yi_n = len([x for x in (yi_items or []) if str(x).strip()])
    ji_n = len([x for x in (ji_items or []) if str(x).strip()])

    # Minimum holds (spec: 宜/忌 ≥ 2s)
    good_need = 2.5 if yi_n <= 3 else 2.8
    bad_need = 2.5 if ji_n <= 3 else 2.8
    fortune_need = 2.5
    brand_need = 1.0
    hold_need = 1.5

    tl = ShortsTimeline()
    # Fixed early scene beats (paper world forming)
    tl.hook_start = 0.2
    tl.hook_date_end = 0.8
    tl.hook_title_end = 1.2
    tl.hook_end = 1.5
    tl.scene_start = 1.0
    tl.nature_start = 1.0
    tl.bg_build_end = 3.5
    tl.nature_end = 3.5
    tl.plants_start = 2.5
    tl.plants_end = 5.5
    tl.hero_start = 4.0
    tl.hero_duration = 2.4
    tl.hero_end = 7.0
    tl.accent_start = 6.0
    tl.accent_end = 8.5
    tl.season_start = 7.5
    tl.season_end = 10.0
    tl.scene_complete = 10.0
    tl.scene_hold_end = tl.scene_complete + hold_need  # 11.5
    tl.idle_start = tl.scene_complete

    tl.fortune_start = tl.scene_hold_end
    tl.fortune_end = tl.fortune_start + fortune_need
    tl.good_start = tl.fortune_end
    tl.good_end = tl.good_start + good_need
    tl.bad_start = tl.good_end
    tl.bad_end = tl.bad_start + bad_need
    tl.brand_start = tl.bad_end
    total = tl.brand_start + brand_need

    target = float(base)
    if total < target:
        # stretch brand / hold slightly rather than compressing text
        pad = target - total
        tl.brand_start = tl.bad_end
        total = tl.brand_start + brand_need + pad
    elif total > 22.0:
        # Cap at 22s: trim brand only (never 宜/忌)
        total = 22.0
        tl.brand_start = min(tl.brand_start, total - brand_need)
        tl.bad_end = min(tl.bad_end, tl.brand_start)
        tl.good_end = min(tl.good_end, tl.bad_start)
    else:
        total = max(target, total)

    tl.duration = total
    tl.brand_end = total
    return tl


# Convenience aliases matching the spec naming
def build_shorts_timeline(
    yi_items: Optional[Sequence[str]] = None,
    ji_items: Optional[Sequence[str]] = None,
    base: float = 20.0,
) -> ShortsTimeline:
    return default_shorts_timeline(yi_items, ji_items, base=base)
