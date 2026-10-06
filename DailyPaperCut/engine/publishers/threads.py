"""Threads API: image post + video (Reels-like) post."""
from __future__ import annotations

import os
from typing import Any, Dict

from .meta_config import MetaConfig, public_asset_url
from .meta_http import graph_error_message, poll_until, request_json

THREADS_HOST = "https://graph.threads.net"


def _base(cfg: MetaConfig) -> str:
    # Threads API uses graph.threads.net; default v1.0
    ver = (os.getenv("THREADS_GRAPH_VERSION") or "v1.0").strip() or "v1.0"
    if not ver.startswith("v"):
        ver = "v1.0"
    return f"{THREADS_HOST}/{ver}"


def _container_status(cfg: MetaConfig, creation_id: str) -> Dict[str, Any]:
    ok, payload, _ = request_json(
        "GET",
        f"{_base(cfg)}/{creation_id}",
        params={
            "fields": "status,error_message,id",
            "access_token": cfg.threads_token,
        },
    )
    if not ok:
        return {"status": "ERROR", "error": graph_error_message(payload), "raw": payload}
    # Normalize to status_code for poll_until
    status = str(payload.get("status") or "").upper()
    out = dict(payload)
    out["status_code"] = status
    if status in ("ERROR", "EXPIRED") and payload.get("error_message"):
        out["error"] = payload.get("error_message")
    return out


def _create_and_publish(
    cfg: MetaConfig,
    *,
    container_data: Dict[str, Any],
    kind: str,
) -> Dict[str, Any]:
    if not cfg.threads_user_id or not cfg.threads_token:
        return {
            "ok": False,
            "kind": kind,
            "error": "缺少 THREADS_USER_ID 或 THREADS_ACCESS_TOKEN",
        }

    data = dict(container_data)
    data["access_token"] = cfg.threads_token
    ok, created, status = request_json(
        "POST",
        f"{_base(cfg)}/{cfg.threads_user_id}/threads",
        data=data,
        timeout=180,
    )
    if not ok:
        return {
            "ok": False,
            "kind": kind,
            "step": "container",
            "http": status,
            "error": graph_error_message(created),
            "raw": created,
        }

    creation_id = created.get("id")
    if not creation_id:
        return {
            "ok": False,
            "kind": kind,
            "step": "container",
            "error": "未取得 creation_id",
            "raw": created,
        }

    # TEXT containers are often immediately publishable; media may need wait
    media_type = str(container_data.get("media_type") or "TEXT").upper()
    if media_type in ("IMAGE", "VIDEO"):
        polled = poll_until(
            lambda: _container_status(cfg, creation_id),
            ok_values=("FINISHED",),
            fail_values=("ERROR", "EXPIRED"),
            timeout_sec=240,
            interval_sec=4.0,
        )
        if not polled.get("ok"):
            return {
                "ok": False,
                "kind": kind,
                "step": "processing",
                "creation_id": creation_id,
                "error": polled.get("error") or "媒體處理失敗",
                "raw": polled,
            }

    ok_p, published, p_status = request_json(
        "POST",
        f"{_base(cfg)}/{cfg.threads_user_id}/threads_publish",
        data={
            "creation_id": creation_id,
            "access_token": cfg.threads_token,
        },
    )
    if not ok_p:
        return {
            "ok": False,
            "kind": kind,
            "step": "publish",
            "creation_id": creation_id,
            "http": p_status,
            "error": graph_error_message(published),
            "raw": published,
        }
    return {
        "ok": True,
        "kind": kind,
        "id": published.get("id"),
        "creation_id": creation_id,
        "raw": published,
    }


def publish_photo(cfg: MetaConfig, *, image_url: str, text: str) -> Dict[str, Any]:
    return _create_and_publish(
        cfg,
        container_data={
            "media_type": "IMAGE",
            "image_url": image_url,
            "text": text or "",
        },
        kind="photo",
    )


def publish_video(cfg: MetaConfig, *, video_url: str, text: str) -> Dict[str, Any]:
    return _create_and_publish(
        cfg,
        container_data={
            "media_type": "VIDEO",
            "video_url": video_url,
            "text": text or "",
        },
        kind="reel",
    )


def publish_day(
    cfg: MetaConfig,
    *,
    caption: str,
    image_public_path: str,
    video_public_path: str,
) -> Dict[str, Any]:
    results: Dict[str, Any] = {"ok": False, "channel": "threads", "posts": {}}
    image_url = public_asset_url(cfg, image_public_path) if image_public_path else ""
    video_url = public_asset_url(cfg, video_public_path) if video_public_path else ""

    if image_url:
        results["posts"]["photo"] = publish_photo(cfg, image_url=image_url, text=caption)
    else:
        results["posts"]["photo"] = {"ok": False, "error": "沒有可用的靜態圖公開 URL"}

    if video_url:
        results["posts"]["reel"] = publish_video(cfg, video_url=video_url, text=caption)
    else:
        results["posts"]["reel"] = {"ok": False, "error": "沒有可用的影片公開 URL"}

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
        results["error"] = "；".join(e for e in errs if e) or "Threads 發布失敗"
    return results
