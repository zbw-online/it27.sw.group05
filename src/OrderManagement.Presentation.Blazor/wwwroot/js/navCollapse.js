(function () {
    const STORAGE_KEY = "nav-collapsed";

    function apply(collapsed) {
        const shell = document.getElementById("app-shell");
        const button = document.getElementById("nav-collapse-toggle");
        if (!shell) {
            return;
        }

        shell.classList.toggle("is-nav-collapsed", collapsed);

        if (button) {
            button.setAttribute("aria-expanded", (!collapsed).toString());
            button.setAttribute("aria-label", collapsed ? "Navigation ausklappen" : "Navigation einklappen");
        }
    }

    function init() {
        let stored = null;
        try {
            stored = localStorage.getItem(STORAGE_KEY);
        } catch {
            stored = null;
        }

        apply(stored === "true");
    }

    window.toggleNavCollapse = function () {
        const shell = document.getElementById("app-shell");
        const collapsed = !(shell && shell.classList.contains("is-nav-collapsed"));

        try {
            localStorage.setItem(STORAGE_KEY, collapsed.toString());
        } catch {
            // Ignore storage failures (e.g. private browsing); the toggle still applies for this view.
        }

        apply(collapsed);
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }

    document.addEventListener("enhancedload", init);
})();
