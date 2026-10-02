"""Write social text files."""
from __future__ import annotations

from pathlib import Path
from typing import Dict


def write_social(social_dir: Path, copy: Dict[str, str]) -> Dict[str, str]:
    social_dir.mkdir(parents=True, exist_ok=True)
    mapping = {
        "youtube": "youtube.txt",
        "instagram": "instagram.txt",
        "facebook": "facebook.txt",
        "threads": "threads.txt",
    }
    out = {}
    for key, fname in mapping.items():
        path = social_dir / fname
        path.write_text(copy.get(key, ""), encoding="utf-8")
        out[key] = str(path)
    return out
