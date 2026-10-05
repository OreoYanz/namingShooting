/** 真實案例：水平輪播（transform 緩動＋無縫循環） */
(function () {
  const root = document.querySelector("[data-cases]");
  if (!root) return;

  const viewport = root.querySelector("[data-cases-viewport]");
  const track = root.querySelector("[data-cases-track]");
  const empty = root.querySelector("[data-cases-empty]");
  const prevBtn = root.querySelector("[data-cases-prev]");
  const nextBtn = root.querySelector("[data-cases-next]");
  const dots = root.querySelector("[data-cases-dots]");
  if (!track || !viewport) return;

  const SERVICE = {
    newborn: "新生兒命名",
    rename: "專業改名",
    liunian: "流年分析",
  };

  const AUTO_MS = 4800;
  const EASE_MS = 820;
  const reduceMotion =
    window.matchMedia &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  function esc(s) {
    return String(s == null ? "" : s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function cardHtml(c) {
    const svc = c.service === "rename" || c.service === "liunian" ? c.service : "newborn";
    const label = c.serviceLabel || SERVICE[svc] || "名序服務";
    const highlight =
      svc === "liunian" ? c.macroStage || "流年觀察" : c.image || "";
    const highlightLabel = svc === "liunian" ? "宏觀階段" : "意象";
    return (
      '<article class="case-card case-card--' +
      svc +
      '">' +
      '<span class="case-badge">' +
      esc(label) +
      "</span>" +
      '<p class="case-name">' +
      esc(c.displayName || "OO") +
      "</p>" +
      '<ul class="case-meta">' +
      "<li><span>生日</span><strong>" +
      esc(c.birthDate || "—") +
      "</strong></li>" +
      "<li><span>地區</span><strong>" +
      esc(c.region || "—") +
      "</strong></li>" +
      "<li><span>性別</span><strong>" +
      esc(c.gender || "—") +
      "</strong></li>" +
      "</ul>" +
      (highlight
        ? '<p class="case-highlight"><span>' +
          esc(highlightLabel) +
          "</span>" +
          esc(highlight) +
          "</p>"
        : "") +
      "</article>"
    );
  }

  /** easeOutQuint — 進場稍快、收尾柔和 */
  function easeOutQuint(t) {
    return 1 - Math.pow(1 - t, 5);
  }

  function setupSlider(count) {
    const realCards = Array.prototype.slice.call(track.querySelectorAll(".case-card"));
    if (!realCards.length) return;

    // 複製一輪以做無縫向前循環
    const cloneFrag = document.createDocumentFragment();
    realCards.forEach(function (card) {
      const clone = card.cloneNode(true);
      clone.setAttribute("aria-hidden", "true");
      clone.classList.add("case-card--clone");
      cloneFrag.appendChild(clone);
    });
    track.appendChild(cloneFrag);

    root.classList.add("cases--transform");
    viewport.classList.add("is-transform");
    track.style.willChange = "transform";

    let index = 0; // 0 .. count-1（邏輯位置）；動畫可暫到 count（克隆區）
    let offset = 0;
    let animating = false;
    let timer = null;
    let paused = false;
    let raf = 0;

    function gap() {
      const style = window.getComputedStyle(track);
      return parseFloat(style.columnGap || style.gap || "0") || 0;
    }

    function step() {
      const first = track.querySelector(".case-card");
      if (!first) return viewport.clientWidth;
      return first.getBoundingClientRect().width + gap();
    }

    function visibleCount() {
      const s = step();
      if (s <= 0) return 1;
      return Math.max(1, Math.round(viewport.clientWidth / s));
    }

    function maxIndex() {
      return Math.max(0, count - visibleCount());
    }

    function applyTransform(x, withTransition) {
      offset = x;
      if (withTransition) {
        track.style.transition =
          "transform " + EASE_MS + "ms cubic-bezier(0.22, 1, 0.36, 1)";
      } else {
        track.style.transition = "none";
      }
      track.style.transform = "translate3d(" + -x + "px,0,0)";
    }

    function syncInstant() {
      applyTransform(index * step(), false);
      // 強制 reflow，確保下一幀 transition 生效
      void track.offsetWidth;
    }

    function paintDots() {
      if (!dots) return;
      const max = maxIndex();
      if (max <= 0) {
        dots.innerHTML = "";
        return;
      }
      const active = Math.min(index, max);
      dots.innerHTML = Array.from({ length: max + 1 }, function (_, i) {
        return (
          '<button type="button" class="case-dot' +
          (i === active ? " is-active" : "") +
          '" data-i="' +
          i +
          '" aria-label="第 ' +
          (i + 1) +
          ' 張"></button>'
        );
      }).join("");
      dots.querySelectorAll("[data-i]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          goTo(Number(btn.getAttribute("data-i") || 0));
          restart();
        });
      });
    }

    function animateTo(targetX, onDone) {
      if (reduceMotion) {
        applyTransform(targetX, false);
        if (onDone) onDone();
        return;
      }
      if (raf) cancelAnimationFrame(raf);
      animating = true;
      const from = offset;
      const dist = targetX - from;
      if (Math.abs(dist) < 0.5) {
        applyTransform(targetX, false);
        animating = false;
        if (onDone) onDone();
        return;
      }
      const start = performance.now();
      track.style.transition = "none";

      function frame(now) {
        const t = Math.min(1, (now - start) / EASE_MS);
        const eased = easeOutQuint(t);
        applyTransform(from + dist * eased, false);
        if (t < 1) {
          raf = requestAnimationFrame(frame);
        } else {
          animating = false;
          raf = 0;
          if (onDone) onDone();
        }
      }
      raf = requestAnimationFrame(frame);
    }

    function goTo(i, opts) {
      opts = opts || {};
      const max = maxIndex();
      if (max <= 0) {
        index = 0;
        syncInstant();
        paintDots();
        return;
      }

      const forwardLoop = opts.forwardLoop;
      const backwardLoop = opts.backwardLoop;

      if (forwardLoop) {
        // 向前滑入克隆區第 0 頁，再瞬間歸位（視覺連續、無倒退）
        animateTo(count * step(), function () {
          index = 0;
          syncInstant();
          paintDots();
        });
        return;
      }

      if (backwardLoop) {
        // 瞬間跳到克隆區同畫面，再向右滑回最後一頁
        applyTransform(count * step(), false);
        void track.offsetWidth;
        index = max;
        animateTo(max * step(), function () {
          paintDots();
        });
        return;
      }

      const target = Math.max(0, Math.min(max, i));
      index = target;
      animateTo(target * step(), function () {
        paintDots();
      });
      paintDots();
    }

    function next() {
      const max = maxIndex();
      if (max <= 0 || animating) return;
      if (index >= max) goTo(0, { forwardLoop: true });
      else goTo(index + 1);
    }

    function prev() {
      const max = maxIndex();
      if (max <= 0 || animating) return;
      if (index <= 0) goTo(max, { backwardLoop: true });
      else goTo(index - 1);
    }

    function restart() {
      if (timer) clearInterval(timer);
      timer = null;
      if (maxIndex() > 0 && !reduceMotion) {
        timer = setInterval(function () {
          if (!paused && !animating) next();
        }, AUTO_MS);
      }
    }

    if (prevBtn) {
      prevBtn.addEventListener("click", function () {
        prev();
        restart();
      });
    }
    if (nextBtn) {
      nextBtn.addEventListener("click", function () {
        next();
        restart();
      });
    }

    root.addEventListener("mouseenter", function () {
      paused = true;
    });
    root.addEventListener("mouseleave", function () {
      paused = false;
    });
    root.addEventListener("focusin", function () {
      paused = true;
    });
    root.addEventListener("focusout", function () {
      paused = false;
    });

    // 觸控拖曳（跟手＋鬆手吸附）
    let drag = null;
    viewport.addEventListener(
      "pointerdown",
      function (e) {
        if (animating || e.button === 2) return;
        paused = true;
        if (timer) clearInterval(timer);
        drag = {
          id: e.pointerId,
          x0: e.clientX,
          origin: offset,
          moved: false,
        };
        try {
          viewport.setPointerCapture(e.pointerId);
        } catch (err) {
          /* ignore */
        }
        track.style.transition = "none";
      },
      { passive: true }
    );

    viewport.addEventListener(
      "pointermove",
      function (e) {
        if (!drag || e.pointerId !== drag.id) return;
        const dx = e.clientX - drag.x0;
        if (Math.abs(dx) > 4) drag.moved = true;
        const maxX = (count + maxIndex()) * step();
        const nextX = Math.max(0, Math.min(maxX, drag.origin - dx));
        applyTransform(nextX, false);
      },
      { passive: true }
    );

    function endDrag(e) {
      if (!drag || (e && e.pointerId !== drag.id)) return;
      const dx = (e ? e.clientX : drag.x0) - drag.x0;
      const s = step();
      const raw = offset / s;
      let target = Math.round(raw);
      if (drag.moved && Math.abs(dx) > 40) {
        target = dx < 0 ? Math.ceil(raw) : Math.floor(raw);
      }
      const max = maxIndex();
      // 落在克隆區 → 對應回真實 index
      if (target >= count) {
        index = Math.min(max, target - count);
        syncInstant();
        animateTo(index * step(), paintDots);
      } else {
        index = Math.max(0, Math.min(max, target));
        animateTo(index * step(), paintDots);
      }
      drag = null;
      paused = false;
      restart();
    }

    viewport.addEventListener("pointerup", endDrag);
    viewport.addEventListener("pointercancel", endDrag);

    function onResize() {
      syncInstant();
      paintDots();
      restart();
    }

    syncInstant();
    paintDots();
    restart();
    window.addEventListener("resize", onResize);
  }

  function render(cases) {
    if (!cases.length) {
      track.innerHTML = "";
      track.style.transform = "";
      if (empty) empty.hidden = false;
      if (dots) dots.innerHTML = "";
      root.classList.remove("has-cases");
      return;
    }
    if (empty) empty.hidden = true;
    root.classList.add("has-cases");
    track.innerHTML = cases.map(cardHtml).join("");
    setupSlider(cases.length);
  }

  const src = root.getAttribute("data-cases-src") || "data/cases.json";
  fetch(src + (src.indexOf("?") >= 0 ? "&" : "?") + "v=" + Date.now())
    .then(function (r) {
      if (!r.ok) throw new Error("cases load failed");
      return r.json();
    })
    .then(function (data) {
      const list = Array.isArray(data) ? data : data && data.cases ? data.cases : [];
      render(list.filter(Boolean));
    })
    .catch(function () {
      render([]);
    });
})();
