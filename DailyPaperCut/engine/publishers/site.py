"""Publish daily pack to the public site (site/ source + docs/ GitHub Pages)."""
from __future__ import annotations

import shutil
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List

from .. import ROOT, day_dir, load_json, save_json

REPO_ROOT = ROOT.parent
SITE_ROOT = REPO_ROOT / "site"
DOCS_ROOT = REPO_ROOT / "docs"  # GitHub Pages serves docs/


def _copy_if_exists(src: Path, dst: Path) -> bool:
    if not src.exists():
        return False
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dst)
    return True


def _publish_into(root: Path, yyyymmdd: str, daily: Dict[str, Any], fortune: Dict[str, Any]) -> List[str]:
    """Write media + JSON under one web root. Returns relative paths from that root."""
    assets_dir = root / "assets" / "daily"
    assets_dir.mkdir(parents=True, exist_ok=True)
    data_dir = root / "data"
    data_dir.mkdir(parents=True, exist_ok=True)

    base = day_dir(yyyymmdd)
    copied: List[str] = []
    media_map = [
        (base / "gif" / f"daily_{yyyymmdd}.gif", assets_dir / f"daily_{yyyymmdd}.gif", assets_dir / "latest.gif"),
        (base / "gif" / f"daily_{yyyymmdd}.webp", assets_dir / f"daily_{yyyymmdd}.webp", assets_dir / "latest.webp"),
        (base / "short" / f"daily_{yyyymmdd}_preview.jpg", assets_dir / f"daily_{yyyymmdd}_preview.jpg", assets_dir / "latest_preview.jpg"),
        (base / "scene" / f"scene_{yyyymmdd}.jpg", assets_dir / f"scene_{yyyymmdd}.jpg", assets_dir / "latest_scene.jpg"),
        (base / "scene" / f"scene_{yyyymmdd}.png", assets_dir / f"scene_{yyyymmdd}.png", assets_dir / "latest_scene.png"),
    ]
    for src, dated, latest in media_map:
        if _copy_if_exists(src, dated):
            copied.append(str(dated.relative_to(root)).replace("\\", "/"))
            shutil.copy2(src, latest)
            copied.append(str(latest.relative_to(root)).replace("\\", "/"))

    if (assets_dir / "latest_scene.jpg").exists():
        scene_rel = "assets/daily/latest_scene.jpg"
    elif (assets_dir / "latest_scene.png").exists():
        scene_rel = "assets/daily/latest_scene.png"
    else:
        scene_rel = None

    payload = {
        "version": 1,
        "updatedAt": datetime.now().isoformat(timespec="seconds"),
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
        "brandTagline": daily.get("brandTagline") or "知名・知運・知人生",
        "media": {
            "gif": "assets/daily/latest.gif",
            "webp": "assets/daily/latest.webp",
            "preview": "assets/daily/latest_preview.jpg",
            "scene": scene_rel,
            "gifDated": f"assets/daily/daily_{yyyymmdd}.gif",
        },
        "fortuneNote": fortune.get("note") or "公共日曆吉凶；宜忌來自農曆通書資料，非個人命盤。",
    }

    latest_json = data_dir / "daily_latest.json"
    dated_json = data_dir / "daily" / f"{yyyymmdd}.json"
    save_json(latest_json, payload)
    save_json(dated_json, payload)
    copied.extend(
        [
            str(latest_json.relative_to(root)).replace("\\", "/"),
            str(dated_json.relative_to(root)).replace("\\", "/"),
        ]
    )

    # Keep daily.html in sync if present under site/
    site_page = SITE_ROOT / "daily.html"
    if site_page.exists() and root != SITE_ROOT:
        _copy_if_exists(site_page, root / "daily.html")
        copied.append("daily.html")

    return copied


def publish_to_site(yyyymmdd: str) -> Dict[str, Any]:
    """
    Sync one day's GIF / preview / daily JSON into site/ and docs/.
    GitHub Pages serves docs/; site/ is the editable source.
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

    all_copied: List[str] = []
    for root in roots:
        label = root.name
        for rel in _publish_into(root, yyyymmdd, daily, fortune):
            all_copied.append(f"{label}/{rel}")

    return {
        "ok": True,
        "channel": "site",
        "siteRoot": str(SITE_ROOT),
        "docsRoot": str(DOCS_ROOT),
        "files": all_copied,
        "latest": str(DOCS_ROOT / "data" / "daily_latest.json"),
        "page": "daily.html",
        "url": "https://oreoyanz.github.io/namingShooting/daily.html",
        "note": "已寫入 site/ 與 docs/。請 commit + push docs/ 後 GitHub Pages 才會上線。",
    }
