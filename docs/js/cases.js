/** 真實案例：水平滑動字卡 */
(function () {
  const root = document.querySelector("[data-cases]");
  if (!root) return;

  const viewport = root.querySelector("[data-cases-viewport]");
  const track = root.querySelector("[data-cases-track]");
  const empty = root.querySelector("[data-cases-empty]");
  const prevBtn = root.querySelector("[data-cases-prev]");
  const nextBtn = root.querySelector("[data-cases-next]");
  const dots = root.querySelector("[data-cases-dots]");
  if (!track) return;

  const SERVICE = {
    newborn: "新生兒命名",
    rename: "成人改名",
    liunian: "流年分析",
  };

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

  function setupSlider(count) {
    const scroller = viewport || track;
    const cards = Array.prototype.slice.call(track.querySelectorAll(".case-card"));
    if (!cards.length) return;

    let timer = null;
    let paused = false;

    function cardStep() {
      const first = cards[0];
      if (!first) return scroller.clientWidth;
      const style = window.getComputedStyle(track);
      const gap = parseFloat(style.columnGap || style.gap || "0") || 0;
      return first.getBoundingClientRect().width + gap;
    }

    function maxIndex() {
      const step = cardStep();
      if (step <= 0) return 0;
      const visible = Math.max(1, Math.round(scroller.clientWidth / step));
      return Math.max(0, count - visible);
    }

    function currentIndex() {
      const step = cardStep();
      if (step <= 0) return 0;
      return Math.round(scroller.scrollLeft / step);
    }

    function paintDots() {
      if (!dots) return;
      const max = maxIndex();
      if (max <= 0) {
        dots.innerHTML = "";
        return;
      }
      const active = Math.min(currentIndex(), max);
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

    function goTo(i) {
      const max = maxIndex();
      const target = ((i % (max + 1)) + (max + 1)) % (max + 1);
      scroller.scrollTo({ left: target * cardStep(), behavior: "smooth" });
    }

    function next() {
      const max = maxIndex();
      if (max <= 0) return;
      const i = currentIndex();
      goTo(i >= max ? 0 : i + 1);
    }

    function prev() {
      const max = maxIndex();
      if (max <= 0) return;
      const i = currentIndex();
      goTo(i <= 0 ? max : i - 1);
    }

    function restart() {
      if (timer) clearInterval(timer);
      if (maxIndex() > 0) {
        timer = setInterval(function () {
          if (!paused) next();
        }, 4500);
      }
    }

    if (prevBtn) prevBtn.addEventListener("click", function () { prev(); restart(); });
    if (nextBtn) nextBtn.addEventListener("click", function () { next(); restart(); });

    scroller.addEventListener("scroll", function () {
      window.clearTimeout(scroller._dotTimer);
      scroller._dotTimer = window.setTimeout(paintDots, 80);
    }, { passive: true });

    root.addEventListener("mouseenter", function () { paused = true; });
    root.addEventListener("mouseleave", function () { paused = false; });
    root.addEventListener("focusin", function () { paused = true; });
    root.addEventListener("focusout", function () { paused = false; });

    scroller.addEventListener("touchstart", function () { paused = true; }, { passive: true });
    scroller.addEventListener("touchend", function () {
      paused = false;
      restart();
    }, { passive: true });

    paintDots();
    restart();
    window.addEventListener("resize", function () {
      paintDots();
      restart();
    });
  }

  function render(cases) {
    if (!cases.length) {
      track.innerHTML = "";
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
