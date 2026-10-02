"""Generate Daily pipeline."""
from __future__ import annotations

import os
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, Optional

try:
    from dotenv import load_dotenv
except ImportError:  # optional
    def load_dotenv(*args, **kwargs):
        return False

from . import ROOT, ensure_day_folders, load_settings, parse_date_arg, save_json
from .ai_copy import choose_theme, generate_social_copy
from .cutout_from_pool import ensure_cutouts
from .fortune import generate_fortune
from .gif_builder import export_gif_webp
from .manifest import build_manifest, write_publish
from .materials import pick_materials, recently_used_themes, record_usage
from .scene import build_scene_elements, compose_scene, save_scene_json
from .short_builder import export_short
from .social import write_social


def generate_daily(
    date_str: str,
    force: bool = False,
    force_theme: Optional[str] = None,
    use_theme_set: Optional[bool] = None,
) -> Dict[str, Any]:
    load_dotenv(ROOT / ".env")
    d, yyyymmdd = parse_date_arg(date_str)
    settings = load_settings()
    base = ensure_day_folders(yyyymmdd)

    fortune = generate_fortune(d)
    save_json(base / "data" / "fortune.json", fortune)

    blocked_themes = recently_used_themes(d, settings["reuseRules"]["sameThemeMinDays"])
    themes = choose_theme(fortune, blocked_themes)
    if force_theme:
        themes = {
            "mainTheme": force_theme,
            "secondaryTheme": themes.get("secondaryTheme") or force_theme,
        }

    # Background: random pick from materials/assets/Background/
    materials = pick_materials(d, themes["mainTheme"])

    cutouts = ensure_cutouts(materials, base / "cutout", base / "source")
    by_id = {c["id"]: c["cutout"] for c in cutouts if c.get("id")}
    # also map library files directly if cutout copy missed color-fill-less bg
    for it in (
        materials.get("hero"),
        materials.get("background"),
        materials.get("nature"),
        materials.get("accent"),
        *list(materials.get("plants") or []),
        *list(materials.get("accents") or []),
    ):
        if it and it.get("id") and it.get("file") and it["id"] not in by_id:
            by_id[it["id"]] = str(ROOT / it["file"])

    scene_png = base / "scene" / f"scene_{yyyymmdd}.png"
    media = settings["media"]
    gif_size = tuple(media["gifSize"])
    elements = build_scene_elements(materials, by_id, gif_size)
    compose_scene(cutouts, scene_png, size=gif_size, materials=materials)
    save_scene_json(
        base / "scene" / "scene.json",
        elements,
        {
            "date": yyyymmdd,
            "theme": themes["mainTheme"],
            "template": materials.get("template"),
            "profile": (materials.get("themeProfile") or {}).get("profileKey"),
        },
    )

    daily = {
        "date": d.isoformat(),
        "dateKey": yyyymmdd,
        "rocDate": fortune.get("rocDate"),
        "lunarDate": fortune.get("lunarDate"),
        "dateLine1": fortune.get("dateLine1"),
        "dateLine2": fortune.get("dateLine2"),
        "yearGanZhi": fortune.get("yearGanZhi"),
        "monthGanZhi": fortune.get("monthGanZhi"),
        "dayGanZhi": fortune.get("dayGanZhi"),
        "dayAnimalHint": fortune.get("dayAnimalHint"),
        "fortuneLevel": fortune.get("fortuneLevel"),
        "fortuneScore": fortune.get("fortuneScore"),
        "mainTheme": themes["mainTheme"],
        "secondaryTheme": themes["secondaryTheme"],
        "yi": fortune.get("yi"),
        "ji": fortune.get("ji"),
        "luckyElements": fortune.get("luckyElements"),
        "luckyColor": fortune.get("luckyColor"),
        "luckyDirection": fortune.get("luckyDirection"),
        "luckyTime": fortune.get("luckyTime"),
        "shortMessage": f"今日{fortune.get('fortuneLevel')}，以「{themes['mainTheme']}」為題，慢慢把好運疊起來。",
        "summaryText": "",
        "closingMessage": "名序｜新生兒命名 ‧ 專業改名 ‧ 流年運勢",
        "brandName": (settings.get("brand") or {}).get("name") or "名序",
        "brandTagline": (settings.get("brand") or {}).get("tagline") or "新生兒命名 ‧ 專業改名 ‧ 流年運勢",
        "ctaLine": (settings.get("cta") or {}).get("line") or settings["cta"]["line"],
        "ctaProduct": (settings.get("cta") or {}).get("product") or settings["cta"]["product"],
        "ctaUrl": (settings.get("cta") or {}).get("url") or settings["cta"]["url"],
    }

    # Skip remote copy when OPENAI key missing or DAILYPAPERCUT_SKIP_AI=1 (faster local tests)
    if os.environ.get("DAILYPAPERCUT_SKIP_AI", "").strip() in ("1", "true", "yes"):
        from .ai_copy import _fallback_copy

        copy = _fallback_copy(daily, settings)
    else:
        copy = generate_social_copy(daily, fortune)
    daily["shortMessage"] = copy.get("shortMessage") or daily["shortMessage"]
    daily["closingMessage"] = copy.get("closingMessage") or daily["closingMessage"]
    daily["summaryText"] = copy.get("summaryText") or daily.get("summaryText") or ""
    if not daily["summaryText"]:
        from .ai_copy import _fallback_summary

        daily["summaryText"] = _fallback_summary(daily)
    save_json(base / "data" / "daily.json", daily)

    gif_files = export_gif_webp(
        scene_png,
        daily,
        base / "gif",
        yyyymmdd,
        gif_size,
        media["durationSec"],
        media["fps"],
        cutouts=cutouts,
        materials=materials,
        scene_json_path=base / "data" / "scene.json",
    )
    short_files = export_short(
        scene_png,
        daily,
        base / "short",
        yyyymmdd,
        tuple(media["shortSize"]),
        media["durationSec"],
        media["fps"],
        cutouts=cutouts,
        materials=materials,
        # MP4 獨立產幀（20s 正常字卡節奏），不共用 GIF 加速幀
        square_frames=None,
    )
    # drop heavy frame list from return payload
    gif_files.pop("frames", None)
    gif_files.pop("elements", None)
    social_files = write_social(base / "social", copy)
    publish_path = write_publish(base / "publish", yyyymmdd, status="draft")

    generation = {
        "date": yyyymmdd,
        "generatedAt": datetime.now().isoformat(timespec="seconds"),
        "materials": materials,
        "cutouts": cutouts,
        "openaiCopy": bool(os.environ.get(settings["openai"]["apiKeyEnv"], "").strip()),
        "imageGenerationEnabled": bool(settings["openai"].get("enableImageGeneration")),
        "sceneEngine": {
            "template": materials.get("template"),
            "heroId": (materials.get("hero") or {}).get("id"),
            "elementCount": len(elements),
        },
    }
    save_json(base / "data" / "generation.json", generation)

    files = {
        "dailyJson": str(base / "data" / "daily.json"),
        "fortuneJson": str(base / "data" / "fortune.json"),
        "generationJson": str(base / "data" / "generation.json"),
        "scenePng": str(scene_png),
        "sceneJpg": str(scene_png.with_suffix(".jpg")),
        "sceneJson": str(base / "scene" / "scene.json"),
        "gif": gif_files.get("gif"),
        "webp": gif_files.get("webp"),
        "shortMp4": short_files.get("mp4"),
        "shortPreview": short_files.get("preview"),
        "social": social_files,
        "publish": publish_path,
        "cutouts": [c["cutout"] for c in cutouts],
    }
    manifest = build_manifest(yyyymmdd, fortune, daily, materials, files)
    manifest_path = base / "manifest.json"
    save_json(manifest_path, manifest)
    files["manifest"] = str(manifest_path)

    record_usage(
        yyyymmdd,
        themes["mainTheme"],
        (materials.get("hero") or materials.get("animals") or {}).get("id", ""),
        materials["allIds"],
        background_id=(materials.get("background") or {}).get("id"),
    )

    return {
        "ok": True,
        "date": yyyymmdd,
        "dir": str(base),
        "daily": daily,
        "fortune": fortune,
        "materials": materials,
        "files": files,
        "manifest": manifest,
    }
