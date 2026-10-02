#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""CLI: python run_generate.py 20261002"""
from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))

from engine.pipeline import generate_daily


def main(argv=None):
    argv = argv or sys.argv[1:]
    date_str = argv[0] if argv else "20261002"
    result = generate_daily(date_str)
    print(json.dumps({"ok": result["ok"], "date": result["date"], "dir": result["dir"]}, ensure_ascii=False, indent=2))
    print("GIF:", result["files"].get("gif"))
    print("Short MP4:", result["files"].get("shortMp4"))
    print("Short preview:", result["files"].get("shortPreview"))
    print("Manifest:", result["files"].get("manifest"))


if __name__ == "__main__":
    main()
