"""Publish daily pack to the public site (site/ source + docs/ GitHub Pages)."""
from __future__ import annotations

import shutil
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List, Optional
from zoneinfo import ZoneInfo

from .. import ROOT, day_dir, load_json, save_json

REPO_ROOT = ROOT.parent
SITE_ROOT = REPO_ROOT / "site"
DOCS_ROOT = REPO_ROOT / "docs"  # GitHub Pages serves docs/
TAIPEI = ZoneInfo("Asia/Taipei")


def _taipei_today_key() -> str:
    return datetime.now(TAIPEI).strftime("%Y%m%d")


def _copy_if_exists(src: Path, dst: Path) -> bool:
    if not src.exists():
        return False
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dst)
    return True


def _archive_entry(yyyymmdd: str, daily: Dict[str, Any], payload: Dict[str, Any]) -> Dict[str, Any]:
    return {
        "dateKey": yyyymmdd,
        "date": daily.get("date") or payload.get("date"),
        "dateLine1": daily.get("dateLine1") or payload.get("dateLine1") or "",
        "dateLine2": daily.get("dateLine2") or payload.get("dateLine2") or "",
        "fortuneLevel": daily.get("fortuneLevel") or payload.get("fortuneLevel"),
        "mainTheme": daily.get("mainTheme") or payload.get("mainTheme") or "",
        "secondaryTheme": daily.get("secondaryTheme") or payload.get("secondaryTheme") or "",
        "summaryText": daily.get("summaryText") or payload.get("summaryText") or "",
        "rocDate": daily.get("rocDate") or payload.get("rocDate") or "",
        "gif": f"assets/daily/daily_{yyyymmdd}.gif",
        "preview": f"assets/daily/daily_{yyyymmdd}_preview.jpg",
        "page": f"daily/{yyyymmdd}.html",
    }


def _update_archive(root: Path, entry: Dict[str, Any]) -> str:
    """Merge / rebuild daily_archive.json from data/daily/*.json + new entry."""
    data_dir = root / "data"
    daily_dir = data_dir / "daily"
    days: Dict[str, Dict[str, Any]] = {}

    if daily_dir.is_dir():
        for p in daily_dir.glob("*.json"):
            try:
                item = load_json(p)
            except Exception:
                continue
            key = str(item.get("dateKey") or p.stem)
            if not key.isdigit():
                continue
            media = item.get("media") or {}
            days[key] = {
                "dateKey": key,
                "date": item.get("date"),
                "dateLine1": item.get("dateLine1") or "",
                "dateLine2": item.get("dateLine2") or "",
                "fortuneLevel": item.get("fortuneLevel"),
                "mainTheme": item.get("mainTheme") or "",
                "secondaryTheme": item.get("secondaryTheme") or "",
                "summaryText": item.get("summaryText") or "",
                "rocDate": item.get("rocDate") or "",
                "gif": media.get("gifDated") or f"assets/daily/daily_{key}.gif",
                "preview": media.get("previewDated") or f"assets/daily/daily_{key}_preview.jpg",
                "page": f"daily/{key}.html",
            }

    days[entry["dateKey"]] = entry
    ordered = [days[k] for k in sorted(days.keys(), reverse=True)]
    today = _taipei_today_key()
    archive = {
        "version": 1,
        "updatedAt": datetime.now().isoformat(timespec="seconds"),
        "timezone": "Asia/Taipei",
        "todayKey": today,
        "count": len(ordered),
        "days": ordered,
    }
    archive_path = data_dir / "daily_archive.json"
    save_json(archive_path, archive)
    return str(archive_path.relative_to(root)).replace("\\", "/")


def _build_payload(yyyymmdd: str, daily: Dict[str, Any], fortune: Dict[str, Any]) -> Dict[str, Any]:
    gif_dated = f"assets/daily/daily_{yyyymmdd}.gif"
    preview_dated = f"assets/daily/daily_{yyyymmdd}_preview.jpg"
    scene_jpg = f"assets/daily/scene_{yyyymmdd}.jpg"
    scene_png = f"assets/daily/scene_{yyyymmdd}.png"
    return {
        "version": 1,
        "updatedAt": datetime.now().isoformat(timespec="seconds"),
        "timezone": "Asia/Taipei",
        "date": daily.get("date"),
        "dateKey": yyyymmdd,
        "rocDate": daily.get("rocDate"),
        "lunarDate": daily.get("lunarDate"),
        "dateLine1": daily.get("dateLine1"),
        "dateLine2": daily.get("dateLine2"),
        "dayGanZhi": daily.get("dayGanZhi"),
        "dayAnimalHint": daily.get("dayAnimalHint"),
        "fortuneLevel": daily.get("fortuneLevel"),
        "fortuneScore": daily.get("fortuneScore"),
        "mainTheme": daily.get("mainTheme"),
        "secondaryTheme": daily.get("secondaryTheme"),
        "yi": daily.get("yi") or [],
        "ji": daily.get("ji") or [],
        "summaryText": daily.get("summaryText") or "",
        "shortMessage": daily.get("shortMessage") or "",
        "ctaLine": daily.get("ctaLine"),
        "ctaProduct": daily.get("ctaProduct"),
        "ctaUrl": daily.get("ctaUrl"),
        "brandName": daily.get("brandName") or "名序",
        "brandTagline": daily.get("brandTagline") or "新生兒命名 ‧ 專業改名 ‧ 流年運勢",
        "page": f"daily/{yyyymmdd}.html",
        "media": {
            # Prefer dated paths so the site can schedule by calendar day.
            "gif": gif_dated,
            "webp": f"assets/daily/daily_{yyyymmdd}.webp",
            "preview": preview_dated,
            "scene": scene_jpg,
            "scenePng": scene_png,
            "gifDated": gif_dated,
            "previewDated": preview_dated,
            "latestGif": "assets/daily/latest.gif",
            "latestPreview": "assets/daily/latest_preview.jpg",
        },
        "fortuneNote": fortune.get("note") or "公共日曆吉凶；宜忌來自農曆通書資料，非個人命盤。",
    }


def _active_display_key(root: Path, today: str) -> Optional[str]:
    """Newest published dateKey that is <= Taipei today."""
    daily_dir = root / "data" / "daily"
    if not daily_dir.is_dir():
        return None
    keys = sorted(
        (p.stem for p in daily_dir.glob("*.json") if p.stem.isdigit() and p.stem <= today),
        reverse=True,
    )
    return keys[0] if keys else None


def _refresh_latest_pointer(root: Path, copied: List[str]) -> None:
    """
    Point daily_latest.json + latest.* media at the calendar-active day
    (Taipei today if published, else newest day <= today). Future schedules
    do not overwrite the live pointer.
    """
    today = _taipei_today_key()
    active = _active_display_key(root, today)
    if not active:
        return

    data_dir = root / "data"
    assets_dir = root / "assets" / "daily"
    dated_json = data_dir / "daily" / f"{active}.json"
    if not dated_json.exists():
        return

    payload = load_json(dated_json)
    payload["displayKey"] = active
    payload["resolvedFor"] = today
    latest_json = data_dir / "daily_latest.json"
    save_json(latest_json, payload)
    copied.append(str(latest_json.relative_to(root)).replace("\\", "/"))

    pairs = [
        (assets_dir / f"daily_{active}.gif", assets_dir / "latest.gif"),
        (assets_dir / f"daily_{active}.webp", assets_dir / "latest.webp"),
        (assets_dir / f"daily_{active}_preview.jpg", assets_dir / "latest_preview.jpg"),
        (assets_dir / f"scene_{active}.jpg", assets_dir / "latest_scene.jpg"),
        (assets_dir / f"scene_{active}.png", assets_dir / "latest_scene.png"),
    ]
    for src, dst in pairs:
        if src.exists():
            shutil.copy2(src, dst)
            copied.append(str(dst.relative_to(root)).replace("\\", "/"))


def _write_seo_pages(root: Path, copied: List[str]) -> None:
    """Write crawlable daily/*.html, hub daily.html, and refresh sitemap.xml."""
    from .daily_seo import render_day_html, render_daily_hub_html, write_sitemap

    daily_dir = root / "data" / "daily"
    out_dir = root / "daily"
    out_dir.mkdir(parents=True, exist_ok=True)
    today = _taipei_today_key()
    days: List[Dict[str, Any]] = []
    keys: List[str] = []

    payloads: List[Dict[str, Any]] = []
    if daily_dir.is_dir():
        for p in sorted(daily_dir.glob("*.json"), reverse=True):
            if not p.stem.isdigit():
                continue
            try:
                item = load_json(p)
            except Exception:
                continue
            key = str(item.get("dateKey") or p.stem)
            keys.append(key)
            payloads.append(item)
            days.append(
                {
                    "dateKey": key,
                    "rocDate": item.get("rocDate") or "",
                    "fortuneLevel": item.get("fortuneLevel"),
                    "mainTheme": item.get("mainTheme") or "",
                    "summaryText": item.get("summaryText") or "",
                    "page": f"daily/{key}.html",
                }
            )

    # Chronological neighbors for crawlable prev/next (keys already newest-first)
    chrono = list(reversed(keys))
    idx_map = {k: i for i, k in enumerate(chrono)}
    for item in payloads:
        key = str(item.get("dateKey") or "")
        i = idx_map.get(key, -1)
        prev_key = chrono[i - 1] if i > 0 else ""
        next_key = chrono[i + 1] if 0 <= i < len(chrono) - 1 else ""
        html = render_day_html(item, prev_key=prev_key, next_key=next_key)
        out = out_dir / f"{key}.html"
        out.write_text(html, encoding="utf-8")
        copied.append(str(out.relative_to(root)).replace("\\", "/"))

    hub = root / "daily.html"
    hub.write_text(render_daily_hub_html(days, today), encoding="utf-8")
    copied.append("daily.html")
    write_sitemap(root, keys)
    copied.append("sitemap.xml")


def _publish_into(root: Path, yyyymmdd: str, daily: Dict[str, Any], fortune: Dict[str, Any]) -> List[str]:
    """Write dated media + JSON; refresh calendar latest pointer; write SEO pages."""
    assets_dir = root / "assets" / "daily"
    assets_dir.mkdir(parents=True, exist_ok=True)
    data_dir = root / "data"
    data_dir.mkdir(parents=True, exist_ok=True)

    base = day_dir(yyyymmdd)
    copied: List[str] = []
    dated_map = [
        (base / "gif" / f"daily_{yyyymmdd}.gif", assets_dir / f"daily_{yyyymmdd}.gif"),
        (base / "gif" / f"daily_{yyyymmdd}.webp", assets_dir / f"daily_{yyyymmdd}.webp"),
        (base / "short" / f"daily_{yyyymmdd}_preview.jpg", assets_dir / f"daily_{yyyymmdd}_preview.jpg"),
        (base / "scene" / f"scene_{yyyymmdd}.jpg", assets_dir / f"scene_{yyyymmdd}.jpg"),
        (base / "scene" / f"scene_{yyyymmdd}.png", assets_dir / f"scene_{yyyymmdd}.png"),
    ]
    for src, dated in dated_map:
        if _copy_if_exists(src, dated):
            copied.append(str(dated.relative_to(root)).replace("\\", "/"))

    payload = _build_payload(yyyymmdd, daily, fortune)
    dated_json = data_dir / "daily" / f"{yyyymmdd}.json"
    save_json(dated_json, payload)
    copied.append(str(dated_json.relative_to(root)).replace("\\", "/"))

    archive_rel = _update_archive(root, _archive_entry(yyyymmdd, daily, payload))
    copied.append(archive_rel)

    _refresh_latest_pointer(root, copied)
    _write_seo_pages(root, copied)

    site_js = SITE_ROOT / "js" / "daily.js"
    if site_js.exists() and root != SITE_ROOT:
        _copy_if_exists(site_js, root / "js" / "daily.js")
        copied.append("js/daily.js")

    site_layout = SITE_ROOT / "js" / "layout.js"
    if site_layout.exists() and root != SITE_ROOT:
        _copy_if_exists(site_layout, root / "js" / "layout.js")
        copied.append("js/layout.js")

    return copied


def publish_to_site(yyyymmdd: str) -> Dict[str, Any]:
    """
    Sync one day's GIF / preview / daily JSON into site/ and docs/.
    Future dates are stored for schedule; live pointer follows Asia/Taipei today.
    """
    if not SITE_ROOT.is_dir():
        raise FileNotFoundError(f"找不到官網目錄：{SITE_ROOT}")

    base = day_dir(yyyymmdd)
    daily_p = base / "data" / "daily.json"
    if not daily_p.exists():
        raise FileNotFoundError(f"尚未產生此日：{yyyymmdd}")

    daily = load_json(daily_p)
    fortune = {}
    fortune_p = base / "data" / "fortune.json"
    if fortune_p.exists():
        fortune = load_json(fortune_p)

    roots = [SITE_ROOT]
    if DOCS_ROOT.is_dir():
        roots.append(DOCS_ROOT)
    elif not DOCS_ROOT.exists():
        DOCS_ROOT.mkdir(parents=True, exist_ok=True)
        roots.append(DOCS_ROOT)

    today = _taipei_today_key()
    all_copied: List[str] = []
    for root in roots:
        label = root.name
        for rel in _publish_into(root, yyyymmdd, daily, fortune):
            all_copied.append(f"{label}/{rel}")

    scheduled = yyyymmdd > today
    note = (
        f"已寫入排程日 {yyyymmdd}（台北今天 {today}）。官網到當天會自動顯示。"
        if scheduled
        else f"已寫入當日／可顯示日 {yyyymmdd}，並更新 live 指標。工作台發布會接著自動 commit／push。"
    )

    return {
        "ok": True,
        "channel": "site",
        "siteRoot": str(SITE_ROOT),
        "docsRoot": str(DOCS_ROOT),
        "files": all_copied,
        "dateKey": yyyymmdd,
        "todayKey": today,
        "scheduled": scheduled,
        "latest": str(DOCS_ROOT / "data" / "daily_latest.json"),
        "archive": str(DOCS_ROOT / "data" / "daily_archive.json"),
        "page": "daily.html",
        "url": f"https://oreoyanz.github.io/namingShooting/daily/{yyyymmdd}.html",
        "note": note,
    }
