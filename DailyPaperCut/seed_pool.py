#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Seed placeholder PNGs for library items missing files (current asset categories only)."""
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))

from engine import ROOT as EROOT, save_json
from engine.ai_image import _pillow_placeholder
from engine.material_pool import category_color, get_categories, load_library


def main():
    cats = get_categories(sync_library=True)
    known = set(cats)
    lib = load_library()
    n = 0
    for it in lib.get("items", []):
        cat = it.get("category")
        if cat not in known:
            continue
        mid = it["id"]
        rel = it.get("file") or f"materials/assets/{cat}/{mid}.png"
        abs_path = EROOT / rel
        if not abs_path.exists():
            abs_path.parent.mkdir(parents=True, exist_ok=True)
            _pillow_placeholder(abs_path, it.get("name", mid), category_color(cat))
            n += 1
        it["file"] = rel.replace("\\", "/")
        it.setdefault("method", "placeholder_seed")
    lib["categories"] = cats
    save_json(EROOT / "materials" / "library.json", lib)
    print(f"categories={cats}")
    print(f"seeded/linked known-category items, new files={n}")


if __name__ == "__main__":
    main()
