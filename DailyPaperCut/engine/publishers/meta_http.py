"""Small HTTP helpers for Meta Graph / Threads."""
from __future__ import annotations

import time
from typing import Any, Dict, Optional, Tuple

import requests


def graph_error_message(payload: Any, fallback: str = "Meta API 錯誤") -> str:
    if not isinstance(payload, dict):
        return fallback
    err = payload.get("error") or {}
    if isinstance(err, dict):
        msg = err.get("message") or err.get("error_user_msg") or ""
        code = err.get("code")
        sub = err.get("error_subcode")
        parts = [str(msg)]
        if code is not None:
            parts.append(f"code={code}")
        if sub is not None:
            parts.append(f"subcode={sub}")
        return " ".join(p for p in parts if p).strip() or fallback
    return fallback


def request_json(
    method: str,
    url: str,
    *,
    params: Optional[Dict[str, Any]] = None,
    data: Optional[Dict[str, Any]] = None,
    files: Optional[Dict[str, Any]] = None,
    timeout: int = 120,
) -> Tuple[bool, Dict[str, Any], int]:
    try:
        resp = requests.request(
            method,
            url,
            params=params,
            data=data,
            files=files,
            timeout=timeout,
        )
    except requests.RequestException as e:
        return False, {"error": {"message": str(e)}}, 0

    try:
        payload = resp.json()
    except ValueError:
        payload = {"error": {"message": (resp.text or "")[:500] or f"HTTP {resp.status_code}"}}

    if not isinstance(payload, dict):
        payload = {"data": payload}

    ok = 200 <= resp.status_code < 300 and "error" not in payload
    return ok, payload, resp.status_code


def poll_until(
    check_fn,
    *,
    ok_values=("FINISHED",),
    fail_values=("ERROR", "EXPIRED"),
    timeout_sec: int = 180,
    interval_sec: float = 3.0,
) -> Dict[str, Any]:
    """
    check_fn() -> dict with at least status_code or status.
    """
    deadline = time.time() + timeout_sec
    last: Dict[str, Any] = {}
    while time.time() < deadline:
        last = check_fn() or {}
        status = str(last.get("status_code") or last.get("status") or "").upper()
        if status in ok_values:
            return {"ok": True, **last}
        if status in fail_values:
            return {
                "ok": False,
                "error": last.get("error") or last.get("status") or status,
                **last,
            }
        time.sleep(interval_sec)
    return {"ok": False, "error": "等待媒體處理逾時", **last}
