// Theming for the whole app. Presets swap a `data-theme` attribute on <html>, which the
// `[data-theme="..."]` blocks in app.css key off of. Custom themes go one step further and
// set the CSS variables directly as inline styles (which always win over attribute-selector
// rules), so a custom palette doesn't need any CSS changes at all.
window.turfTheme = (function () {
    var STORAGE_KEY = 'turfTheme';
    var presets = ['dark', 'light', 'ocean', 'forest', 'sunset'];
    var THEMED_VARS = ['--bg', '--panel', '--panel-alt', '--border', '--text', '--text-dim', '--brand', '--brand-dark'];

    // Simple additive lighten/darken — good enough for deriving a hover shade from one picked color.
    function shade(hex, pct) {
        hex = hex.replace('#', '');
        var n = parseInt(hex, 16);
        var a = Math.round(255 * pct / 100);
        var r = Math.max(0, Math.min(255, (n >> 16) + a));
        var g = Math.max(0, Math.min(255, ((n >> 8) & 255) + a));
        var b = Math.max(0, Math.min(255, (n & 255) + a));
        return '#' + (0x1000000 + r * 0x10000 + g * 0x100 + b).toString(16).slice(1);
    }

    function clearInlineOverrides() {
        var style = document.documentElement.style;
        THEMED_VARS.forEach(function (v) { style.removeProperty(v); });
    }

    function apply(name) {
        if (presets.indexOf(name) === -1) name = 'dark';
        clearInlineOverrides();
        document.documentElement.setAttribute('data-theme', name);
        localStorage.setItem(STORAGE_KEY, JSON.stringify({ name: name }));
    }

    // colors: { bg, panel, border, text, textDim, brand } — panelAlt and brandDark are derived.
    function applyCustom(colors) {
        var root = document.documentElement;
        root.setAttribute('data-theme', 'custom');
        root.style.setProperty('--bg', colors.bg);
        root.style.setProperty('--panel', colors.panel);
        root.style.setProperty('--panel-alt', shade(colors.panel, 8));
        root.style.setProperty('--border', colors.border);
        root.style.setProperty('--text', colors.text);
        root.style.setProperty('--text-dim', colors.textDim);
        root.style.setProperty('--brand', colors.brand);
        root.style.setProperty('--brand-dark', shade(colors.brand, -12));
        localStorage.setItem(STORAGE_KEY, JSON.stringify({ name: 'custom', customColors: colors }));
    }

    function init() {
        var saved = null;
        try { saved = JSON.parse(localStorage.getItem(STORAGE_KEY) || 'null'); } catch (e) { /* ignore */ }

        if (saved && saved.customColors) {
            applyCustom(saved.customColors);
        } else {
            apply((saved && saved.name) || 'dark');
        }
    }

    function getCurrent() {
        var saved = null;
        try { saved = JSON.parse(localStorage.getItem(STORAGE_KEY) || 'null'); } catch (e) { /* ignore */ }
        return JSON.stringify(saved || { name: 'dark' });
    }

    return { apply: apply, applyCustom: applyCustom, init: init, getCurrent: getCurrent, presets: presets };
})();

window.turfTheme.init();
