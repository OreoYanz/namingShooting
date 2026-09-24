/** 近五年常見名字文字雲（螺旋避碰放置） */
(function () {
  const canvas = document.getElementById("wordcloud");
  if (!canvas) return;
  const ctx = canvas.getContext("2d");
  let words = [];

  function fits(x, y, w, h, boxes, pad) {
    for (const b of boxes) {
      if (
        x < b.x + b.w + pad &&
        x + w + pad > b.x &&
        y < b.y + b.h + pad &&
        y + h + pad > b.y
      ) {
        return false;
      }
    }
    return true;
  }

  function placeWord(item, maxW, cw, ch, boxes) {
    const size = Math.round(13 + (item.weight / maxW) * 26);
    ctx.font = `600 ${size}px "Microsoft JhengHei", "微軟正黑體", sans-serif`;
    const tw = ctx.measureText(item.text).width;
    const th = size * 1.15;
    const cx = cw / 2;
    const cy = ch / 2;
    const pad = 6;

    for (let step = 0; step < 900; step++) {
      const angle = step * 0.42;
      const radius = 6 + step * 1.65;
      let x = cx + Math.cos(angle) * radius - tw / 2;
      let y = cy + Math.sin(angle) * radius * 0.72 + th * 0.2;
      if (x < pad || y < th || x + tw > cw - pad || y > ch - pad) continue;
      if (fits(x, y - th * 0.85, tw, th, boxes, pad)) {
        boxes.push({ x, y: y - th * 0.85, w: tw, h: th });
        return { x, y, size, tw, alpha: 0.38 + (item.weight / maxW) * 0.52 };
      }
    }
    return null;
  }

  function resize() {
    const dpr = window.devicePixelRatio || 1;
    const rect = canvas.getBoundingClientRect();
    canvas.width = Math.max(1, Math.floor(rect.width * dpr));
    canvas.height = Math.max(1, Math.floor(rect.height * dpr));
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    draw();
  }

  function draw() {
    const w = canvas.clientWidth;
    const h = canvas.clientHeight;
    ctx.clearRect(0, 0, w, h);
    if (!words.length) return;

    const sorted = [...words].sort((a, b) => b.weight - a.weight);
    const maxW = Math.max(...sorted.map((x) => x.weight));
    const boxes = [];

    sorted.forEach((item) => {
      const p = placeWord(item, maxW, w, h, boxes);
      if (!p) return;
      ctx.font = `600 ${p.size}px "Microsoft JhengHei", "微軟正黑體", sans-serif`;
      ctx.fillStyle = `rgba(166, 124, 61, ${p.alpha})`;
      ctx.fillText(item.text, p.x, p.y);
    });
  }

  fetch("data/wordcloud.json")
    .then((r) => r.json())
    .then((data) => {
      words = data;
      resize();
    })
    .catch(() => {
      words = [
        { text: "承恩", weight: 10 },
        { text: "品睿", weight: 9 },
        { text: "子晴", weight: 9 },
        { text: "詩涵", weight: 8 },
      ];
      resize();
    });

  window.addEventListener("resize", resize);
})();
