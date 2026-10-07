# -*- coding: utf-8 -*-
import re
import urllib.request
from pathlib import Path

out = Path(__file__).resolve().parents[1] / "winforms" / "_gen" / "_ris_cache"
out.mkdir(parents=True, exist_ok=True)

url = "https://www.ris.gov.tw/documents/html/5/2/popudata-quart-pub.html"
html = urllib.request.urlopen(url, timeout=60).read().decode("utf-8", "ignore")
hrefs = re.findall(r'href=["\']([^"\']+)["\']', html, re.I)
base = "https://www.ris.gov.tw"
cands = []
for h in hrefs:
    full = h if h.startswith("http") else (base + h if h.startswith("/") else base + "/documents/html/5/2/" + h)
    if ".pdf" in full.lower() or "Download" in full or "data/5/2" in full or "data/8/5" in full:
        cands.append(full)
print("candidates", len(cands))
for c in cands:
    print(c)

# Also try known yearbook attachment patterns from previous RIS URLs
known = [
    "https://www.ris.gov.tw/documents/data/5/2/112namestat.pdf",
]
for u in known:
    name = u.rsplit("/", 1)[-1]
    dest = out / name
    if dest.exists() and dest.stat().st_size > 1000:
        print("exists", dest)
        continue
    print("download", u)
    try:
        urllib.request.urlretrieve(u, dest)
        print("saved", dest, dest.stat().st_size)
    except Exception as e:
        print("fail", e)
