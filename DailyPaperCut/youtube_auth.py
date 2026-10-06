#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""One-time YouTube OAuth: opens browser, saves config/youtube_token.json."""
from __future__ import annotations

import json
import socket
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))

from dotenv import load_dotenv

load_dotenv(ROOT / ".env", override=True)

from engine.publishers.youtube import (  # noqa: E402
    DEFAULT_CLIENT,
    SCOPES,
    _client_secrets_path,
    _token_path,
)

# Preferred ports when using a Web OAuth client (must match Cloud Console redirect URIs)
PREFERRED_PORTS = (8765, 8766, 8767, 8787, 8888, 8080)


def _port_free(port: int, host: str = "127.0.0.1") -> bool:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        try:
            s.bind((host, port))
            return True
        except OSError:
            return False


def _pick_port(preferred: tuple[int, ...] = PREFERRED_PORTS) -> int:
    for p in preferred:
        if _port_free(p):
            return p
    # Last resort: OS-assigned ephemeral port
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.bind(("127.0.0.1", 0))
        return int(s.getsockname()[1])


def _client_kind(client: Path) -> str:
    data = json.loads(client.read_text(encoding="utf-8"))
    if "installed" in data:
        return "installed"
    if "web" in data:
        return "web"
    return "unknown"


def _prepare_client_file(client: Path, port: int) -> Path:
    """Return a client secrets path suitable for InstalledAppFlow."""
    data = json.loads(client.read_text(encoding="utf-8"))
    if "installed" in data:
        return client

    if "web" not in data:
        print(f"無法辨識 client secret 格式：{client}")
        print("請重新下載「桌面應用程式」類型的 OAuth 用戶端 JSON。")
        raise SystemExit(1)

    web = data["web"]
    redirect_uris = [
        f"http://localhost:{port}/",
        f"http://127.0.0.1:{port}/",
        f"http://localhost:{port}",
        f"http://127.0.0.1:{port}",
    ]
    print()
    print("=" * 60)
    print("偵測到目前是「網頁應用程式 (Web)」OAuth 用戶端。")
    print("請確認 Cloud Console 已加入以下「已授權的重新導向 URI」：")
    for u in redirect_uris[:2]:
        print(f"  {u}")
    print()
    print("【更建議】改建成「桌面應用程式」憑證，覆蓋 JSON 後重跑。")
    print("=" * 60)
    print()

    wrapped = {
        "installed": {
            "client_id": web.get("client_id"),
            "client_secret": web.get("client_secret"),
            "project_id": web.get("project_id"),
            "auth_uri": web.get("auth_uri") or "https://accounts.google.com/o/oauth2/auth",
            "token_uri": web.get("token_uri") or "https://oauth2.googleapis.com/token",
            "auth_provider_x509_cert_url": web.get("auth_provider_x509_cert_url"),
            "redirect_uris": redirect_uris,
        }
    }
    tmp = ROOT / "config" / "_youtube_client_installed_wrap.json"
    tmp.write_text(json.dumps(wrapped, ensure_ascii=False, indent=2), encoding="utf-8")
    return tmp


def main() -> int:
    try:
        from google_auth_oauthlib.flow import InstalledAppFlow
    except ImportError:
        print("請先安裝：pip install google-api-python-client google-auth-oauthlib google-auth-httplib2")
        return 1

    client = _client_secrets_path()
    if not client.exists():
        print(f"找不到 client secret：{client}")
        print("請依 docs/YOUTUBE_SETUP.md 下載 OAuth 用戶端 JSON，")
        print(f"存成：{DEFAULT_CLIENT}")
        return 1

    kind = _client_kind(client)
    # Desktop ("installed"): let OS pick a free port (port=0).
    # Web: pick a free preferred port and require matching redirect URI in Console.
    if kind == "installed":
        port = 0
        client_for_flow = client
        print("偵測到「桌面應用程式」憑證，將自動選用空閒連接埠。")
    else:
        port = _pick_port()
        try:
            client_for_flow = _prepare_client_file(client, port if port else 8765)
        except SystemExit as e:
            return int(e.code) if isinstance(e.code, int) else 1
        if port == 0:
            port = _pick_port()
        print(f"使用連接埠 {port}（若 8765 被占用會自動換埠）")

    redirect_hint = f"http://localhost:{port}/" if port else "http://localhost:<自動>/"
    print(f"即將開啟瀏覽器授權（redirect ≈ {redirect_hint}）…")
    print("請登入「要上傳 Shorts 的那個 YouTube 頻道」帳號。")

    flow = InstalledAppFlow.from_client_secrets_file(str(client_for_flow), SCOPES)
    try:
        creds = flow.run_local_server(
            port=port,
            open_browser=True,
            bind_addr="127.0.0.1",
        )
    except OSError as e:
        print()
        print("授權失敗（連接埠）：", e)
        print("請關閉占用該埠的程式後重試，或改用「桌面應用程式」憑證（可用 port=0）。")
        return 1
    except Exception as e:
        print()
        print("授權失敗：", e)
        print()
        print("若仍是 redirect_uri_mismatch：")
        print("1) 最快：下載「桌面應用程式」類型的 client secret JSON 覆蓋原檔後重跑")
        if port:
            print("2) 或在 Web 用戶端加入重新導向 URI：")
            print(f"   http://localhost:{port}/")
            print(f"   http://127.0.0.1:{port}/")
        return 1

    token = _token_path()
    token.parent.mkdir(parents=True, exist_ok=True)
    token.write_text(creds.to_json(), encoding="utf-8")
    print(f"已儲存 token：{token}")
    print("之後產生當日包即可自動上傳 Shorts（AUTO_PUBLISH_YOUTUBE=true）。")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
