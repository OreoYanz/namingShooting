/** 名序官網 · 互動與無障礙 */
(function () {
  const toggle = document.getElementById("navToggle");
  const nav = document.getElementById("siteNav");
  const backdrop = document.getElementById("navBackdrop");

  function setNavOpen(open) {
    if (!nav || !toggle) return;
    nav.classList.toggle("open", open);
    toggle.setAttribute("aria-expanded", open ? "true" : "false");
    document.body.classList.toggle("nav-open", open);
    if (backdrop) backdrop.classList.toggle("show", open);
  }

  if (toggle && nav) {
    toggle.addEventListener("click", (e) => {
      e.stopPropagation();
      setNavOpen(!nav.classList.contains("open"));
    });
    nav.querySelectorAll("a").forEach((a) => {
      a.addEventListener("click", () => setNavOpen(false));
    });
    if (backdrop) {
      backdrop.addEventListener("click", () => setNavOpen(false));
    }
    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape") setNavOpen(false);
    });
  }

  const reveals = document.querySelectorAll(".reveal");
  if (reveals.length && "IntersectionObserver" in window) {
    const io = new IntersectionObserver(
      (entries) => {
        entries.forEach((e) => {
          if (e.isIntersecting) {
            e.target.classList.add("visible");
            io.unobserve(e.target);
          }
        });
      },
      { threshold: 0.12, rootMargin: "0px 0px -40px 0px" }
    );
    reveals.forEach((el) => io.observe(el));
  } else {
    reveals.forEach((el) => el.classList.add("visible"));
  }
})();
