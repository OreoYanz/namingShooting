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

from . import ROOT, ensure_day_folders, load_json, load_settings, parse_date_arg, save_json
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
    load_dotenv(ROOT / ".env", override=True)
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

    # Background: theme cast (Hero / Plants / Nature / Accent) for Shorts V3.
    # GIF keeps current bg_only look — do not change square GIF composition.
    materials = pick_materials(d, themes["mainTheme"])
    gif_materials = {
        **materials,
        "template": "bg_only",
        "hero": None,
        "nature": None,
        "plants": [],
        "accent": None,
        "accents": [],
        "season": None,
    }

    cutouts = ensure_cutouts(materials, base / "cutout", base / "source")
    by_id = {c["id"]: c["cutout"] for c in cutouts if c.get("id")}
    # also map library files directly if cutout copy missed color-fill-less bg
    for it in (
        materials.get("hero"),
        materials.get("background"),
        materials.get("nature"),
        materials.get("accent"),
        materials.get("season"),
        *list(materials.get("plants") or []),
        *list(materials.get("accents") or []),
    ):
        if it and it.get("id") and it.get("file") and it["id"] not in by_id:
            by_id[it["id"]] = str(ROOT / it["file"])

    scene_png = base / "scene" / f"scene_{yyyymmdd}.png"
    media = settings["media"]
    gif_size = tuple(media["gifSize"])
    short_size = tuple(media["shortSize"])
    # Square scene preview follows GIF (bg_only) so existing GIF look stays
    elements = build_scene_elements(gif_materials, by_id, gif_size)
    compose_scene(cutouts, scene_png, size=gif_size, materials=gif_materials)
    save_scene_json(
        base / "scene" / "scene.json",
        elements,
        {
            "date": yyyymmdd,
            "theme": themes["mainTheme"],
            "template": gif_materials.get("template"),
            "profile": (materials.get("themeProfile") or {}).get("profileKey"),
            "shortsTemplate": materials.get("template"),
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
        materials=gif_materials,
        scene_json_path=base / "data" / "scene.json",
    )
    short_files = export_short(
        scene_png,
        daily,
        base / "short",
        yyyymmdd,
        short_size,
        media["durationSec"],
        media["fps"],
        cutouts=cutouts,
        materials=gif_materials,
        # MP4：正方形動畫 letterbox 成 9:16（與 GIF 同內容）
        square_frames=None,
    )
    # drop heavy frame list from return payload
    gif_files.pop("frames", None)
    gif_files.pop("elements", None)
    social_files = write_social(base / "social", copy)
    publish_path = write_publish(base / "publish", yyyymmdd, status="draft")

    mail_result = None
    try:
        from .mailer import send_daily_social_mail

        mp4 = short_files.get("mp4")
        mail_result = send_daily_social_mail(
            yyyymmdd,
            social_dir=base / "social",
            mp4_path=Path(mp4) if mp4 else None,
        )
    except Exception as e:
        mail_result = {"ok": False, "error": str(e)}

    generation = {
        "date": yyyymmdd,
        "generatedAt": datetime.now().isoformat(timespec="seconds"),
        "materials": materials,
        "cutouts": cutouts,
        "openaiCopy": bool(os.environ.get(settings["openai"]["apiKeyEnv"], "").strip()),
        "imageGenerationEnabled": bool(settings["openai"].get("enableImageGeneration")),
        "sceneEngine": {
            "template": gif_materials.get("template"),
            "heroId": (materials.get("hero") or {}).get("id"),
            "elementCount": len(elements),
        },
        "mail": mail_result,
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
        "gifLast": gif_files.get("last"),
        "shortMp4": short_files.get("mp4"),
        "shortPreview": short_files.get("preview"),
        "social": social_files,
        "publish": publish_path,
        "cutouts": [c["cutout"] for c in cutouts],
        "mail": mail_result,
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

    # 產生完成後自動寫入官網並 commit／push（可用 AUTO_PUBLISH_SITE=false 關閉）
    site_publish = None
    auto_flag = (os.getenv("AUTO_PUBLISH_SITE") or "true").strip().lower()
    if auto_flag not in ("0", "false", "no", "off"):
        try:
            from .publishers.git_push import commit_and_push_site
            from .publishers.site import publish_to_site

            site_res = publish_to_site(yyyymmdd)
            git_res = None
            if site_res.get("ok"):
                git_res = commit_and_push_site(yyyymmdd)
            site_publish = {"site": site_res, "git": git_res}
            # 更新 publish.json
            try:
                pub_path = Path(publish_path)
                pub = load_json(pub_path) if pub_path.exists() else {
                    "date": yyyymmdd,
                    "channels": {},
                    "confirmedAt": None,
                    "publishedAt": None,
                }
                pub.setdefault("channels", {})
                pub.setdefault("results", {})
                pub["channels"]["site"] = bool(site_res.get("ok"))
                pub["results"]["site"] = site_res
                if git_res is not None:
                    pub["results"]["git"] = git_res
                if site_res.get("ok"):
                    pub["status"] = "published"
                    pub["publishedAt"] = datetime.now().isoformat(timespec="seconds")
                    if not pub.get("confirmedAt"):
                        pub["confirmedAt"] = pub["publishedAt"]
                else:
                    pub["status"] = pub.get("status") or "draft"
                pub["autoPublished"] = True
                save_json(pub_path, pub)
            except Exception as pe:
                site_publish["publishJsonError"] = str(pe)
        except Exception as e:
            site_publish = {"ok": False, "error": str(e)}

    # 自動上傳 YouTube Shorts（需先 youtube_auth.py；AUTO_PUBLISH_YOUTUBE=false 可關）
    youtube_publish = None
    yt_flag = (os.getenv("AUTO_PUBLISH_YOUTUBE") or "true").strip().lower()
    if yt_flag not in ("0", "false", "no", "off"):
        try:
            from .publishers.youtube import publish_day as publish_youtube_day
            from .publishers.youtube import youtube_ready

            if youtube_ready():
                youtube_publish = publish_youtube_day(yyyymmdd)
            else:
                youtube_publish = {
                    "ok": False,
                    "skipped": True,
                    "error": "尚未設定 YouTube（見 docs/YOUTUBE_SETUP.md）",
                }
            # 寫入 publish.json
            try:
                pub_path = Path(publish_path)
                pub = load_json(pub_path) if pub_path.exists() else {
                    "date": yyyymmdd,
                    "channels": {},
                    "results": {},
                }
                pub.setdefault("channels", {})
                pub.setdefault("results", {})
                pub["channels"]["youtube"] = bool(youtube_publish.get("ok"))
                pub["results"]["youtube"] = youtube_publish
                save_json(pub_path, pub)
            except Exception as pe:
                if isinstance(youtube_publish, dict):
                    youtube_publish["publishJsonError"] = str(pe)
        except Exception as e:
            youtube_publish = {"ok": False, "error": str(e)}

    generation["sitePublish"] = site_publish
    generation["youtubePublish"] = youtube_publish
    save_json(base / "data" / "generation.json", generation)
    files["sitePublish"] = site_publish
    files["youtubePublish"] = youtube_publish

    return {
        "ok": True,
        "date": yyyymmdd,
        "dir": str(base),
        "daily": daily,
        "fortune": fortune,
        "materials": materials,
        "files": files,
        "manifest": manifest,
        "mail": mail_result,
        "sitePublish": site_publish,
        "youtubePublish": youtube_publish,
    }
