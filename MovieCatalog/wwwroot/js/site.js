// Movies front-end: toasts, delete confirmations and the SignalR live connection.
window.cv = (() => {
    const me = document.body.dataset.user || "";
    const esc = s => String(s ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));

    // ---------- toasts ----------
    function toast(html, type = "info", timeout = 5000) {
        const box = document.getElementById("toasts");
        const el = document.createElement("div");
        el.className = `cv-toast ${type}`;
        el.innerHTML = `<div class="cv-toast-body">${html}</div><button class="cv-toast-x" aria-label="Close">&times;</button>`;
        el.querySelector("button").onclick = () => close();
        box.appendChild(el);
        requestAnimationFrame(() => el.classList.add("show"));
        const close = () => { el.classList.remove("show"); setTimeout(() => el.remove(), 300); };
        setTimeout(close, timeout);
    }
    document.querySelectorAll("[data-toast]").forEach(el => toast(esc(el.dataset.toast), el.dataset.type));

    // ---------- confirm dialog for <form data-confirm="..."> ----------
    const modalEl = document.getElementById("confirmModal");
    const modal = modalEl ? new bootstrap.Modal(modalEl) : null;
    let pendingForm = null;
    document.addEventListener("submit", e => {
        const form = e.target;
        if (!form.dataset.confirm || form.dataset.confirmed) return;
        e.preventDefault();
        pendingForm = form;
        document.getElementById("confirmText").textContent = form.dataset.confirm;
        modal.show();
    });
    document.getElementById("confirmOk")?.addEventListener("click", () => {
        if (!pendingForm) return;
        pendingForm.dataset.confirmed = "1";
        modal.hide();
        pendingForm.submit();
    });

    // ---------- SignalR ----------
    const dot = document.getElementById("presence-dot");
    const text = document.getElementById("presence-text");
    const setStatus = (state, label) => { dot.className = `cv-dot ${state}`; text.textContent = label; };

    const conn = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/catalog")
        .withAutomaticReconnect()
        .build();

    conn.on("PresenceChanged", p => {
        setStatus("on", `${p.users} member${p.users === 1 ? "" : "s"} · ${p.guests} guest${p.guests === 1 ? "" : "s"}`);
    });

    conn.on("MovieAdded", m => {
        if (m.byUser !== me)
            toast(`🎬 <strong>${esc(m.by)}</strong> added <a href="/Movies/Details/${m.id}">${esc(m.title)} (${m.releaseYear})</a>`, "live", 8000);
        document.dispatchEvent(new CustomEvent("cv:movieAdded", { detail: m }));
    });

    conn.on("ReviewsChanged", r => {
        // update every rating badge of this movie on the current page
        document.querySelectorAll(`[data-rating-for="${r.movieId}"]`).forEach(el => {
            el.textContent = r.average == null ? "–" : r.average.toFixed(1);
            el.classList.remove("cv-pulse"); void el.offsetWidth; el.classList.add("cv-pulse");
        });
        if (r.byUser !== me) {
            const link = `<a href="/Movies/Details/${r.movieId}">${esc(r.movieTitle)}</a>`;
            const msg = {
                posted: `⭐ <strong>${esc(r.by)}</strong> rated ${link} <strong>${r.rating}/10</strong>`,
                updated: `✏️ <strong>${esc(r.by)}</strong> updated their review of ${link}`,
                deleted: `🗑️ <strong>${esc(r.by)}</strong> removed their review of ${link}`
            }[r.action];
            if (msg) toast(msg, "live", 7000);
        }
        document.dispatchEvent(new CustomEvent("cv:reviewsChanged", { detail: r }));
    });

    conn.onreconnecting(() => setStatus("wait", "reconnecting…"));
    conn.onreconnected(() => setStatus("on", "live"));
    conn.onclose(() => setStatus("off", "offline"));

    (async function start() {
        try { await conn.start(); }
        catch { setStatus("off", "offline – retrying"); setTimeout(start, 3000); }
    })();

    return { me, esc, toast, connection: conn };
})();
