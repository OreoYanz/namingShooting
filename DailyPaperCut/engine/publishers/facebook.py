"""Facebook Page: photo post + Reels."""
from __future__ import annotations

from pathlib import Path
from typing import Any, Dict, Optional

from .meta_config import MetaConfig, public_asset_url
from .meta_http import graph_error_message, request_json


def _base(cfg: MetaConfig) -> str:
    return f"https://graph.facebook.com/{cfg.graph_version}"


def publish_photo(
    cfg: MetaConfig,
    *,
    image_url: str,
    caption: str,
) -> Dict[str, Any]:
    if not cfg.page_id or not cfg.page_token:
        return {"ok": False, "error": "缺少 META_PAGE_ID 或 META_PAGE_ACCESS_TOKEN"}

    url = f"{_base(cfg)}/{cfg.page_id}/photos"
    ok, payload, status = request_json(
        "POST",
        url,
        data={
            "url": image_url,
            "caption": caption or "",
            "published": "true",
            "access_token": cfg.page_token,
        },
    )
    if not ok:
        return {
            "ok": False,
            "kind": "photo",
            "http": status,
            "error": graph_error_message(payload),
            "raw": payload,
        }
    return {
        "ok": True,
        "kind": "photo",
        "id": payload.get("id") or payload.get("post_id"),
        "post_id": payload.get("post_id"),
        "raw": payload,
    }


def publish_reel(
    cfg: MetaConfig,
    *,
    video_url: str,
    description: str,
    title: Optional[str] = None,
) -> Dict[str, Any]:
    """
    Facebook Reels via video_reels upload session + finish.
    Requires a publicly reachable video_url.
    """
    if not cfg.page_id or not cfg.page_token:
        return {"ok": False, "error": "缺少 META_PAGE_ID 或 META_PAGE_ACCESS_TOKEN"}

    # 1) start upload session
    start_url = f"{_base(cfg)}/{cfg.page_id}/video_reels"
    ok, start, status = request_json(
        "POST",
        start_url,
        data={
            "upload_phase": "start",
            "access_token": cfg.page_token,
        },
    )
    if not ok:
        return {
            "ok": False,
            "kind": "reel",
            "step": "start",
            "http": status,
            "error": graph_error_message(start),
            "raw": start,
        }

    video_id = start.get("video_id")
    upload_url = start.get("upload_url")
    if not video_id:
        return {
            "ok": False,
            "kind": "reel",
            "step": "start",
            "error": "未取得 video_id",
            "raw": start,
        }

    # 2) host / file_url upload (Meta pulls from public URL)
    # Prefer file_url on the upload host when available; else finish with video_url.
    if upload_url:
        ok_up, up_payload, up_status = request_json(
            "POST",
            upload_url,
            data={
                "file_url": video_url,
                "access_token": cfg.page_token,
            },
            timeout=300,
        )
        # Some environments expect header-based rupload; file_url is the simpler path.
        if not ok_up and "error" in (up_payload or {}):
            # continue to finish — some tokens accept video_state finish with video_url only
            pass
        elif not ok_up:
            return {
                "ok": False,
                "kind": "reel",
                "step": "upload",
                "http": up_status,
                "error": graph_error_message(up_payload, "Reels 上傳失敗"),
                "raw": up_payload,
            }

    # 3) finish / publish
    finish_data = {
        "upload_phase": "finish",
        "video_id": video_id,
        "video_state": "PUBLISHED",
        "description": description or "",
        "access_token": cfg.page_token,
    }
    if title:
        finish_data["title"] = title
    # When file_url upload was skipped/failed, pass video_url for host ingest if supported
    finish_data["video_url"] = video_url

    ok_f, finish, f_status = request_json(
        "POST",
        start_url,
        data=finish_data,
        timeout=300,
    )
    if not ok_f:
        # Fallback: Page videos endpoint (may land as regular video / reel depending on account)
        ok_v, vpay, vstatus = request_json(
            "POST",
            f"{_base(cfg)}/{cfg.page_id}/videos",
            data={
                "file_url": video_url,
                "description": description or "",
                "published": "true",
                "access_token": cfg.page_token,
            },
            timeout=300,
        )
        if ok_v:
            return {
                "ok": True,
                "kind": "reel",
                "mode": "videos_fallback",
                "id": vpay.get("id"),
                "raw": {"finish_error": finish, "videos": vpay},
            }
        return {
            "ok": False,
            "kind": "reel",
            "step": "finish",
            "http": f_status,
            "error": graph_error_message(finish) + " / " + graph_error_message(vpay),
            "raw": {"finish": finish, "videos": vpay},
        }

    return {
        "ok": True,
        "kind": "reel",
        "id": finish.get("post_id") or finish.get("id") or video_id,
        "video_id": video_id,
        "raw": finish,
    }


def publish_day(
    cfg: MetaConfig,
    *,
    caption: str,
    image_public_path: str = "",
    video_public_path: str = "",
    local_image: Optional[Path] = None,
    local_video: Optional[Path] = None,
    title: str = "名序｜每日吉祥",
) -> Dict[str, Any]:
    results: Dict[str, Any] = {"ok": False, "channel": "facebook", "posts": {}}

    image_url = public_asset_url(cfg, image_public_path) if image_public_path else ""
    video_url = public_asset_url(cfg, video_public_path) if video_public_path else ""

    if image_url:
        results["posts"]["photo"] = publish_photo(cfg, image_url=image_url, caption=caption)
    elif local_image and local_image.exists():
        # multipart fallback
        url = f"{_base(cfg)}/{cfg.page_id}/photos"
        with local_image.open("rb") as fh:
            ok, payload, status = request_json(
                "POST",
                url,
                data={
                    "caption": caption or "",
                    "published": "true",
                    "access_token": cfg.page_token,
                },
                files={"source": (local_image.name, fh, "image/jpeg")},
                timeout=180,
            )
        results["posts"]["photo"] = (
            {"ok": True, "kind": "photo", "id": payload.get("id"), "raw": payload}
            if ok
            else {
                "ok": False,
                "kind": "photo",
                "http": status,
                "error": graph_error_message(payload),
                "raw": payload,
            }
        )
    else:
        results["posts"]["photo"] = {"ok": False, "error": "沒有可用的靜態圖"}

    if video_url:
        results["posts"]["reel"] = publish_reel(
            cfg, video_url=video_url, description=caption, title=title
        )
    elif local_video and local_video.exists():
        results["posts"]["reel"] = {
            "ok": False,
            "error": "Facebook Reels 需公開 video URL；請先發布官網（含 MP4）",
            "local": str(local_video),
        }
    else:
        results["posts"]["reel"] = {"ok": False, "error": "沒有可用的 MP4／Reels"}

    photo_ok = bool(results["posts"].get("photo", {}).get("ok"))
    reel_ok = bool(results["posts"].get("reel", {}).get("ok"))
    results["ok"] = photo_ok or reel_ok
    results["image_url"] = image_url
    results["video_url"] = video_url
    if not results["ok"]:
        errs = [
            results["posts"].get("photo", {}).get("error"),
            results["posts"].get("reel", {}).get("error"),
        ]
        results["error"] = "；".join(e for e in errs if e) or "Facebook 發布失敗"
    return results
