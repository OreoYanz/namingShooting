"""Commit + push published site/docs files to GitHub Pages."""
from __future__ import annotations

import subprocess
from pathlib import Path
from typing import Any, Dict, List

from .. import ROOT

REPO_ROOT = ROOT.parent


def _run(args: List[str], timeout: int = 180) -> subprocess.CompletedProcess:
    return subprocess.run(
        args,
        cwd=str(REPO_ROOT),
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        timeout=timeout,
        check=False,
    )


def commit_and_push_site(yyyymmdd: str) -> Dict[str, Any]:
    """
    Stage site/ + docs/ daily publish artifacts, commit, and push to origin.
    Safe subset only — does not add unrelated untracked files.
    """
    if not (REPO_ROOT / ".git").exists():
        return {"ok": False, "error": f"找不到 git 倉庫：{REPO_ROOT}"}

    paths = [
        "site/daily.html",
        "site/daily",
        "site/data/daily_latest.json",
        "site/data/daily_archive.json",
        "site/data/daily",
        "site/assets/daily",
        "site/sitemap.xml",
        "docs/daily.html",
        "docs/daily",
        "docs/data/daily_latest.json",
        "docs/data/daily_archive.json",
        "docs/data/daily",
        "docs/assets/daily",
        "docs/sitemap.xml",
        "docs/js/layout.js",
        "docs/index.html",
        "site/index.html",
        "site/js/daily.js",
        "docs/js/daily.js",
        "site/js/layout.js",
        "site/css/site.css",
        "docs/css/site.css",
    ]
    existing = [p for p in paths if (REPO_ROOT / p).exists()]
    if not existing:
        return {"ok": False, "error": "沒有可提交的官網檔案"}

    add = _run(["git", "add", "--"] + existing)
    if add.returncode != 0:
        return {
            "ok": False,
            "error": "git add 失敗",
            "stderr": (add.stderr or add.stdout or "").strip(),
        }

    status = _run(["git", "status", "--porcelain", "--"] + existing)
    staged = (status.stdout or "").strip()
    if not staged:
        return {
            "ok": True,
            "skipped": True,
            "message": "官網檔案無變更，略過 commit／push",
        }

    msg = f"Publish daily {yyyymmdd} to GitHub Pages."
    commit = _run(["git", "commit", "-m", msg])
    if commit.returncode != 0:
        out = (commit.stdout or "") + (commit.stderr or "")
        if "nothing to commit" in out.lower():
            return {
                "ok": True,
                "skipped": True,
                "message": "官網檔案無變更，略過 commit／push",
            }
        return {
            "ok": False,
            "error": "git commit 失敗",
            "stderr": out.strip(),
        }

    push = _run(["git", "push", "origin", "HEAD"], timeout=300)
    if push.returncode != 0:
        return {
            "ok": False,
            "error": "git push 失敗",
            "stderr": (push.stderr or push.stdout or "").strip(),
            "committed": True,
        }

    rev = _run(["git", "rev-parse", "--short", "HEAD"])
    sha = (rev.stdout or "").strip()
    return {
        "ok": True,
        "pushed": True,
        "commit": sha,
        "url": f"https://oreoyanz.github.io/namingShooting/daily/{yyyymmdd}.html",
        "message": f"已 commit／push（{sha or 'ok'}），GitHub Pages 稍後會更新",
    }
