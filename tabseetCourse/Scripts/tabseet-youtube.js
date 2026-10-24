/*
 * tabseet-youtube.js — أكاديمية تبسيط
 * آلية موحدة لروابط YouTube في المتصفح:
 *   TabseetYouTube.parse(url)              → { isYouTube, valid, videoId, start, watchUrl, embedUrl, error }
 *   TabseetYouTube.mount(container, opts)  → مشغّل متجاوب 16:9 (يبدأ التشغيل بعد ضغط الطالب على زر التشغيل)
 *   TabseetYouTube.destroy(container)      → إيقاف التشغيل وإزالة المشغّل عند إغلاق النافذة
 * القواعد مطابقة لـ Classes/YouTubeLinkHelper.cs على الخادم.
 */
(function (global) {
    "use strict";

    var ID_RE = /^[A-Za-z0-9_-]{11}$/;
    var SCHEME_RE = /^[a-zA-Z][a-zA-Z0-9+.\-]*:\/\//;
    var IFRAME_SRC_RE = /src\s*=\s*["']([^"']+)["']/i;
    var MAX_START = 86400;

    var MSG = {
        empty: "الرابط فارغ",
        invalid: "صيغة الرابط غير صالحة",
        fakeHost: "نطاق الرابط ليس نطاق YouTube الرسمي (youtube.com أو youtu.be)",
        protocol: "بروتوكول الرابط غير مدعوم",
        playlist: "الرابط يشير إلى قائمة تشغيل كاملة وليس إلى فيديو محدد",
        channel: "الرابط يشير إلى قناة YouTube وليس إلى فيديو محدد",
        noId: "لم يتم العثور على معرّف فيديو YouTube صالح في الرابط",
        play: "اضغط للتشغيل",
        playGeneric: "اضغط لعرض الفيديو",
        watchOnYouTube: "مشاهدة على YouTube",
        openNewWindow: "فتح الفيديو في نافذة جديدة",
        invalidSource: "رابط الفيديو غير صالح أو غير مدعوم.",
        err2: "معرّف الفيديو غير صالح.",
        err5: "تعذر تشغيل الفيديو في هذا المتصفح.",
        err100: "هذا الفيديو غير متاح: قد يكون محذوفاً أو خاصاً.",
        err101: "صاحب الفيديو لا يسمح بتشغيله خارج موقع YouTube.",
        errDefault: "تعذر تشغيل الفيديو داخل المنصة."
    };

    function isOfficialHost(host) {
        host = (host || "").toLowerCase().replace(/\.$/, "");
        return host === "youtube.com" || /\.youtube\.com$/.test(host) ||
            host === "youtube-nocookie.com" || /\.youtube-nocookie\.com$/.test(host) ||
            host === "youtu.be" || host === "www.youtu.be";
    }

    function looksLikeYouTube(host) {
        host = (host || "").toLowerCase();
        return host.indexOf("youtube") !== -1 || host.indexOf("youtu.be") !== -1;
    }

    function clampStart(n) {
        return (isFinite(n) && n > 0) ? Math.min(Math.floor(n), MAX_START) : 0;
    }

    function parseStart(value) {
        if (!value) return 0;
        value = String(value).trim().toLowerCase();
        if (/^\d+$/.test(value)) return clampStart(parseInt(value, 10));
        var m = /^(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s?)?$/.exec(value);
        if (!m || (m[1] === undefined && m[2] === undefined && m[3] === undefined)) return 0;
        return clampStart((parseInt(m[1] || "0", 10) * 3600) + (parseInt(m[2] || "0", 10) * 60) + parseInt(m[3] || "0", 10));
    }

    function buildWatchUrl(videoId, start) {
        return "https://www.youtube.com/watch?v=" + videoId + (start > 0 ? "&t=" + start + "s" : "");
    }

    function buildEmbedUrl(videoId, start) {
        return "https://www.youtube.com/embed/" + videoId + "?rel=0&modestbranding=1&playsinline=1" + (start > 0 ? "&start=" + start : "");
    }

    function decodeSegment(s) {
        try { return decodeURIComponent(s); } catch (e) { return s; }
    }

    function parse(input) {
        var info = { isYouTube: false, valid: false, videoId: "", start: 0, watchUrl: "", embedUrl: "", error: "" };
        var url = (input || "").trim();
        if (!url) { info.error = MSG.empty; return info; }

        if (url.toLowerCase().indexOf("<iframe") !== -1) {
            var im = IFRAME_SRC_RE.exec(url);
            if (im && im[1]) url = im[1].trim();
        }

        if (url.indexOf("//") === 0) url = "https:" + url;

        if (!SCHEME_RE.test(url)) {
            var probe = url.split(/[\/?#]/)[0];
            if (!looksLikeYouTube(probe)) return info;
            url = "https://" + url;
        }

        var u;
        try {
            u = new URL(url);
        } catch (e) {
            if (looksLikeYouTube(url)) { info.isYouTube = true; info.error = MSG.invalid; }
            return info;
        }

        var host = (u.hostname || "").toLowerCase().replace(/\.$/, "");
        if (!isOfficialHost(host)) {
            if (looksLikeYouTube(host)) { info.isYouTube = true; info.error = MSG.fakeHost; }
            return info;
        }

        info.isYouTube = true;
        if (u.protocol !== "https:" && u.protocol !== "http:") { info.error = MSG.protocol; return info; }

        var segs = u.pathname.split("/").filter(Boolean).map(decodeSegment);
        var first = (segs[0] || "").toLowerCase();
        var candidate = null;

        if (host === "youtu.be" || host === "www.youtu.be") {
            candidate = segs[0] || null;
        } else if (first === "watch") {
            candidate = u.searchParams.get("v") || segs[1] || null;
        } else if (first === "shorts" || first === "live" || first === "embed" || first === "v" || first === "e") {
            candidate = segs[1] || null;
        } else if (first === "attribution_link") {
            var inner = u.searchParams.get("u");
            if (inner) {
                var innerInfo = parse("https://www.youtube.com" + (inner.charAt(0) === "/" ? "" : "/") + inner);
                if (innerInfo.valid) return innerInfo;
            }
        } else if (first === "playlist") {
            info.error = MSG.playlist; return info;
        } else if (first.charAt(0) === "@" || first === "channel" || first === "c" || first === "user") {
            info.error = MSG.channel; return info;
        } else {
            candidate = u.searchParams.get("v");
        }

        if (candidate && candidate.toLowerCase() === "videoseries") { info.error = MSG.playlist; return info; }
        if (!candidate || !ID_RE.test(candidate)) { info.error = MSG.noId; return info; }

        var start = parseStart(u.searchParams.get("t")) || parseStart(u.searchParams.get("start"));
        if (!start && u.hash) {
            var hm = /[#&?]t=([^&]+)/.exec(u.hash);
            if (hm) start = parseStart(hm[1]);
        }

        info.videoId = candidate;
        info.start = start;
        info.valid = true;
        info.watchUrl = buildWatchUrl(candidate, start);
        info.embedUrl = buildEmbedUrl(candidate, start);
        return info;
    }

    function isHttpsUrl(url) {
        try { return new URL(url).protocol === "https:"; } catch (e) { return false; }
    }

    /* ------------------------------------------------------------------ */
    /* المشغّل                                                             */
    /* ------------------------------------------------------------------ */

    var STYLE_ID = "tabseet-yt-style";
    var CSS =
        ".tb-player{position:relative;width:100%;padding-top:56.25%;background:#000;border-radius:10px;overflow:hidden;direction:ltr}" +
        ".tb-player>iframe,.tb-player>.tb-poster,.tb-player>.tb-error{position:absolute;top:0;left:0;width:100%;height:100%;border:0;margin:0}" +
        ".tb-poster{display:flex;align-items:center;justify-content:center;background-color:#000;background-position:center;background-size:cover;background-repeat:no-repeat;cursor:pointer;padding:0}" +
        ".tb-poster::before{content:'';position:absolute;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,.28)}" +
        ".tb-poster-btn{position:relative;z-index:1;width:74px;height:52px;border-radius:14px;background:#f00;display:flex;align-items:center;justify-content:center;box-shadow:0 4px 18px rgba(0,0,0,.45);transition:transform .15s ease}" +
        ".tb-poster:hover .tb-poster-btn,.tb-poster:focus .tb-poster-btn{transform:scale(1.08)}" +
        ".tb-poster-btn::after{content:'';border-style:solid;border-width:11px 0 11px 20px;border-color:transparent transparent transparent #fff;margin-left:5px}" +
        ".tb-poster-label{position:absolute;bottom:12px;left:0;right:0;text-align:center;color:#fff;font-size:14px;font-weight:700;z-index:1;text-shadow:0 1px 3px rgba(0,0,0,.7);direction:rtl}" +
        ".tb-error{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:12px;background:#111827;color:#fff;text-align:center;padding:20px;direction:rtl;font-size:15px;line-height:1.7}" +
        ".tb-error p{margin:0;color:#fff}" +
        ".tb-error a{display:inline-flex;align-items:center;gap:6px;background:#f00;color:#fff!important;text-decoration:none!important;font-weight:700;padding:9px 18px;border-radius:8px}" +
        ".tb-player-foot{margin-top:8px;text-align:right;direction:rtl;font-size:13px}" +
        ".tb-player-foot a{color:#113464;text-decoration:none;font-weight:700}" +
        ".tb-player-foot a:hover{text-decoration:underline}";

    function ensureStyles() {
        if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
        var s = document.createElement("style");
        s.id = STYLE_ID;
        s.textContent = CSS;
        (document.head || document.documentElement).appendChild(s);
    }

    var apiCallbacks = [];
    var apiRequested = false;

    function flushApi(ok) {
        var cbs = apiCallbacks;
        apiCallbacks = [];
        for (var i = 0; i < cbs.length; i++) {
            try { cbs[i](ok); } catch (e) { }
        }
    }

    function whenApiReady(cb) {
        if (global.YT && global.YT.Player) { cb(true); return; }
        apiCallbacks.push(cb);
        if (apiRequested) return;
        apiRequested = true;

        var prev = global.onYouTubeIframeAPIReady;
        global.onYouTubeIframeAPIReady = function () {
            if (typeof prev === "function") { try { prev(); } catch (e) { } }
            flushApi(true);
        };

        var s = document.createElement("script");
        s.src = "https://www.youtube.com/iframe_api";
        s.async = true;
        s.onerror = function () { flushApi(false); };
        (document.head || document.documentElement).appendChild(s);
        setTimeout(function () { flushApi(false); }, 8000);
    }

    function errorMessage(code) {
        switch (Number(code)) {
            case 2: return MSG.err2;
            case 5: return MSG.err5;
            case 100: return MSG.err100;
            case 101:
            case 150: return MSG.err101;
            default: return MSG.errDefault;
        }
    }

    function clear(el) {
        while (el && el.firstChild) el.removeChild(el.firstChild);
    }

    /* أحداث المشغّل (تُستخدم لاحتساب الحضور): ready / state / opened / noapi / error / destroyed */
    var YT_STATES = { "-1": "unstarted", "0": "ended", "1": "playing", "2": "paused", "3": "buffering", "5": "cued" };

    function emit(state, evt) {
        if (!state || !state.listeners) return;
        var list = state.listeners.slice();
        for (var i = 0; i < list.length; i++) {
            try { list[i](evt); } catch (e) { }
        }
    }

    function safeDuration(player) {
        try {
            var d = player && typeof player.getDuration === "function" ? player.getDuration() : 0;
            return (isFinite(d) && d > 0) ? d : 0;
        } catch (e) { return 0; }
    }

    function on(container, listener) {
        var state = container && container.__tbState;
        if (!state || typeof listener !== "function") return false;
        state.listeners.push(listener);
        return true;
    }

    function off(container, listener) {
        var state = container && container.__tbState;
        if (!state || !state.listeners) return;
        var idx = state.listeners.indexOf(listener);
        if (idx !== -1) state.listeners.splice(idx, 1);
    }

    function makeLink(href, text) {
        var a = document.createElement("a");
        a.href = href;
        a.target = "_blank";
        a.rel = "noopener noreferrer";
        a.textContent = text;
        return a;
    }

    function addFoot(container, href, text) {
        if (!href || !isHttpsUrl(href)) return;
        var foot = document.createElement("div");
        foot.className = "tb-player-foot";
        foot.appendChild(makeLink(href, text));
        container.appendChild(foot);
    }

    function showError(state, message, href) {
        if (!state || state.destroyed) return;
        if (state.player && typeof state.player.destroy === "function") {
            try { state.player.destroy(); } catch (e) { }
        }
        state.player = null;
        state.iframe = null;
        clear(state.box);
        var box = document.createElement("div");
        box.className = "tb-error";
        box.setAttribute("role", "alert");
        var p = document.createElement("p");
        p.textContent = message;
        box.appendChild(p);
        if (href && isHttpsUrl(href)) {
            box.appendChild(makeLink(href, state.info && state.info.isYouTube ? MSG.watchOnYouTube : MSG.openNewWindow));
        }
        state.box.appendChild(box);
    }

    function makePoster(label, imageUrl, onClick) {
        var poster = document.createElement("button");
        poster.type = "button";
        poster.className = "tb-poster";
        poster.setAttribute("aria-label", label);
        if (imageUrl) poster.style.backgroundImage = "url('" + imageUrl + "')";
        var btn = document.createElement("span");
        btn.className = "tb-poster-btn";
        var lbl = document.createElement("span");
        lbl.className = "tb-poster-label";
        lbl.textContent = label;
        poster.appendChild(btn);
        poster.appendChild(lbl);
        poster.addEventListener("click", onClick);
        return poster;
    }

    function createIframe(src, title) {
        var iframe = document.createElement("iframe");
        iframe.src = src;
        iframe.title = title || "Video player";
        iframe.setAttribute("frameborder", "0");
        iframe.setAttribute("allow", "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share");
        iframe.setAttribute("allowfullscreen", "");
        iframe.setAttribute("referrerpolicy", "strict-origin-when-cross-origin");
        return iframe;
    }

    function startYouTube(state) {
        if (!state || state.destroyed) return;
        var info = state.info;
        clear(state.box);

        var origin = "";
        try {
            if (global.location && (global.location.protocol === "http:" || global.location.protocol === "https:")) {
                origin = global.location.origin;
            }
        } catch (e) { }

        var src = info.embedUrl + "&autoplay=1&enablejsapi=1" + (origin ? "&origin=" + encodeURIComponent(origin) : "");
        var iframe = createIframe(src, state.title || "YouTube video player");
        state.box.appendChild(iframe);
        state.iframe = iframe;

        whenApiReady(function (ok) {
            if (state.destroyed || state.iframe !== iframe) return;
            if (!ok || !global.YT || !global.YT.Player) {
                emit(state, { type: "noapi" });
                return;
            }
            try {
                state.player = new global.YT.Player(iframe, {
                    events: {
                        onReady: function (e) {
                            try { if (e && e.target && typeof e.target.playVideo === "function") e.target.playVideo(); } catch (x) { }
                            emit(state, { type: "ready", duration: safeDuration(e && e.target) });
                        },
                        onStateChange: function (e) {
                            var code = e && e.data !== undefined ? String(e.data) : "";
                            var player = e && e.target;
                            var current = 0;
                            try { current = player && typeof player.getCurrentTime === "function" ? player.getCurrentTime() : 0; } catch (x) { }
                            emit(state, { type: "state", state: YT_STATES[code] || "unknown", code: e && e.data, duration: safeDuration(player), currentTime: current });
                        },
                        onError: function (e) {
                            emit(state, { type: "error", code: e && e.data });
                            showError(state, errorMessage(e && e.data), info.watchUrl);
                        }
                    }
                });
            } catch (e) { }
        });
    }

    function startGeneric(state) {
        if (!state || state.destroyed) return;
        clear(state.box);
        var iframe = createIframe(state.url, state.title || "Video player");
        state.box.appendChild(iframe);
        state.iframe = iframe;
        emit(state, { type: "opened", generic: true });
    }

    /**
     * يركّب المشغّل داخل الحاوية.
     * opts: { url, videoId, start, title, autoplay }
     *  - url: الرابط المخزن (YouTube أو أي مصدر HTTPS آخر)
     *  - autoplay: true عندما يكون الضغط الذي فتح المشغّل هو زر التشغيل نفسه
     */
    function mount(container, opts) {
        if (!container) return null;
        ensureStyles();
        destroy(container);
        opts = opts || {};

        var info;
        if (opts.videoId && ID_RE.test(opts.videoId)) {
            var st = clampStart(opts.start || 0);
            info = { isYouTube: true, valid: true, videoId: opts.videoId, start: st, watchUrl: buildWatchUrl(opts.videoId, st), embedUrl: buildEmbedUrl(opts.videoId, st), error: "" };
        } else {
            info = parse(opts.url);
        }

        var box = document.createElement("div");
        box.className = "tb-player";
        container.appendChild(box);

        var state = { box: box, info: info, url: (opts.url || "").trim(), title: opts.title || "", player: null, iframe: null, destroyed: false, listeners: [] };
        if (typeof opts.onEvent === "function") state.listeners.push(opts.onEvent);
        container.__tbState = state;

        if (info.valid) {
            if (opts.autoplay) {
                startYouTube(state);
            } else {
                box.appendChild(makePoster(MSG.play, "https://i.ytimg.com/vi/" + info.videoId + "/hqdefault.jpg", function () { startYouTube(state); }));
            }
            addFoot(container, info.watchUrl, MSG.watchOnYouTube);
        } else if (!info.isYouTube && state.url && isHttpsUrl(state.url)) {
            if (opts.autoplay) {
                startGeneric(state);
            } else {
                box.appendChild(makePoster(MSG.playGeneric, "", function () { startGeneric(state); }));
            }
            addFoot(container, state.url, MSG.openNewWindow);
        } else {
            // رابط YouTube غير صالح → رسالة الخطأ المحددة؛ رابط فارغ أو غير HTTPS → رسالة عامة مفهومة للطالب
            showError(state, (info.isYouTube && info.error) ? info.error : MSG.invalidSource, isHttpsUrl(state.url) ? state.url : "");
        }
        return state;
    }

    function destroy(container) {
        if (!container) return;
        var state = container.__tbState;
        if (state) {
            state.destroyed = true;
            emit(state, { type: "destroyed" });
            state.listeners = [];
            if (state.player && typeof state.player.destroy === "function") {
                try { state.player.destroy(); } catch (e) { }
            }
            if (state.iframe) {
                try { state.iframe.src = "about:blank"; } catch (e) { }
                if (state.iframe.parentNode) state.iframe.parentNode.removeChild(state.iframe);
            }
            container.__tbState = null;
        }
        clear(container);
    }

    var api = {
        parse: parse,
        parseStart: parseStart,
        buildWatchUrl: buildWatchUrl,
        buildEmbedUrl: buildEmbedUrl,
        isOfficialHost: isOfficialHost,
        isYouTubeUrl: function (url) { return parse(url).isYouTube; },
        mount: mount,
        destroy: destroy,
        on: on,
        off: off,
        messages: MSG
    };

    global.TabseetYouTube = api;
    if (typeof module !== "undefined" && module.exports) {
        module.exports = api;
    }
})(typeof window !== "undefined" ? window : (typeof globalThis !== "undefined" ? globalThis : this));
