"""Meta / Threads credentials from environment."""
from __future__ import annotations

import os
from dataclasses import dataclass
from typing import Any, Dict


DEFAULT_GRAPH_VERSION = "v21.0"
DEFAULT_PUBLIC_BASE = "https://mingxu.mingxu.workers.dev"


@dataclass
class MetaConfig:
    page_id: str
    page_token: str
    ig_user_id: str
    ig_token: str
    threads_user_id: str
    threads_token: str
    public_base_url: str
    graph_version: str

    def summary(self) -> Dict[str, Any]:
        def mask(v: str) -> str:
            if not v:
                return "(empty)"
            if len(v) <= 8:
                return "****"
            return v[:4] + "…" + v[-4:]

        return {
            "page_id": self.page_id or "(empty)",
            "page_token": mask(self.page_token),
            "ig_user_id": self.ig_user_id or "(empty)",
            "ig_token": mask(self.ig_token),
            "threads_user_id": self.threads_user_id or "(empty)",
            "threads_token": mask(self.threads_token),
            "public_base_url": self.public_base_url,
            "graph_version": self.graph_version,
            "facebook_ready": bool(self.page_id and self.page_token),
            "instagram_ready": bool(self.ig_user_id and self.ig_token),
            "threads_ready": bool(self.threads_user_id and self.threads_token),
        }


def load_meta_config() -> MetaConfig:
    page_token = (os.getenv("META_PAGE_ACCESS_TOKEN") or "").strip()
    ig_token = (os.getenv("INSTAGRAM_ACCESS_TOKEN") or "").strip() or page_token
    return MetaConfig(
        page_id=(os.getenv("META_PAGE_ID") or "").strip(),
        page_token=page_token,
        ig_user_id=(os.getenv("INSTAGRAM_BUSINESS_ACCOUNT_ID") or "").strip(),
        ig_token=ig_token,
        threads_user_id=(os.getenv("THREADS_USER_ID") or "").strip(),
        threads_token=(os.getenv("THREADS_ACCESS_TOKEN") or "").strip(),
        public_base_url=(os.getenv("PUBLIC_MEDIA_BASE_URL") or DEFAULT_PUBLIC_BASE).rstrip("/"),
        graph_version=(os.getenv("META_GRAPH_VERSION") or DEFAULT_GRAPH_VERSION).strip(),
    )


def public_asset_url(cfg: MetaConfig, relative_path: str) -> str:
    rel = relative_path.lstrip("/").replace("\\", "/")
    return f"{cfg.public_base_url}/{rel}"
