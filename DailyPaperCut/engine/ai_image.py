"""Image / cutout generation. Uses OpenAI images when enabled; else Pillow placeholders."""
from __future__ import annotations

import os
from pathlib import Path
from typing import Any, Dict, List

from . import load_settings, load_style_bible


def _pillow_placeholder(path: Path, title: str, color: tuple) -> None:
    from PIL import Image, ImageDraw, ImageFont

    path.parent.mkdir(parents=True, exist_ok=True)
    img = Image.new("RGBA", (768, 768), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # layered paper discs
    for i, alpha in enumerate((40, 70, 110)):
        r = 280 - i * 36
        c = (*color, alpha)
        draw.ellipse([384 - r, 384 - r, 384 + r, 384 + r], fill=c)
    # soft inner shape
    draw.rounded_rectangle([220, 260, 548, 520], radius=48, fill=(*color, 200))
    try:
        font = ImageFont.truetype("msjh.ttc", 36)
    except Exception:
        font = ImageFont.load_default()
    # Spec: no text in consumer AI art — placeholders keep tiny internal label only in source/
    # For cutout used in scene we avoid glyphs: draw abstract marks instead of title text.
    draw.ellipse([340, 340, 428, 428], fill=(255, 255, 255, 180))
    img.save(path)


def ensure_cutouts(
    materials: Dict[str, Any],
    cutout_dir: Path,
    source_dir: Path,
) -> List[Dict[str, str]]:
    settings = load_settings()
    style = load_style_bible()
    enable_ai = bool(settings["openai"].get("enableImageGeneration"))
    api_key = os.environ.get(settings["openai"]["apiKeyEnv"], "").strip()

    pack = []
    roles = [("hero", materials["hero"])]
    for s in materials["supports"]:
        roles.append(("support", s))
    roles.append(("environment", materials["environment"]))
    roles.append(("auspicious", materials["auspicious"]))

    palette = {
        "hero": (90, 150, 200),
        "support": (120, 170, 110),
        "environment": (180, 160, 120),
        "auspicious": (210, 170, 70),
    }

    for role, item in roles:
        mid = item["id"]
        out = cutout_dir / f"{mid}.png"
        src = source_dir / f"{mid}_source.png"
        if not out.exists():
            if enable_ai and api_key:
                # Reserved: call OpenAI Images API and save transparent PNG
                # For phase-1 default we still write placeholder to keep pipeline offline-capable.
                _pillow_placeholder(out, item.get("name", mid), palette.get(role, (150, 150, 150)))
                _pillow_placeholder(src, item.get("name", mid), palette.get(role, (150, 150, 150)))
            else:
                _pillow_placeholder(out, item.get("name", mid), palette.get(role, (150, 150, 150)))
                _pillow_placeholder(src, item.get("name", mid), palette.get(role, (150, 150, 150)))
        pack.append(
            {
                "id": mid,
                "role": role,
                "name": item.get("name", mid),
                "cutout": str(out),
                "source": str(src),
                "style": style.get("style"),
            }
        )
    return pack
