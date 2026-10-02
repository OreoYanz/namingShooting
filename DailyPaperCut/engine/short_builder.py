"""Shorts 9:16 MP4 — same Scene / Animation Engine as GIF; letterbox full 1:1 (no side crop)."""
from __future__ import annotations

import shutil
import subprocess
import tempfile
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

from .gif_builder import _load_scene, build_animated_frames, _elements_from_inputs

# Match gif_builder letterbox cream so Shorts pad matches brand paper tone
_LETTERBOX_RGB = (247, 244, 239)


def letterbox_square_to_vertical(square_frames, size: Tuple[int, int]):
    """
    Fit full 1:1 frames into 9:16 without cropping.
    Scale to fit width, center vertically on cream bars (top/bottom).
    Example: 1080×1080 → centered on 1080×1920.
    """
    from PIL import Image

    out_w, out_h = size
    out = []
    for fr in square_frames:
        im = fr.convert("RGB") if hasattr(fr, "convert") else fr
        sw, sh = im.size
        # Contain: never crop; scale so the square fits inside the vertical canvas
        scale = min(out_w / float(sw), out_h / float(sh))
        nw = max(1, int(round(sw * scale)))
        nh = max(1, int(round(sh * scale)))
        if (nw, nh) != (sw, sh):
            im = im.resize((nw, nh), Image.Resampling.LANCZOS)
        canvas = Image.new("RGB", size, _LETTERBOX_RGB)
        x0 = (out_w - nw) // 2
        y0 = (out_h - nh) // 2
        canvas.paste(im, (x0, y0))
        out.append(canvas)
    return out


# backward-compatible aliases (old crop path was cutting content; now letterbox)
def crop_square_to_vertical(square_frames, size: Tuple[int, int]):
    return letterbox_square_to_vertical(square_frames, size)


def letterbox_frames(square_frames, size: Tuple[int, int], daily: Optional[Dict] = None):
    return letterbox_square_to_vertical(square_frames, size)


def build_short_frames(
    scene_png: Path,
    daily: Dict,
    size: Tuple[int, int],
    duration_sec: int = 20,
    fps: int = 10,
    cutouts: Optional[List[Dict]] = None,
    materials: Optional[Dict[str, Any]] = None,
):
    # Animate on square (same as GIF content), then letterbox into 9:16
    scene_size = (size[0], size[0])
    elements = _elements_from_inputs(materials, cutouts, scene_size)
    frames, _meta = build_animated_frames(
        daily,
        scene_size,
        float(duration_sec),
        fps,
        elements,
        letterbox=size,
        card_pace=1.0,
    )
    if not frames:
        from PIL import Image

        scene = _load_scene(scene_png, scene_size)
        frames = letterbox_square_to_vertical([scene.convert("RGB")], size)
    return frames


def _write_mp4_imageio(frames, mp4: Path, fps: int) -> bool:
    try:
        import imageio.v2 as imageio
        import numpy as np

        writer = imageio.get_writer(
            str(mp4),
            fps=fps,
            codec="libx264",
            quality=8,
            pixelformat="yuv420p",
            macro_block_size=None,
        )
        for fr in frames:
            writer.append_data(np.asarray(fr))
        writer.close()
        return mp4.exists() and mp4.stat().st_size > 0
    except Exception:
        return False


def _write_mp4_ffmpeg(frames, mp4: Path, fps: int) -> bool:
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        try:
            import imageio_ffmpeg

            ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
        except Exception:
            return False
    with tempfile.TemporaryDirectory() as tmp:
        tmp_path = Path(tmp)
        for i, fr in enumerate(frames):
            fr.save(tmp_path / f"frame_{i:04d}.jpg", quality=92)
        cmd = [
            ffmpeg,
            "-y",
            "-framerate",
            str(fps),
            "-i",
            str(tmp_path / "frame_%04d.jpg"),
            "-c:v",
            "libx264",
            "-pix_fmt",
            "yuv420p",
            "-movflags",
            "+faststart",
            str(mp4),
        ]
        try:
            subprocess.run(cmd, check=True, capture_output=True)
            return mp4.exists() and mp4.stat().st_size > 0
        except Exception:
            return False


def export_short(
    scene_png: Path,
    daily: Dict,
    short_dir: Path,
    yyyymmdd: str,
    size: Tuple[int, int],
    duration_sec: int,
    fps: int,
    cutouts: Optional[List[Dict]] = None,
    materials: Optional[Dict[str, Any]] = None,
    square_frames: Optional[List] = None,
) -> Dict[str, str]:
    short_dir.mkdir(parents=True, exist_ok=True)
    if square_frames:
        frames = letterbox_square_to_vertical(square_frames, size)
    else:
        frames = build_short_frames(
            scene_png, daily, size, duration_sec, fps, cutouts=cutouts, materials=materials
        )
    preview = short_dir / f"daily_{yyyymmdd}_preview.jpg"
    mp4 = short_dir / f"daily_{yyyymmdd}.mp4"
    frames[min(len(frames) - 1, int(len(frames) * 0.7))].save(preview, quality=92)

    ok = _write_mp4_imageio(frames, mp4, fps) or _write_mp4_ffmpeg(frames, mp4, fps)
    if not ok:
        note = short_dir / f"daily_{yyyymmdd}_mp4_pending.txt"
        note.write_text(
            "MP4 編碼失敗。請確認已安裝：pip install imageio imageio-ffmpeg numpy\n",
            encoding="utf-8",
        )
        return {"mp4": str(note), "preview": str(preview)}
    pending = short_dir / f"daily_{yyyymmdd}_mp4_pending.txt"
    if pending.exists():
        pending.unlink()
    return {"mp4": str(mp4), "preview": str(preview)}
