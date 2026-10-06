"""Animation Library — shared by GIF and Shorts renderers."""
from __future__ import annotations

import math
from dataclasses import dataclass
from typing import Dict, List, Optional, Tuple

from .scene import DEPTH_STYLE, SceneElement


@dataclass
class AnimState:
    opacity: float = 1.0
    dx: float = 0.0
    dy: float = 0.0
    scale_mul: float = 1.0
    rotation_add: float = 0.0


def _clamp01(x: float) -> float:
    return max(0.0, min(1.0, x))


def _ease_out_cubic(t: float) -> float:
    t = _clamp01(t)
    return 1 - (1 - t) ** 3


def _ease_in_out(t: float) -> float:
    t = _clamp01(t)
    return 3 * t * t - 2 * t * t * t


def local_t(el: SceneElement, t: float) -> float:
    if t < el.start_time:
        return 0.0
    if el.duration <= 0:
        return 1.0
    return _clamp01((t - el.start_time) / el.duration)


def animate_element(el: SceneElement, t: float, duration_total: float = 20.0) -> AnimState:
    """Compute transform for element at global time t (seconds)."""
    lt = local_t(el, t)
    if t < el.start_time:
        return AnimState(opacity=0.0, scale_mul=0.92)

    kind = el.animation_type or "slow_reveal"
    state = AnimState()
    e = _ease_out_cubic(lt)

    if kind == "slow_reveal":
        state.opacity = e
        state.scale_mul = 0.96 + 0.04 * e
    elif kind == "slide_left":
        state.opacity = e
        state.dx = -80 * (1 - e)
        state.scale_mul = 0.94 + 0.06 * e
    elif kind == "slide_right":
        state.opacity = e
        state.dx = 80 * (1 - e)
        state.scale_mul = 0.94 + 0.06 * e
    elif kind == "rise":
        state.opacity = e
        state.dy = 80 * (1 - e)
        state.scale_mul = 0.85 + 0.15 * e
    elif kind == "float":
        state.opacity = e
        state.dy = -12 * (1 - e)
        state.scale_mul = 0.92 + 0.08 * e
    elif kind == "hero_reveal":
        # gentle sway reveal (紙片輕晃登場) — GIF path
        if lt < 0.4:
            p = lt / 0.4
            state.opacity = _ease_out_cubic(p)
            state.scale_mul = 0.88 + 0.12 * _ease_out_cubic(p)
            state.dy = 28 * (1 - _ease_out_cubic(p))
            state.rotation_add = -2.0 + 3.0 * p
        elif lt < 0.75:
            p = (lt - 0.4) / 0.35
            state.opacity = 1.0
            state.scale_mul = 1.0
            state.rotation_add = 1.0 - 2.0 * p
            state.dy = -4 * math.sin(p * math.pi)
        else:
            p = (lt - 0.75) / 0.25
            state.opacity = 1.0
            state.scale_mul = 1.0
            state.rotation_add = -1.0 * (1 - p)
            state.dy = 0.0
    elif kind == "hero_place":
        # V3 Shorts: paper placed into scene (no fade-in pop)
        # scale 0.82→0.92→1.03→1.00 · Y+30→-5→0 · rot -1→+1→0
        state.opacity = 1.0
        if lt < 0.35:
            p = _ease_out_cubic(lt / 0.35)
            state.scale_mul = 0.82 + 0.10 * p
            state.dy = 30 * (1 - p)
            state.rotation_add = -1.0 + 1.0 * p
        elif lt < 0.70:
            p = _ease_in_out((lt - 0.35) / 0.35)
            state.scale_mul = 0.92 + 0.11 * p
            state.dy = 30 * (1 - p) + (-5) * p  # from ~0 toward -5 after first phase
            # continue from dy≈0 at end of phase1; blend 0 → -5
            state.dy = -5.0 * p
            state.rotation_add = 1.0 * p
        else:
            p = _ease_out_cubic((lt - 0.70) / 0.30)
            state.scale_mul = 1.03 - 0.03 * p
            state.dy = -5.0 * (1 - p)
            state.rotation_add = 1.0 * (1 - p)
    elif kind == "bg_slow_zoom":
        # Full-bleed background: soft reveal then 1.00→1.03 over clip
        state.opacity = min(1.0, 0.35 + 0.65 * e) if lt < 1.0 else 1.0
        progress = _clamp01(t / max(1.0, duration_total))
        state.scale_mul = 1.00 + 0.03 * progress
    elif kind == "paper_reveal":
        state.opacity = e
        state.scale_mul = 0.94 + 0.06 * e
        state.dy = 12 * (1 - e)
    elif kind == "soft_scale":
        state.opacity = e
        state.scale_mul = 0.90 + 0.10 * e
    elif kind == "particle_twinkle":
        # 出現 → 消失 → 出現（慢速閃爍）
        elapsed = max(0.0, t - el.start_time)
        ramp = min(1.0, elapsed / 0.6)
        # ~0.35 Hz：約 2.8 秒一輪明暗
        phase = elapsed * 0.35 + (el.visual_weight % 17) * 0.21 + (hash(el.asset_id) % 7) * 0.15
        wave = 0.5 + 0.5 * math.sin(phase * math.pi)
        blink = wave ** 1.15
        state.opacity = ramp * (0.28 + 0.72 * blink)
        state.scale_mul = 0.96 + 0.04 * blink
        state.dy = math.sin(phase * 0.4) * 2
    elif kind == "particle_gather":
        state.opacity = e
        state.scale_mul = 0.55 + 0.45 * e
        state.dx = 30 * (1 - e) * math.cos(el.visual_weight)
        state.dy = -25 * (1 - e)
        if lt > 0.85:
            state.scale_mul = 1.0 + 0.04 * math.sin((lt - 0.85) * 20)
    elif kind == "scale_pop":
        state.opacity = e
        state.scale_mul = 0.7 + 0.35 * e if lt < 0.7 else 1.05 - 0.05 * ((lt - 0.7) / 0.3)
    elif kind == "unfold":
        state.opacity = e
        state.scale_mul = 0.88 + 0.12 * e
        state.rotation_add = -4 * (1 - e)
    elif kind == "gentle_sway":
        state.opacity = e
        state.scale_mul = 0.9 + 0.1 * e
        state.rotation_add = 2.0 * math.sin(lt * math.pi)
    elif kind == "parallax":
        state.opacity = e
        state.dx = 20 * (1 - e)
    else:
        state.opacity = e
        state.scale_mul = 0.95 + 0.05 * e

    # Idle micro-motion after settled (skip twinkle —它有自己的閃爍)
    if kind != "particle_twinkle":
        # Shorts V3 uses hero_place / bg_slow_zoom → idle from ~10s; GIF keeps 11.5 floor
        idle_floor = 10.0 if kind in ("hero_place", "bg_slow_zoom", "paper_reveal") else 11.5
        idle_start = max(el.start_time + el.duration, idle_floor)
        if t >= idle_start and t < duration_total - 0.35:
            motion = float(DEPTH_STYLE.get(el.depth, DEPTH_STYLE["midground"])["motion"])
            phase = t * (0.7 + el.visual_weight * 0.01)
            if el.depth in ("foreground", "midground") or el.role == "frame":
                state.rotation_add += math.sin(phase) * min(1.2, motion * 0.12)
                state.dy += math.sin(phase * 0.8) * motion * 0.25
            elif el.role == "hero":
                state.dy += math.sin(phase * 0.6) * motion * 0.2
                state.rotation_add += math.sin(phase * 0.5) * 0.35
            elif el.category == "Nature":
                name = el.name or ""
                if any(k in name for k in ("水", "波", "湖")):
                    state.dx += math.sin(phase * 0.5) * motion * 0.4
                elif any(k in name for k in ("光", "星")):
                    state.opacity = min(1.0, state.opacity * (0.92 + 0.08 * abs(math.sin(phase))))
                else:
                    state.dx += math.sin(phase * 0.4) * motion * 0.2
            elif el.category in ("Accent", "Fortune", "Love", "Season"):
                state.dy += math.sin(phase * 0.7) * motion * 0.15
                state.rotation_add += math.sin(phase) * 0.4

    return state


@dataclass
class Timeline:
    duration: float = 22.0
    # scene beats
    bg_end: float = 2.0
    frame_end: float = 4.5
    accent_end: float = 8.5
    hero_end: float = 11.5
    # text beats
    date_start: float = 11.5
    date_end: float = 13.5
    yi_start: float = 13.5
    yi_end: float = 16.0
    ji_start: float = 16.0
    ji_end: float = 18.0
    summary_start: float = 18.0
    summary_end: float = 20.0
    logo_start: float = 20.0
    # legacy aliases
    scene_hold_end: float = 11.5
    fortune_card_start: float = 11.5
    fortune_card_end: float = 13.5

    def to_dict(self) -> Dict:
        return {
            "duration": self.duration,
            "beats": {
                "1_background": [0.0, self.bg_end],
                "2_date": [self.date_start, self.date_end],
                "3_yi": [self.yi_start, self.yi_end],
                "4_ji": [self.ji_start, self.ji_end],
                "5_summary": [self.summary_start, self.summary_end],
                "6_logo": [self.logo_start, self.duration],
            },
        }


def build_timeline(
    yi_items: List[str],
    ji_items: List[str],
    base: float = 20.0,
    *,
    card_pace: float = 1.0,
    logo_hold: float | None = None,
) -> Timeline:
    """BG → Date → 宜 → 忌 → 今日總結 → Logo.

    card_pace: 1.0 = normal 20s (MP4); 0.5 = same beats at half speed spacing (GIF ~10s).
    logo_hold: if set, force last logo beat length in seconds (after pace scaling).
    """
    pace = max(0.25, float(card_pace))
    yi_n = max(1, len([x for x in yi_items if x]))
    ji_n = max(1, len([x for x in ji_items if x]))
    yi_need = max(1.8, min(3.0, 0.45 * yi_n + 1.0))
    ji_need = max(1.8, min(2.8, 0.45 * ji_n + 1.0))
    date_need = 1.8
    summary_need = 2.2
    logo_need = 2.0
    bg_hold = 1.6

    bg_end = bg_hold
    date_start = bg_hold
    date_end = date_start + date_need
    yi_start = date_end
    yi_end = yi_start + yi_need
    ji_start = yi_end
    ji_end = ji_start + ji_need
    summary_start = ji_end
    summary_end = summary_start + summary_need
    logo_start = summary_end
    total = logo_start + logo_need

    # Always build the normal 20s pacing first, then scale for GIF.
    target = float(base)
    if total < target:
        pad = target - total
        date_end = date_start + date_need + pad * 0.12
        yi_start = date_end
        yi_end = yi_start + yi_need + pad * 0.2
        ji_start = yi_end
        ji_end = ji_start + ji_need + pad * 0.2
        summary_start = ji_end
        summary_end = summary_start + summary_need + pad * 0.28
        logo_start = summary_end
        total = max(target, logo_start + logo_need)
    total = min(24.0, max(target, total))

    if abs(pace - 1.0) > 0.001:
        bg_end *= pace
        date_start *= pace
        date_end *= pace
        yi_start *= pace
        yi_end *= pace
        ji_start *= pace
        ji_end *= pace
        summary_start *= pace
        summary_end *= pace
        logo_start *= pace
        total *= pace

    if logo_hold is not None and float(logo_hold) > 0:
        total = logo_start + float(logo_hold)

    tl = Timeline(duration=total)
    tl.bg_end = bg_end
    tl.frame_end = bg_end
    tl.accent_end = bg_end
    tl.hero_end = bg_end
    tl.date_start = date_start
    tl.date_end = date_end
    tl.yi_start = yi_start
    tl.yi_end = yi_end
    tl.ji_start = ji_start
    tl.ji_end = ji_end
    tl.summary_start = summary_start
    tl.summary_end = summary_end
    tl.logo_start = logo_start
    tl.scene_hold_end = bg_end
    tl.fortune_card_start = date_start
    tl.fortune_card_end = date_end
    return tl


def card_opacity(t: float, start: float, end: float) -> float:
    """Appear then hold until end (no disappear). `end` kept for API compat."""
    if t < start:
        return 0.0
    fade = 0.28
    if t < start + fade:
        return _ease_out_cubic((t - start) / fade)
    return 1.0


def card_opacity_hold(t: float, start: float) -> float:
    """Fade in at start, stay visible for the rest of the clip."""
    return card_opacity(t, start, start + 999.0)


def card_scale_pop(t: float, start: float) -> float:
    lt = _clamp01((t - start) / 0.45)
    if lt <= 0:
        return 0.85
    if lt < 0.7:
        return 0.85 + 0.2 * _ease_out_cubic(lt / 0.7)
    return 1.05 - 0.05 * _ease_out_cubic((lt - 0.7) / 0.3)


def card_opacity_window(
    t: float,
    start: float,
    end: float,
    *,
    fade_in: float = 0.35,
    fade_out: float = 0.35,
) -> float:
    """Appear at start, hold, soft fade out before end (Shorts sequential cards)."""
    if t < start or t >= end:
        return 0.0
    if t < start + fade_in:
        return _ease_out_cubic((t - start) / max(0.01, fade_in))
    if t > end - fade_out:
        return _ease_out_cubic((end - t) / max(0.01, fade_out))
    return 1.0


def card_soft_scale(t: float, start: float) -> float:
    """Soft paper scale — no bounce (Shorts brand / hook)."""
    lt = _clamp01((t - start) / 0.55)
    if lt <= 0:
        return 0.94
    return 0.94 + 0.06 * _ease_out_cubic(lt)
