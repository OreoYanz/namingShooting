"""YouTube Shorts upload via YouTube Data API v3."""
from __future__ import annotations

import os
import pickle
from pathlib import Path
from typing import Any, Dict, List, Optional

from .. import ROOT, day_dir, load_json

SCOPES = ["https://www.googleapis.com/auth/youtube.upload"]
DEFAULT_TOKEN = ROOT / "config" / "youtube_token.json"
DEFAULT_CLIENT = ROOT / "config" / "youtube_client_secret.json"


def _env(name: str, default: str = "") -> str:
    return (os.getenv(name) or default).strip()


def youtube_ready() -> bool:
    """True if a usable token file exists (or client secret for first auth)."""
    return _token_path().exists() or youtube_configured()


def youtube_configured() -> bool:
    token = Path(_env("YOUTUBE_TOKEN_FILE") or str(DEFAULT_TOKEN))
    client = Path(_env("YOUTUBE_CLIENT_SECRETS") or str(DEFAULT_CLIENT))
    if token.exists():
        return True
    if client.exists():
        return True
    return bool(_env("YOUTUBE_CLIENT_ID") and _env("YOUTUBE_CLIENT_SECRET"))


def _token_path() -> Path:
    return Path(_env("YOUTUBE_TOKEN_FILE") or str(DEFAULT_TOKEN))


def _client_secrets_path() -> Path:
    return Path(_env("YOUTUBE_CLIENT_SECRETS") or str(DEFAULT_CLIENT))


def _load_credentials():
    """Load OAuth credentials; refresh if needed. Raises if missing."""
    try:
        from google.auth.transport.requests import Request
        from google.oauth2.credentials import Credentials
        from google_auth_oauthlib.flow import InstalledAppFlow
    except ImportError as e:
        raise RuntimeError(
            "缺少 YouTube 套件，請執行：pip install google-api-python-client google-auth-oauthlib google-auth-httplib2"
        ) from e

    token_path = _token_path()
    creds = None
    if token_path.exists():
        try:
            creds = Credentials.from_authorized_user_file(str(token_path), SCOPES)
        except Exception:
            # legacy pickle fallback
            try:
                with token_path.open("rb") as fh:
                    creds = pickle.load(fh)
            except Exception as e:
                raise RuntimeError(f"無法讀取 YouTube token：{token_path}（{e}）") from e

    if creds and creds.expired and creds.refresh_token:
        creds.refresh(Request())
        token_path.parent.mkdir(parents=True, exist_ok=True)
        token_path.write_text(creds.to_json(), encoding="utf-8")
        return creds

    if creds and creds.valid:
        return creds

    # Need interactive auth
    client = _client_secrets_path()
    if not client.exists():
        # Build from env if possible
        cid = _env("YOUTUBE_CLIENT_ID")
        csec = _env("YOUTUBE_CLIENT_SECRET")
        if not (cid and csec):
            raise RuntimeError(
                "尚未授權 YouTube。請先放置 config/youtube_client_secret.json，"
                "並執行：python youtube_auth.py（見 docs/YOUTUBE_SETUP.md）"
            )
        import json
        import tempfile

        payload = {
            "installed": {
                "client_id": cid,
                "client_secret": csec,
                "auth_uri": "https://accounts.google.com/o/oauth2/auth",
                "token_uri": "https://oauth2.googleapis.com/token",
                "redirect_uris": ["http://localhost"],
            }
        }
        tmp = Path(tempfile.gettempdir()) / "mingxu_youtube_client.json"
        tmp.write_text(json.dumps(payload), encoding="utf-8")
        client = tmp

    flow = InstalledAppFlow.from_client_secrets_file(str(client), SCOPES)
    creds = flow.run_local_server(port=0)
    token_path.parent.mkdir(parents=True, exist_ok=True)
    token_path.write_text(creds.to_json(), encoding="utf-8")
    return creds


def build_youtube_service():
    from googleapiclient.discovery import build

    creds = _load_credentials()
    return build("youtube", "v3", credentials=creds, cache_discovery=False)


def _title_from_daily(yyyymmdd: str, daily: Dict[str, Any], caption: str) -> str:
    custom = _env("YOUTUBE_TITLE_TEMPLATE")
    roc = daily.get("rocDate") or yyyymmdd
    theme = daily.get("mainTheme") or daily.get("fortuneLevel") or "每日吉祥"
    level = daily.get("fortuneLevel") or ""
    if custom:
        title = (
            custom.replace("{date}", yyyymmdd)
            .replace("{roc}", str(roc))
            .replace("{theme}", str(theme))
            .replace("{level}", str(level))
        )
    else:
        title = f"名序｜每日吉祥 {roc}｜{theme}"
        if level and level not in title:
            title = f"名序｜今日{level} {roc}｜{theme}"
    if "#Shorts" not in title and "#shorts" not in title.lower():
        title = title.strip() + " #Shorts"
    # YouTube title max ~100 chars
    return title[:100]


def _tags() -> List[str]:
    raw = _env("YOUTUBE_TAGS", "名序,每日吉祥,剪紙,Shorts,運勢,宜忌")
    tags = [t.strip() for t in raw.replace("，", ",").split(",") if t.strip()]
    if "Shorts" not in tags and "shorts" not in [t.lower() for t in tags]:
        tags.append("Shorts")
    return tags[:15]


def upload_short(
    *,
    video_path: Path,
    title: str,
    description: str,
    tags: Optional[List[str]] = None,
    privacy: Optional[str] = None,
    category_id: str = "22",
) -> Dict[str, Any]:
    """
    Upload a vertical short MP4. Returns {ok, videoId, url, ...}.
    """
    path = Path(video_path)
    if not path.exists():
        return {"ok": False, "error": f"找不到影片：{path}"}

    privacy = (privacy or _env("YOUTUBE_PRIVACY", "private")).lower()
    if privacy not in ("public", "unlisted", "private"):
        privacy = "private"

    try:
        from googleapiclient.http import MediaFileUpload

        youtube = build_youtube_service()
        body = {
            "snippet": {
                "title": title[:100],
                "description": (description or "")[:5000],
                "tags": tags or _tags(),
                "categoryId": category_id or _env("YOUTUBE_CATEGORY_ID", "22"),
            },
            "status": {
                "privacyStatus": privacy,
                "selfDeclaredMadeForKids": False,
            },
        }
        media = MediaFileUpload(str(path), mimetype="video/mp4", resumable=True, chunksize=1024 * 1024)
        request = youtube.videos().insert(part="snippet,status", body=body, media_body=media)
        response = None
        while response is None:
            status, response = request.next_chunk()
            # status may be None on final
        video_id = response.get("id")
        return {
            "ok": True,
            "channel": "youtube",
            "kind": "short",
            "videoId": video_id,
            "url": f"https://youtube.com/shorts/{video_id}" if video_id else None,
            "privacy": privacy,
            "title": title,
            "raw": {"id": video_id},
        }
    except Exception as e:
        return {"ok": False, "channel": "youtube", "error": str(e)}


def publish_day(yyyymmdd: str) -> Dict[str, Any]:
    """Upload that day's short MP4 with social/youtube.txt caption."""
    if not youtube_configured() and not _token_path().exists():
        return {
            "ok": False,
            "skipped": True,
            "channel": "youtube",
            "error": "尚未設定 YouTube OAuth（見 docs/YOUTUBE_SETUP.md）",
        }

    base = day_dir(yyyymmdd)
    mp4 = base / "short" / f"daily_{yyyymmdd}.mp4"
    caption_path = base / "social" / "youtube.txt"
    caption = caption_path.read_text(encoding="utf-8").strip() if caption_path.exists() else ""
    daily = {}
    daily_p = base / "data" / "daily.json"
    if daily_p.exists():
        daily = load_json(daily_p)

    title = _title_from_daily(yyyymmdd, daily, caption)
    desc = caption
    if "#Shorts" not in desc and "#shorts" not in desc.lower():
        desc = (desc + "\n\n#Shorts #名序 #每日吉祥").strip()

    result = upload_short(video_path=mp4, title=title, description=desc)
    return result
