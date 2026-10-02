"""Manifest + publish.json."""
from __future__ import annotations

from datetime import datetime
from pathlib import Path
from typing import Any, Dict

from . import load_settings, save_json


def build_manifest(
    yyyymmdd: str,
    fortune: Dict[str, Any],
    daily: Dict[str, Any],
    materials: Dict[str, Any],
    files: Dict[str, Any],
) -> Dict[str, Any]:
    settings = load_settings()
    layers = materials.get("layers") or {}

    def _layer_id(key: str):
        item = layers.get(key)
        if isinstance(item, list):
            item = item[0] if item else None
        if isinstance(item, dict):
            return item.get("id")
        # fall back to top-level cast keys
        top = materials.get(key)
        if isinstance(top, list):
            top = top[0] if top else None
        if isinstance(top, dict):
            return top.get("id")
        if key == "animals":
            return (materials.get("hero") or {}).get("id") if isinstance(materials.get("hero"), dict) else None
        if key == "fortune":
            return (materials.get("accent") or {}).get("id") if isinstance(materials.get("accent"), dict) else None
        return None

    hero = materials.get("hero") or materials.get("animals") or {}
    if not isinstance(hero, dict):
        hero = {}

    return {
        "date": yyyymmdd,
        "version": settings.get("version", "0.1.0"),
        "generatedAt": datetime.now().isoformat(timespec="seconds"),
        "fortune": {
            "level": fortune.get("fortuneLevel"),
            "score": fortune.get("fortuneScore"),
            "ganzhi": fortune.get("dayGanZhi"),
            "source": fortune.get("source"),
        },
        "theme": {
            "main": daily.get("mainTheme"),
            "secondary": daily.get("secondaryTheme"),
        },
        "materialIds": materials.get("allIds", []),
        "heroId": hero.get("id"),
        "layers": {
            k: _layer_id(k)
            for k in ("background", "nature", "plants", "animals", "fortune")
        },
        "files": files,
        "publishStatus": "pending_review",
    }


def write_publish(publish_dir: Path, yyyymmdd: str, status: str = "draft") -> str:
    publish_dir.mkdir(parents=True, exist_ok=True)
    data = {
        "date": yyyymmdd,
        "status": status,
        "channels": {
            "youtube": False,
            "instagram": False,
            "facebook": False,
            "threads": False,
        },
        "confirmedAt": None,
        "publishedAt": None,
        "note": "第一階段不自動發布，需人工確認。",
    }
    path = publish_dir / "publish.json"
    save_json(path, data)
    return str(path)
