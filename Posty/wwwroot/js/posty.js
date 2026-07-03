// Thin glue for the parts Blazor cannot do on its own: reading/writing the
// textarea's selection range, and copying the finished post to the clipboard.
window.posty = {
    // Current selection plus the live value (authoritative over the bound field).
    getSelection: function (el) {
        if (!el) return { start: 0, end: 0, value: "" };
        return {
            start: el.selectionStart ?? 0,
            end: el.selectionEnd ?? 0,
            value: el.value ?? ""
        };
    },

    // Re-focus the editor and restore a selection after a programmatic edit.
    // preventScroll keeps the page from jumping to the caret on every format.
    setSelection: function (el, start, end) {
        if (!el) return;
        try { el.focus({ preventScroll: true }); } catch (e) { el.focus(); }
        try { el.setSelectionRange(start, end); } catch (e) { /* out of range: ignore */ }
    },

    // Grow the textarea to fit its content so it never shows an inner scrollbar.
    // Collapsing to "auto" to measure briefly shrinks the page, which would make the
    // browser clamp the scroll position — so snapshot and restore the window scroll.
    autoGrow: function (el) {
        if (!el) return;
        var x = window.scrollX, y = window.scrollY;
        el.style.height = "auto";
        el.style.height = el.scrollHeight + "px";
        window.scrollTo(x, y);
    },

    // Wire the editor to auto-grow as the user types (native, no round-trip),
    // size it once for whatever is already there, and route Ctrl/Cmd+B/I/U to .NET.
    initEditor: function (el, dotnetRef) {
        if (!el || el.dataset.grow === "1") return;
        el.dataset.grow = "1";
        el.addEventListener("input", function () { window.posty.autoGrow(el); });

        el.addEventListener("keydown", function (e) {
            if ((e.ctrlKey || e.metaKey) && !e.altKey) {
                var k = e.key.toLowerCase();
                if (k === "b" || k === "i" || k === "u") {
                    e.preventDefault(); // also stops Ctrl+U opening "view source"
                    if (dotnetRef) dotnetRef.invokeMethodAsync("OnShortcut", k);
                }
            }
        });

        window.posty.autoGrow(el);
    },

    // Copy text, with a legacy fallback for browsers without the async clipboard API.
    copy: async function (text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch (e) {
            try {
                const ta = document.createElement("textarea");
                ta.value = text;
                ta.style.position = "fixed";
                ta.style.left = "-9999px";
                ta.setAttribute("readonly", "");
                document.body.appendChild(ta);
                ta.select();
                const ok = document.execCommand("copy");
                document.body.removeChild(ta);
                return ok;
            } catch (e2) {
                return false;
            }
        }
    }
};
