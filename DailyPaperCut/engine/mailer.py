"""Send daily social copy + MP4 to Gmail after generation."""
from __future__ import annotations

import mimetypes
import os
import smtplib
import ssl
from email.message import EmailMessage
from pathlib import Path
from typing import Any, Dict, Optional


def _env(name: str, default: str = "") -> str:
    return (os.getenv(name) or default).strip()


def mail_enabled() -> bool:
    # 每次檢查前重讀 .env，避免工作台長駐時仍用舊密碼
    try:
        from dotenv import load_dotenv
        from . import ROOT

        load_dotenv(ROOT / ".env", override=True)
    except Exception:
        pass
    if _env("GMAIL_DISABLE", "").lower() in ("1", "true", "yes"):
        return False
    return bool(_env("GMAIL_USER") and _env("GMAIL_APP_PASSWORD"))


def _read_social_body(social_dir: Path) -> str:
    parts = []
    for name, title in (
        ("facebook", "Facebook"),
        ("instagram", "Instagram"),
        ("threads", "Threads"),
        ("youtube", "YouTube"),
    ):
        p = social_dir / f"{name}.txt"
        text = p.read_text(encoding="utf-8").strip() if p.exists() else ""
        parts.append(f"【{title}】\n{text or '（無文案）'}\n")
    return "\n".join(parts).strip()


def send_daily_social_mail(
    yyyymmdd: str,
    *,
    social_dir: Path,
    mp4_path: Optional[Path] = None,
    extra_note: str = "",
) -> Dict[str, Any]:
    """
    Email social captions (+ MP4 attachment) via Gmail SMTP.
    Requires GMAIL_USER + GMAIL_APP_PASSWORD (Google App Password).
    """
    if not mail_enabled():
        return {
            "ok": False,
            "skipped": True,
            "error": "未設定 GMAIL_USER／GMAIL_APP_PASSWORD，略過寄信",
        }

    user = _env("GMAIL_USER")
    # Google 應用程式密碼介面常顯示成「xxxx xxxx xxxx xxxx」，空白可忽略
    password = _env("GMAIL_APP_PASSWORD").replace(" ", "").replace("\u3000", "")
    to_addr = _env("GMAIL_TO") or user
    from_addr = _env("GMAIL_FROM") or user

    subject = _env("GMAIL_SUBJECT_PREFIX", "名序每日吉祥") + f" {yyyymmdd}｜社群文案＋MP4"
    body = (
        f"日期：{yyyymmdd}\n"
        f"用途：手動發布 Facebook／Instagram／Threads／YouTube\n"
        f"{extra_note.strip() + chr(10) if extra_note.strip() else ''}\n"
        f"{_read_social_body(Path(social_dir))}\n"
    )

    msg = EmailMessage()
    msg["Subject"] = subject
    msg["From"] = from_addr
    msg["To"] = to_addr
    msg.set_content(body)

    attached = []
    if mp4_path and Path(mp4_path).exists():
        path = Path(mp4_path)
        ctype, _ = mimetypes.guess_type(str(path))
        maintype, subtype = (ctype or "video/mp4").split("/", 1)
        data = path.read_bytes()
        # Gmail ~25MB limit; skip attach if too large and note in body
        if len(data) > 22 * 1024 * 1024:
            msg.set_content(
                body
                + f"\n\n（MP4 過大未附加：{path}，約 {len(data) // (1024 * 1024)} MB，請至工作台目錄下載）\n"
            )
        else:
            msg.add_attachment(
                data,
                maintype=maintype,
                subtype=subtype,
                filename=path.name,
            )
            attached.append(path.name)
    else:
        msg.set_content(body + "\n\n（尚無 MP4 檔可附加）\n")

    host = _env("GMAIL_SMTP_HOST", "smtp.gmail.com")
    # 多數公司網／部分環境會擋 587；Gmail SSL 465 較常可通
    port = int(_env("GMAIL_SMTP_PORT", "465") or "465")

    try:
        context = ssl.create_default_context()
        if port == 465:
            with smtplib.SMTP_SSL(host, port, timeout=60, context=context) as smtp:
                smtp.login(user, password)
                smtp.send_message(msg)
        else:
            with smtplib.SMTP(host, port, timeout=60) as smtp:
                smtp.ehlo()
                smtp.starttls(context=context)
                smtp.ehlo()
                smtp.login(user, password)
                smtp.send_message(msg)
    except Exception as e:
        return {"ok": False, "error": str(e), "to": to_addr}

    return {
        "ok": True,
        "to": to_addr,
        "subject": subject,
        "attached": attached,
    }
