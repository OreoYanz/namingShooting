"""
名序｜每日剪紙工作台（FastAPI）
底層 Python；瀏覽器介面含：每日產生、素材工作室。

啟動：
  cd DailyPaperCut
  uvicorn ui.web_app:app --reload --port 8765
"""
from __future__ import annotations

import json
import sys
from datetime import datetime
from pathlib import Path
from typing import Optional

from fastapi import FastAPI, Form, HTTPException
from fastapi.responses import FileResponse, HTMLResponse, JSONResponse
from fastapi.staticfiles import StaticFiles

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

try:
    from dotenv import load_dotenv
except ImportError:
    def load_dotenv(*args, **kwargs):
        return False

load_dotenv(ROOT / ".env")

from engine import day_dir, load_json, load_settings, save_json
from engine.material_pool import (
    category_color,
    generate_pool_item,
    get_categories,
    list_pool,
    load_library,
    sync_assets_into_library,
    sync_library_categories,
)
from engine.pipeline import generate_daily

app = FastAPI(title="名序｜每日吉祥剪紙工作台", version="0.2.0")

STATIC = Path(__file__).parent / "static"
STATIC.mkdir(exist_ok=True)
app.mount("/static", StaticFiles(directory=str(STATIC)), name="static")


def _html_page() -> str:
    return (STATIC / "index.html").read_text(encoding="utf-8")


@app.get("/", response_class=HTMLResponse)
def home():
    return _html_page()


@app.get("/api/settings")
def api_settings():
    s = load_settings()
    return {
        "cta": s.get("cta"),
        "brand": s.get("brand"),
        "media": s.get("media"),
        "enableImageGeneration": s["openai"].get("enableImageGeneration"),
        "reuseRules": s.get("reuseRules"),
    }


@app.get("/api/categories")
def api_categories():
    cats = get_categories(sync_library=True)
    return {
        "categories": cats,
        "source": "materials/assets/*/",
        "note": "類別隨 assets 資料夾增減自動更新",
    }


@app.post("/api/pool/sync-categories")
def api_sync_categories():
    cats = sync_library_categories()
    synced = sync_assets_into_library()
    return {"ok": True, **cats, "syncedFiles": synced}


@app.get("/api/pool")
def api_pool(category: Optional[str] = None):
    known = get_categories()
    if category and category not in known:
        return {"count": 0, "ready": 0, "items": [], "categories": known}
    out = []
    for it in list_pool(category):
        row = dict(it)
        row["ready"] = bool(row.get("file")) and (ROOT / row["file"]).exists() if row.get("file") else False
        out.append(row)
    return {
        "count": len(out),
        "ready": sum(1 for x in out if x["ready"]),
        "items": out,
        "categories": known,
    }


@app.post("/api/pool/generate")
def api_pool_generate(
    category: str = Form(...),
    name: str = Form(...),
    tags: str = Form(""),
    roles: str = Form("support"),
    placeholder: str = Form("false"),
):
    tag_list = [t.strip() for t in tags.replace("，", ",").split(",") if t.strip()]
    role_list = [r.strip() for r in roles.replace("，", ",").split(",") if r.strip()] or ["support"]
    use_ph = str(placeholder).lower() in ("1", "true", "yes", "on")
    try:
        item = generate_pool_item(
            category=category,
            name=name.strip(),
            tags=tag_list,
            role_hints=role_list,
            use_placeholder=use_ph,
        )
        return {"ok": True, "item": item}
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e)) from e


@app.post("/api/pool/seed-placeholders")
def api_pool_seed():
    """Fill missing files for library entries with placeholders (no API)."""
    from engine.ai_image import _pillow_placeholder

    lib = load_library()
    known = set(get_categories())
    created = []
    for it in lib.get("items", []):
        cat = it.get("category")
        if cat not in known:
            continue
        mid = it["id"]
        rel = it.get("file") or f"materials/assets/{cat}/{mid}.png"
        abs_path = ROOT / rel
        if abs_path.exists():
            if not it.get("file"):
                it["file"] = rel.replace("\\", "/")
                created.append({"id": mid, "action": "linked"})
            continue
        abs_path.parent.mkdir(parents=True, exist_ok=True)
        _pillow_placeholder(abs_path, it.get("name", mid), category_color(cat))
        it["file"] = rel.replace("\\", "/")
        it["method"] = it.get("method") or "placeholder_seed"
        created.append({"id": mid, "action": "created", "file": it["file"]})
    lib["categories"] = get_categories()
    save_json(ROOT / "materials" / "library.json", lib)
    return {"ok": True, "updated": len(created), "items": created, "categories": lib["categories"]}


@app.get("/api/pool/file/{item_id}")
def api_pool_file(item_id: str):
    for it in load_library().get("items", []):
        if it["id"] == item_id and it.get("file"):
            p = ROOT / it["file"]
            if p.exists():
                return FileResponse(p, media_type="image/png")
    raise HTTPException(404, "素材檔不存在")


@app.post("/api/generate")
def api_generate(date: str = Form(...)):
    try:
        result = generate_daily(date)
        return {
            "ok": True,
            "date": result["date"],
            "dir": result["dir"],
            "daily": result["daily"],
            "fortune": result["fortune"],
            "files": {
                k: v for k, v in result["files"].items()
                if k not in ("social", "cutouts")
            },
            "materialIds": result["materials"].get("allIds"),
        }
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e)) from e


@app.get("/api/day/{yyyymmdd}")
def api_day(yyyymmdd: str):
    base = day_dir(yyyymmdd)
    daily_p = base / "data" / "daily.json"
    if not daily_p.exists():
        raise HTTPException(404, "尚未產生此日")
    daily = load_json(daily_p)
    fortune = load_json(base / "data" / "fortune.json") if (base / "data" / "fortune.json").exists() else {}
    pub = base / "publish" / "publish.json"
    publish = load_json(pub) if pub.exists() else {}
    files = {
        "scene": f"/api/day/{yyyymmdd}/file/scene",
        "preview": f"/api/day/{yyyymmdd}/file/preview",
        "gif": f"/api/day/{yyyymmdd}/file/gif",
        "mp4": f"/api/day/{yyyymmdd}/file/mp4",
    }
    social = {}
    for name in ("youtube", "instagram", "facebook", "threads"):
        p = base / "social" / f"{name}.txt"
        if p.exists():
            social[name] = p.read_text(encoding="utf-8")
    return {"daily": daily, "fortune": fortune, "publish": publish, "files": files, "social": social}


@app.get("/api/day/{yyyymmdd}/file/{kind}")
def api_day_file(yyyymmdd: str, kind: str):
    base = day_dir(yyyymmdd)
    mapping = {
        "scene": base / "scene" / f"scene_{yyyymmdd}.jpg",
        "scene_png": base / "scene" / f"scene_{yyyymmdd}.png",
        "preview": base / "short" / f"daily_{yyyymmdd}_preview.jpg",
        "gif": base / "gif" / f"daily_{yyyymmdd}.gif",
        "mp4": base / "short" / f"daily_{yyyymmdd}.mp4",
    }
    path = mapping.get(kind)
    if not path or not path.exists():
        # try png scene
        if kind == "scene" and (base / "scene" / f"scene_{yyyymmdd}.png").exists():
            path = base / "scene" / f"scene_{yyyymmdd}.png"
        else:
            raise HTTPException(404, f"{kind} 尚未產出")
    media = {
        ".jpg": "image/jpeg",
        ".png": "image/png",
        ".gif": "image/gif",
        ".mp4": "video/mp4",
    }.get(path.suffix.lower(), "application/octet-stream")
    return FileResponse(path, media_type=media)


@app.post("/api/day/{yyyymmdd}/confirm")
def api_confirm(yyyymmdd: str):
    base = day_dir(yyyymmdd)
    pub = base / "publish" / "publish.json"
    if not pub.exists():
        raise HTTPException(404, "尚無 publish.json")
    data = load_json(pub)
    data["status"] = "confirmed"
    data["confirmedAt"] = datetime.now().isoformat(timespec="seconds")
    save_json(pub, data)
    return {"ok": True, "publish": data}


@app.post("/api/day/{yyyymmdd}/publish")
def api_publish(
    yyyymmdd: str,
    channels: str = Form("site"),
):
    """
    Publish selected channels. Currently implemented: site (官網).
    channels: comma-separated, e.g. "site,line,youtube"
    """
    base = day_dir(yyyymmdd)
    daily_p = base / "data" / "daily.json"
    if not daily_p.exists():
        raise HTTPException(404, "尚未產生此日，無法發布")

    selected = [c.strip().lower() for c in channels.replace("，", ",").split(",") if c.strip()]
    if not selected:
        raise HTTPException(400, "請至少勾選一個發布目標")

    pub_path = base / "publish" / "publish.json"
    publish = load_json(pub_path) if pub_path.exists() else {
        "date": yyyymmdd,
        "status": "draft",
        "channels": {},
        "confirmedAt": None,
        "publishedAt": None,
    }
    publish.setdefault("channels", {})
    publish.setdefault("results", {})

    results = {}
    errors = []

    if "site" in selected:
        try:
            from engine.publishers.site import publish_to_site

            results["site"] = publish_to_site(yyyymmdd)
            publish["channels"]["site"] = True
        except Exception as e:
            errors.append(f"site: {e}")
            publish["channels"]["site"] = False
            results["site"] = {"ok": False, "error": str(e)}

    # Placeholders for future channels (checkbox may be sent; return clear status)
    pending = []
    for ch in ("line", "facebook", "instagram", "threads", "youtube"):
        if ch in selected:
            pending.append(ch)
            publish["channels"][ch] = False
            results[ch] = {
                "ok": False,
                "pending": True,
                "message": f"「{ch}」發布接線尚未啟用，已記錄勾選。",
            }

    if results and not errors:
        publish["status"] = "published" if "site" in selected and results.get("site", {}).get("ok") else publish.get("status") or "draft"
        if results.get("site", {}).get("ok"):
            publish["publishedAt"] = datetime.now().isoformat(timespec="seconds")
            if not publish.get("confirmedAt"):
                publish["confirmedAt"] = publish["publishedAt"]
    publish["results"] = {**(publish.get("results") or {}), **results}
    publish["lastSelected"] = selected
    save_json(pub_path, publish)

    if errors and "site" in selected and not results.get("site", {}).get("ok"):
        raise HTTPException(status_code=400, detail="; ".join(errors))

    msg_parts = []
    if results.get("site", {}).get("ok"):
        msg_parts.append("官網資料已更新（site/data/daily_latest.json + assets/daily/）")
        note = results["site"].get("note")
        if note:
            msg_parts.append(note)
    if pending:
        msg_parts.append("尚未啟用：" + "、".join(pending))

    return {
        "ok": True,
        "date": yyyymmdd,
        "selected": selected,
        "results": results,
        "publish": publish,
        "message": "\n".join(msg_parts) or "已處理",
    }
