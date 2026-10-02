#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Trial: theme → one Background paper-cut → preview."""
from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))

try:
    from dotenv import load_dotenv
except ImportError:
    def load_dotenv(*args, **kwargs):
        return False

load_dotenv(ROOT / ".env")

from engine.theme_set_generator import compose_theme_set_preview, generate_theme_set


def main():
    theme = "財運開展"
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if args:
        theme = args[0]
    placeholder = "--placeholder" in sys.argv
    print(f"THEME={theme} placeholder={placeholder}", flush=True)
    materials = generate_theme_set(
        theme,
        seed=20261016,
        use_placeholder=placeholder,
        workers=1,
    )
    out = Path(materials["setDir"]) / "preview.png"
    compose_theme_set_preview(materials, out)
    bg = materials.get("background") or {}
    summary = {
        "theme": theme,
        "profileKey": materials.get("themeProfile", {}).get("profileKey"),
        "style": materials.get("style"),
        "palette": materials.get("palette"),
        "template": materials.get("template"),
        "setId": materials.get("themeSetId"),
        "combo": "單張主題剪紙背景（Background）",
        "background": {"id": bg.get("id"), "file": bg.get("file"), "method": bg.get("method")},
        "preview": str(out.with_suffix(".jpg") if out.with_suffix(".jpg").exists() else out),
    }
    print(json.dumps(summary, ensure_ascii=False, indent=2), flush=True)


if __name__ == "__main__":
    main()
