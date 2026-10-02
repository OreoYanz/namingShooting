from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict


ROOT = Path(__file__).resolve().parents[1]


def load_json(path: Path) -> Dict[str, Any]:
    with path.open("r", encoding="utf-8") as f:
        return json.load(f)


def save_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)


def load_settings() -> Dict[str, Any]:
    return load_json(ROOT / "config" / "settings.json")


def load_style_bible() -> Dict[str, Any]:
    return load_json(ROOT / "config" / "StyleBible.json")


def day_dir(yyyymmdd: str) -> Path:
    settings = load_settings()
    out_root = Path(settings["paths"]["outputRoot"])
    if not out_root.is_absolute():
        out_root = ROOT / out_root
    return out_root / yyyymmdd


def ensure_day_folders(yyyymmdd: str) -> Path:
    base = day_dir(yyyymmdd)
    for name in ("data", "source", "cutout", "scene", "gif", "short", "social", "publish"):
        (base / name).mkdir(parents=True, exist_ok=True)
    return base


def parse_date_arg(value: str):
    """Accept YYYYMMDD or YYYY/MM/DD or YYYY-MM-DD."""
    from datetime import datetime

    raw = (value or "").strip().replace("/", "").replace("-", "")
    if len(raw) != 8 or not raw.isdigit():
        raise ValueError("日期格式請用 YYYYMMDD，例如 20261002")
    return datetime.strptime(raw, "%Y%m%d").date(), raw
