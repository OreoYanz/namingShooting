"""GIF / WebP — Scene Engine + Animation Library (shared with Shorts)."""
from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

from PIL import Image

from .animation import AnimState, animate_element, build_timeline, card_opacity_hold, card_scale_pop
from .paper_cards import (
    STACK_Y,
    apply_overlay,
    render_date_card,
    render_logo_slogan_card,
    render_summary_card,
    render_yi_ji_block,
    to_traditional,
)
from .scene import SceneElement, build_scene_elements, load_element_base, paste_element, save_scene_json


def _load_scene(scene_png: Path, size: Tuple[int, int]) -> Image.Image:
    """Load composed scene for Shorts / fallbacks."""
    if scene_png.exists():
        im = Image.open(scene_png).convert("RGBA")
        return im.resize(size, Image.Resampling.LANCZOS)
    return Image.new("RGBA", size, (247, 244, 239, 255))


def _paper_base(size: Tuple[int, int]) -> Image.Image:
    frame = Image.new("RGBA", size, (247, 244, 239, 255))
    for i, alpha in enumerate((255, 235, 210)):
        plate = Image.new("RGBA", (size[0] - 80 - i * 18, size[1] - 100 - i * 22), (255, 252, 245, alpha))
        frame.alpha_composite(plate, (40 + i * 7, 50 + i * 10))
    return frame


def _elements_from_inputs(
    materials: Optional[Dict[str, Any]],
    cutouts: Optional[List[Dict]],
    size: Tuple[int, int],
) -> List[SceneElement]:
    by_id: Dict[str, str] = {}
    if cutouts:
        for c in cutouts:
            if c.get("id") and c.get("cutout"):
                by_id[c["id"]] = c["cutout"]
    if materials:
        return build_scene_elements(materials, by_id, size)
    # minimal fallback from cutout list
    fake_mats: Dict[str, Any] = {"template": "balanced_scene"}
    if cutouts:
        for c in cutouts:
            role = c.get("layer") or c.get("role") or "support"
            item = {
                "id": c.get("id"),
                "category": c.get("category") or "",
                "name": c.get("name") or "",
                "file": None,
                "role": "hero" if role in ("animals", "hero") else (
                    "environment" if role == "background" else (
                        "frame" if str(role).startswith("plant") else "decorative"
                    )
                ),
                "depth": "background" if role == "background" else (
                    "hero" if role in ("animals", "hero") else "midground"
                ),
                "visualWeight": 80 if role in ("animals", "hero") else 50,
                "anchor": ["center"],
                "scaleRange": [0.9, 1.05],
            }
            if role == "background":
                fake_mats["background"] = item
            elif role == "nature":
                fake_mats["nature"] = item
            elif role in ("animals", "hero"):
                fake_mats["hero"] = item
            elif str(role).startswith("plant"):
                fake_mats.setdefault("plants", []).append(item)
            elif role in ("fortune", "accent"):
                fake_mats["accent"] = item
            elif role == "season":
                fake_mats["season"] = item
    return build_scene_elements(fake_mats, by_id, size)


def render_scene_frame(
    elements: List[SceneElement],
    t: float,
    size: Tuple[int, int],
    duration: float,
    image_cache: Optional[Dict[str, Any]] = None,
) -> Image.Image:
    frame = _paper_base(size)
    cache = image_cache or {}
    for el in sorted(elements, key=lambda e: e.depth_index):
        st: AnimState = animate_element(el, t, duration_total=duration)
        if st.opacity <= 0.01:
            continue
        paste_element(
            frame,
            el,
            opacity=st.opacity,
            dx=st.dx,
            dy=st.dy,
            scale_mul=st.scale_mul,
            rotation_add=st.rotation_add,
            base_image=cache.get(el.asset_id),
        )
    return frame


def build_animated_frames(
    daily: Dict,
    size: Tuple[int, int],
    duration_sec: float,
    fps: int,
    elements: List[SceneElement],
    *,
    letterbox: Optional[Tuple[int, int]] = None,
    card_pace: float = 1.0,
) -> Tuple[List[Image.Image], Dict[str, Any]]:
    """
    Shared frame builder for GIF (square) and Shorts (optional 9:16 letterbox).
    letterbox = (width, height) of final canvas; scene square is centered.
    card_pace: 1.0 normal (MP4); 0.5 half card gaps (GIF).
    """
    yi = [to_traditional(x) for x in (daily.get("yi") or [])]
    ji = [to_traditional(x) for x in (daily.get("ji") or [])]
    tl = build_timeline(yi, ji, base=float(duration_sec), card_pace=card_pace)

    line1 = daily.get("dateLine1") or ""
    line2 = daily.get("dateLine2") or ""
    if not line1 or not line2:
        # fallback for older daily.json
        roc = daily.get("rocDate") or ""
        lunar = daily.get("lunarDate") or ""
        if not line1:
            if roc.startswith("中華民國") and lunar.startswith("農曆"):
                line1 = f"{roc}{lunar}"
            else:
                line1 = f"{roc} {lunar}".strip()
        if not line2:
            gz = daily.get("dayGanZhi") or ""
            animal = to_traditional(daily.get("dayAnimalHint") or "")
            yg = daily.get("yearGanZhi") or ""
            mg = daily.get("monthGanZhi") or ""
            if yg and mg and gz:
                line2 = f"{yg} [{animal}] 年{mg}月{gz}日" if animal else f"{yg}年{mg}月{gz}日"
            else:
                line2 = gz

    brand = "名序"
    slogan = "知名・知運・知人生"
    # Prefer explicit brand fields; fallback closingMessage "名序｜slogan"
    if daily.get("brandName"):
        brand = str(daily["brandName"]).strip() or brand
    if daily.get("brandTagline"):
        slogan = str(daily["brandTagline"]).strip() or slogan
    else:
        closing = str(daily.get("closingMessage") or "")
        if closing.startswith("名序") and "｜" in closing:
            parts = closing.split("｜", 1)
            brand = parts[0].strip() or brand
            slogan = parts[1].strip() or slogan

    date_card = render_date_card(line1, line2, size, top_y=int(size[1] * STACK_Y["date"]))
    yi_card = render_yi_ji_block("宜", yi, tone="yi", canvas_size=size, top_y=int(size[1] * STACK_Y["yi"]))
    ji_card = render_yi_ji_block("忌", ji, tone="ji", canvas_size=size, top_y=int(size[1] * STACK_Y["ji"]))

    level = to_traditional(str(daily.get("fortuneLevel") or "平"))
    theme = to_traditional(str(daily.get("mainTheme") or ""))
    secondary = to_traditional(str(daily.get("secondaryTheme") or ""))
    summary = daily.get("summaryText") or daily.get("shortMessage") or ""
    if not summary:
        summary = f"今日{level}" + (f"｜{theme}" if theme else "")
        if secondary and secondary != theme:
            summary = f"{summary}・{secondary}"
    # 字卡不宜太長
    summary = " ".join(str(summary).replace("\n", " ").split())
    if len(summary) > 28:
        summary = summary[:27] + "…"
    summary_card = render_summary_card(
        summary, canvas_size=size, top_y=int(size[1] * STACK_Y["summary"])
    )
    logo_card = render_logo_slogan_card(
        brand=brand, slogan=slogan, canvas_size=size, top_y=int(size[1] * STACK_Y["logo"])
    )

    image_cache = {el.asset_id: load_element_base(el) for el in elements}

    total = max(1, int(round(tl.duration * fps)))
    frames: List[Image.Image] = []
    for i in range(total):
        t = i / fps
        scene = render_scene_frame(elements, t, size, tl.duration, image_cache=image_cache)

        # Stack top→bottom; each card fades in then stays
        do = card_opacity_hold(t, tl.date_start)
        if do > 0:
            scene = apply_overlay(scene, date_card, do, scale=card_scale_pop(t, tl.date_start))

        yo = card_opacity_hold(t, tl.yi_start)
        if yo > 0:
            scene = apply_overlay(scene, yi_card, yo, scale=card_scale_pop(t, tl.yi_start))

        jo = card_opacity_hold(t, tl.ji_start)
        if jo > 0:
            scene = apply_overlay(scene, ji_card, jo, scale=card_scale_pop(t, tl.ji_start))

        so = card_opacity_hold(t, tl.summary_start)
        if so > 0:
            scene = apply_overlay(scene, summary_card, so, scale=card_scale_pop(t, tl.summary_start))

        lo = card_opacity_hold(t, tl.logo_start)
        if lo > 0:
            scene = apply_overlay(scene, logo_card, lo, scale=0.96 + 0.04 * lo)

        if letterbox and letterbox != size:
            canvas = Image.new("RGBA", letterbox, (247, 244, 239, 255))
            y0 = (letterbox[1] - size[1]) // 2
            x0 = (letterbox[0] - size[0]) // 2
            canvas.alpha_composite(scene, (max(0, x0), max(0, y0)))
            frames.append(canvas.convert("RGB"))
        else:
            frames.append(scene.convert("RGB"))

    meta = {"timeline": tl.to_dict(), "elementCount": len(elements)}
    return frames, meta


def build_gif_frames(
    scene_png: Path,
    daily: Dict,
    size: Tuple[int, int],
    duration_sec: int = 20,
    fps: int = 10,
    cutouts: Optional[List[Dict]] = None,
    materials: Optional[Dict[str, Any]] = None,
) -> List[Image.Image]:
    elements = _elements_from_inputs(materials, cutouts, size)
    frames, _ = build_animated_frames(
        daily, size, float(duration_sec), fps, elements, card_pace=0.5
    )
    if not frames and scene_png.exists():
        frames = [_load_scene(scene_png, size).convert("RGB")]
    return frames


def export_gif_webp(
    scene_png: Path,
    daily: Dict,
    gif_dir: Path,
    yyyymmdd: str,
    size: Tuple[int, int],
    duration_sec: int,
    fps: int,
    cutouts: Optional[List[Dict]] = None,
    materials: Optional[Dict[str, Any]] = None,
    scene_json_path: Optional[Path] = None,
) -> Dict[str, str]:
    gif_dir.mkdir(parents=True, exist_ok=True)
    elements = _elements_from_inputs(materials, cutouts, size)
    frames, meta = build_animated_frames(
        daily, size, float(duration_sec), fps, elements, card_pace=0.5
    )

    if scene_json_path:
        save_scene_json(
            scene_json_path,
            elements,
            {
                "date": yyyymmdd,
                "theme": daily.get("mainTheme"),
                "template": (materials or {}).get("template"),
                "animation": meta,
            },
        )
        anim_path = scene_json_path.parent / "animation.json"
        anim_path.write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")

    gif_path = gif_dir / f"daily_{yyyymmdd}.gif"
    webp_path = gif_dir / f"daily_{yyyymmdd}.webp"
    duration_ms = int(1000 / max(1, fps))
    frames[0].save(
        gif_path,
        save_all=True,
        append_images=frames[1:],
        duration=duration_ms,
        loop=0,
        optimize=False,
    )
    try:
        frames[0].save(
            webp_path,
            save_all=True,
            append_images=frames[1:],
            duration=duration_ms,
            loop=0,
            format="WEBP",
        )
    except Exception:
        frames[len(frames) // 2].save(webp_path, format="WEBP")
    out = {
        "gif": str(gif_path),
        "webp": str(webp_path),
        "durationSec": meta["timeline"]["duration"],
        "frames": frames,
        "elements": elements,
        "animationMeta": meta,
    }
    return out
