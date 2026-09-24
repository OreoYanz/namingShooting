/** 真實案例：字卡輪換 */
(function () {
  const root = document.querySelector("[data-cases]");
  if (!root) return;

  const track = root.querySelector("[data-cases-track]");
  const empty = root.querySelector("[data-cases-empty]");
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
      svc === "liunian"
        ? c.macroStage || "流年觀察"
        : c.image || "";
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

  function render(cases) {
    if (!cases.length) {
      track.innerHTML = "";
      if (empty) empty.hidden = false;
      if (dots) dots.innerHTML = "";
      return;
    }
    if (empty) empty.hidden = true;
    track.innerHTML = cases.map(cardHtml).join("");
    setupRotate(cases.length);
  }

  function setupRotate(count) {
    const cards = Array.prototype.slice.call(track.querySelectorAll(".case-card"));
    if (!cards.length) return;

    let index = 0;
    const desktop = window.matchMedia("(min-width: 720px)").matches;
    const visible = Math.min(desktop ? 3 : 1, count);
    let timer = null;

    function paintDots() {
      if (!dots) return;
      if (count <= visible) {
        dots.innerHTML = "";
        return;
      }
      const pages = Math.ceil(count / visible);
      const page = Math.floor(index / visible) % pages;
      dots.innerHTML = Array.from({ length: pages }, function (_, i) {
        return (
          '<button type="button" class="case-dot' +
          (i === page ? " is-active" : "") +
          '" data-page="' +
          i +
          '" aria-label="第 ' +
          (i + 1) +
          ' 頁"></button>'
        );
      }).join("");
      dots.querySelectorAll("[data-page]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          index = Number(btn.getAttribute("data-page") || 0) * visible;
          show();
          restart();
        });
      });
    }

    function show() {
      cards.forEach(function (card, i) {
        const on = i >= index && i < index + visible;
        card.classList.toggle("is-visible", on);
        card.hidden = !on;
      });
      paintDots();
    }

    function next() {
      if (count <= visible) return;
      index = (index + visible) % count;
      show();
    }

    function restart() {
      if (timer) clearInterval(timer);
      if (count > visible) timer = setInterval(next, 5200);
    }

    show();
    restart();
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
