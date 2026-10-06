"""Instagram Content Publishing: feed image + Reels."""
from __future__ import annotations

from typing import Any, Dict

from .meta_config import MetaConfig, public_asset_url
from .meta_http import graph_error_message, poll_until, request_json


def _base(cfg: MetaConfig) -> str:
    return f"https://graph.facebook.com/{cfg.graph_version}"


def _container_status(cfg: MetaConfig, creation_id: str) -> Dict[str, Any]:
    ok, payload, _ = request_json(
        "GET",
        f"{_base(cfg)}/{creation_id}",
        params={
            "fields": "status_code,status,id",
            "access_token": cfg.ig_token,
        },
    )
    if not ok:
        return {"status_code": "ERROR", "error": graph_error_message(payload), "raw": payload}
    return payload


def _create_and_publish(
    cfg: MetaConfig,
    *,
    container_data: Dict[str, Any],
    kind: str,
) -> Dict[str, Any]:
    if not cfg.ig_user_id or not cfg.ig_token:
        return {
            "ok": False,
            "kind": kind,
            "error": "缺少 INSTAGRAM_BUSINESS_ACCOUNT_ID 或 access token",
        }

    data = dict(container_data)
    data["access_token"] = cfg.ig_token
    ok, created, status = request_json(
        "POST",
        f"{_base(cfg)}/{cfg.ig_user_id}/media",
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

    polled = poll_until(
        lambda: _container_status(cfg, creation_id),
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
        f"{_base(cfg)}/{cfg.ig_user_id}/media_publish",
        data={
            "creation_id": creation_id,
            "access_token": cfg.ig_token,
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


def publish_photo(cfg: MetaConfig, *, image_url: str, caption: str) -> Dict[str, Any]:
    return _create_and_publish(
        cfg,
        container_data={
            "image_url": image_url,
            "caption": caption or "",
        },
        kind="photo",
    )


def publish_reel(
    cfg: MetaConfig,
    *,
    video_url: str,
    caption: str,
    cover_url: str = "",
    share_to_feed: bool = True,
) -> Dict[str, Any]:
    data: Dict[str, Any] = {
        "media_type": "REELS",
        "video_url": video_url,
        "caption": caption or "",
        "share_to_feed": "true" if share_to_feed else "false",
    }
    if cover_url:
        data["cover_url"] = cover_url
    return _create_and_publish(cfg, container_data=data, kind="reel")


def publish_day(
    cfg: MetaConfig,
    *,
    caption: str,
    image_public_path: str,
    video_public_path: str,
    cover_public_path: str = "",
) -> Dict[str, Any]:
    results: Dict[str, Any] = {"ok": False, "channel": "instagram", "posts": {}}
    image_url = public_asset_url(cfg, image_public_path) if image_public_path else ""
    video_url = public_asset_url(cfg, video_public_path) if video_public_path else ""
    cover_url = public_asset_url(cfg, cover_public_path) if cover_public_path else image_url

    if image_url:
        results["posts"]["photo"] = publish_photo(cfg, image_url=image_url, caption=caption)
    else:
        results["posts"]["photo"] = {"ok": False, "error": "沒有可用的靜態圖公開 URL"}

    if video_url:
        results["posts"]["reel"] = publish_reel(
            cfg, video_url=video_url, caption=caption, cover_url=cover_url
        )
    else:
        results["posts"]["reel"] = {"ok": False, "error": "沒有可用的 Reels 公開 URL"}

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
        results["error"] = "；".join(e for e in errs if e) or "Instagram 發布失敗"
    return results
