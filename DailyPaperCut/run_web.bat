@echo off
cd /d "%~dp0"
uvicorn ui.web_app:app --reload --port 8765
