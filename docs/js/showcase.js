/** 成果展示：報告翻頁預覽 */
(function () {
  const root = document.querySelector("[data-showcase]");
  if (!root) return;

  function initPanel(panel) {
    const dataEl = panel.querySelector(".showcase-data");
    if (!dataEl) return;
    let slides = [];
    try {
      slides = JSON.parse(dataEl.textContent || "[]");
    } catch (e) {
      return;
    }
    if (!slides.length) return;

    let index = 0;
    const img = panel.querySelector("[data-slide-img]");
    const label = panel.querySelector("[data-slide-label]");
    const counter = panel.querySelector("[data-slide-index]");
    const thumbs = panel.querySelectorAll("[data-thumbs] button");
    const prev = panel.querySelector("[data-prev]");
    const next = panel.querySelector("[data-next]");

    function show(i) {
      index = (i + slides.length) % slides.length;
      const slide = slides[index];
      if (img) {
        img.classList.remove("is-flip");
        void img.offsetWidth;
        img.src = slide.src;
        img.alt = slide.label + "預覽";
        img.classList.add("is-flip");
      }
      if (label) label.textContent = slide.label;
      if (counter) counter.textContent = index + 1 + " / " + slides.length;
      thumbs.forEach((btn) => {
        const active = Number(btn.getAttribute("data-i")) === index;
        btn.classList.toggle("is-active", active);
        btn.setAttribute("aria-current", active ? "true" : "false");
      });
    }

    if (prev) prev.addEventListener("click", () => show(index - 1));
    if (next) next.addEventListener("click", () => show(index + 1));
    thumbs.forEach((btn) => {
      btn.addEventListener("click", () => show(Number(btn.getAttribute("data-i")) || 0));
    });

    panel._showcaseShow = show;
    show(0);
  }

  root.querySelectorAll(".showcase-panel").forEach(initPanel);

  const tabs = root.querySelectorAll(".showcase-tab");
  const panels = root.querySelectorAll(".showcase-panel");

  tabs.forEach((tab) => {
    tab.addEventListener("click", () => {
      const id = tab.getAttribute("data-panel");
      tabs.forEach((t) => {
        const on = t === tab;
        t.classList.toggle("is-active", on);
        t.setAttribute("aria-selected", on ? "true" : "false");
      });
      panels.forEach((p) => {
        const on = p.getAttribute("data-panel-id") === id;
        p.classList.toggle("is-active", on);
        if (on) p.removeAttribute("hidden");
        else p.setAttribute("hidden", "");
      });
    });
  });
})();
