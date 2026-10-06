"""Orchestrate Facebook / Instagram / Threads publish for a daily pack."""
from __future__ import annotations

import time
from pathlib import Path
from typing import Any, Dict, Iterable, List, Set

from .. import day_dir
from . import facebook, instagram, threads
from .git_push import commit_and_push_site
from .meta_config import load_meta_config
from .site import publish_to_site


def _read_caption(base: Path, channel: str) -> str:
    p = base / "social" / f"{channel}.txt"
    if p.exists():
        return p.read_text(encoding="utf-8").strip()
    return ""


def _media_paths(yyyymmdd: str) -> Dict[str, Any]:
    base = day_dir(yyyymmdd)
    last = base / "gif" / f"daily_{yyyymmdd}_last.jpg"
    preview = base / "short" / f"daily_{yyyymmdd}_preview.jpg"
    mp4 = base / "short" / f"daily_{yyyymmdd}.mp4"
    scene = base / "scene" / f"scene_{yyyymmdd}.jpg"

    image_local = last if last.exists() else (preview if preview.exists() else scene)
    video_local = mp4 if mp4.exists() else None

    image_public = f"assets/daily/daily_{yyyymmdd}_last.jpg"
    if image_local == preview:
        image_public = f"assets/daily/daily_{yyyymmdd}_preview.jpg"
    elif image_local == scene:
        image_public = f"assets/daily/scene_{yyyymmdd}.jpg"

    video_public = f"assets/daily/daily_{yyyymmdd}.mp4"
    cover_public = f"assets/daily/daily_{yyyymmdd}_preview.jpg"
    if not preview.exists():
        cover_public = image_public

    return {
        "base": base,
        "image_local": image_local,
        "video_local": video_local,
        "image_public": image_public if image_local and image_local.exists() else "",
        "video_public": video_public if video_local else "",
        "cover_public": cover_public,
    }


def ensure_public_media(yyyymmdd: str) -> Dict[str, Any]:
    """Publish day media to site/docs and push so Meta can fetch HTTPS URLs."""
    out: Dict[str, Any] = {"ok": False, "site": None, "git": None}
    try:
        out["site"] = publish_to_site(yyyymmdd)
    except Exception as e:
        out["error"] = f"site: {e}"
        return out
    if not out["site"].get("ok"):
        out["error"] = out["site"].get("error") or "官網媒體同步失敗"
        return out
    try:
        out["git"] = commit_and_push_site(yyyymmdd)
    except Exception as e:
        out["error"] = f"git: {e}"
        return out
    if not out["git"].get("ok"):
        out["error"] = out["git"].get("error") or "git push 失敗"
        return out
    # GitHub Pages 部署／CDN 需要一點時間，給 Meta 抓圖／影緩衝
    if not out["git"].get("skipped"):
        time.sleep(12)
    out["ok"] = True
    return out


def publish_social_channels(
    yyyymmdd: str,
    channels: Iterable[str],
    *,
    ensure_media: bool = True,
) -> Dict[str, Any]:
    wanted: Set[str] = {c.strip().lower() for c in channels if c and c.strip()}
    social = wanted & {"facebook", "instagram", "threads"}
    if not social:
        return {"ok": True, "skipped": True, "message": "未選擇社群頻道"}

    cfg = load_meta_config()
    media = _media_paths(yyyymmdd)
    base: Path = media["base"]

    prep: Dict[str, Any] = {"ok": True}
    if ensure_media:
        prep = ensure_public_media(yyyymmdd)
        if not prep.get("ok"):
            return {
                "ok": False,
                "error": prep.get("error") or "公開媒體準備失敗",
                "prep": prep,
                "channels": {},
            }

    results: Dict[str, Any] = {}
    errors: List[str] = []

    if "facebook" in social:
        caption = _read_caption(base, "facebook")
        results["facebook"] = facebook.publish_day(
            cfg,
            caption=caption,
            image_public_path=media["image_public"],
            video_public_path=media["video_public"],
            local_image=media["image_local"],
            local_video=media["video_local"],
            title=f"名序｜每日吉祥 {yyyymmdd}",
        )
        if not results["facebook"].get("ok"):
            errors.append("facebook: " + str(results["facebook"].get("error") or "失敗"))

    if "instagram" in social:
        caption = _read_caption(base, "instagram")
        results["instagram"] = instagram.publish_day(
            cfg,
            caption=caption,
            image_public_path=media["image_public"],
            video_public_path=media["video_public"],
            cover_public_path=media["cover_public"],
        )
        if not results["instagram"].get("ok"):
            errors.append("instagram: " + str(results["instagram"].get("error") or "失敗"))

    if "threads" in social:
        caption = _read_caption(base, "threads")
        results["threads"] = threads.publish_day(
            cfg,
            caption=caption,
            image_public_path=media["image_public"],
            video_public_path=media["video_public"],
        )
        if not results["threads"].get("ok"):
            errors.append("threads: " + str(results["threads"].get("error") or "失敗"))

    any_ok = any(r.get("ok") for r in results.values())
    return {
        "ok": any_ok,
        "prep": prep,
        "media": {
            "image_public": media["image_public"],
            "video_public": media["video_public"],
            "cover_public": media["cover_public"],
        },
        "config": cfg.summary(),
        "channels": results,
        "error": "；".join(errors) if errors and not any_ok else (None if any_ok else "社群發布失敗"),
        "partial_errors": errors,
    }
