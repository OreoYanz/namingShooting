"""Shorts 9:16 MP4 — same Scene / Animation Engine as GIF; crop 1:1 sides to fill vertical."""
from __future__ import annotations

import shutil
import subprocess
import tempfile
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

from .gif_builder import _load_scene, build_animated_frames, _elements_from_inputs


def crop_square_to_vertical(square_frames, size: Tuple[int, int]):
    """
    Fit 1:1 frames into 9:16 by scaling to full height, then cropping left/right.
    Example: 1080×1080 → scale to 1920×1920 → center-crop to 1080×1920.
    """
    from PIL import Image

    out_w, out_h = size
    out = []
    for fr in square_frames:
        im = fr.convert("RGB") if hasattr(fr, "convert") else fr
        # scale so height fills 9:16 canvas
        scale = out_h / float(im.size[1])
        nw = max(out_w, int(round(im.size[0] * scale)))
        nh = out_h
        scaled = im.resize((nw, nh), Image.Resampling.LANCZOS)
        left = max(0, (nw - out_w) // 2)
        cropped = scaled.crop((left, 0, left + out_w, out_h))
        if cropped.size != size:
            cropped = cropped.resize(size, Image.Resampling.LANCZOS)
        out.append(cropped)
    return out


# backward-compatible alias
def letterbox_frames(square_frames, size: Tuple[int, int], daily: Optional[Dict] = None):
    return crop_square_to_vertical(square_frames, size)


def build_short_frames(
    scene_png: Path,
    daily: Dict,
    size: Tuple[int, int],
    duration_sec: int = 20,
    fps: int = 10,
    cutouts: Optional[List[Dict]] = None,
    materials: Optional[Dict[str, Any]] = None,
):
    # Animate on square, then crop left/right into 9:16
    scene_size = (size[0], size[0])
    elements = _elements_from_inputs(materials, cutouts, scene_size)
    frames, _meta = build_animated_frames(
        daily,
        scene_size,
        float(duration_sec),
        fps,
        elements,
        letterbox=None,
        card_pace=1.0,
    )
    if not frames:
        from PIL import Image

        scene = _load_scene(scene_png, scene_size)
        frames = [scene.convert("RGB")]
    return crop_square_to_vertical(frames, size)


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
        frames = crop_square_to_vertical(square_frames, size)
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
