/*
 * tabseet-attendance.js — أكاديمية تبسيط
 * احتساب حضور المحاضرات من المتصفح بوقت التشغيل الفعلي:
 *   TabseetAttendance.configure({ startUrl, heartbeatUrl, joinUrl, heartbeatSeconds })
 *   TabseetAttendance.track(playerContainer, { lectureId, onStatus })  → يتابع أحداث مشغّل TabseetYouTube ويرسل النبضات
 *   TabseetAttendance.stop()                                           → إيقاف المتابعة وإرسال ما تبقى (عند إغلاق النافذة)
 *   TabseetAttendance.joinLive(lectureId)                              → تسجيل دخول محاضرة مباشرة (يعمل تلقائياً للروابط a.js-join-live[data-lecture-id])
 *                                                                        وعند النجاح يُطلق الحدث document "tabseet:livejoined" {lectureId, status} ويستدعي cfg.onLiveJoined
 * الخادم هو الحكم: يقيّد الثواني المحتسبة بالزمن الفعلي ويقرر الاكتمال (Services/StudentAttendanceService.cs).
 */
(function (global) {
    "use strict";

    var cfg = {
        startUrl: "",
        heartbeatUrl: "",
        joinUrl: "",
        heartbeatSeconds: 30,
        tokenSelector: 'input[name="__RequestVerificationToken"]',
        post: null,   // لأغراض الاختبار: دالة بديلة للإرسال
        now: null,    // لأغراض الاختبار: مصدر بديل للوقت
        onLiveJoined: null // اختياري: (lectureId, status) بعد نجاح تسجيل دخول محاضرة مباشرة
    };

    var tracker = null;

    function configure(options) {
        options = options || {};
        for (var k in options) {
            if (Object.prototype.hasOwnProperty.call(options, k)) cfg[k] = options[k];
        }
        var hb = parseInt(cfg.heartbeatSeconds, 10);
        cfg.heartbeatSeconds = (isFinite(hb) && hb >= 5) ? hb : 30;
    }

    function now() {
        return cfg.now ? cfg.now() : Date.now();
    }

    function getToken() {
        if (typeof document === "undefined") return "";
        var el = document.querySelector(cfg.tokenSelector);
        return el ? el.value : "";
    }

    function toForm(data) {
        var fd = new FormData();
        fd.append("__RequestVerificationToken", getToken());
        for (var k in data) {
            if (Object.prototype.hasOwnProperty.call(data, k) && data[k] !== undefined && data[k] !== null) {
                fd.append(k, String(data[k]));
            }
        }
        return fd;
    }

    function post(url, data, opts) {
        opts = opts || {};
        if (typeof cfg.post === "function") return cfg.post(url, data, opts);
        if (!url) return Promise.resolve(null);

        if (opts.beacon && global.navigator && typeof global.navigator.sendBeacon === "function") {
            try { global.navigator.sendBeacon(url, toForm(data)); } catch (e) { }
            return Promise.resolve(null);
        }

        if (typeof global.fetch === "function") {
            return global.fetch(url, { method: "POST", body: toForm(data), credentials: "same-origin", keepalive: !!opts.keepalive })
                .then(function (r) { return r.ok ? r.json() : null; })
                .catch(function () { return null; });
        }

        return new Promise(function (resolve) {
            try {
                var x = new XMLHttpRequest();
                x.open("POST", url, true);
                x.onload = function () {
                    try { resolve(x.status >= 200 && x.status < 300 ? JSON.parse(x.responseText) : null); } catch (e) { resolve(null); }
                };
                x.onerror = function () { resolve(null); };
                x.send(toForm(data));
            } catch (e) { resolve(null); }
        });
    }

    function createTracker(container, opts) {
        return {
            container: container,
            lectureId: opts.lectureId,
            onStatus: opts.onStatus,
            playing: false,
            started: false,
            starting: false,
            finished: false,
            unsent: 0,
            duration: null,
            status: null,
            lastTickAt: null,
            timer: null,
            listener: null
        };
    }

    function applyStatus(t, status) {
        if (!status) return;
        t.status = status;
        if (status.isFinish) t.finished = true;
        if (typeof t.onStatus === "function") {
            try { t.onStatus(status, t); } catch (e) { }
        }
    }

    function ensureStarted(t) {
        if (t.started || t.starting || tracker !== t) return;
        t.starting = true;
        post(cfg.startUrl, { lectureId: t.lectureId, durationSeconds: t.duration }).then(function (res) {
            t.starting = false;
            if (res && res.success) {
                t.started = true;
                applyStatus(t, res.status);
                if (t.unsent >= cfg.heartbeatSeconds) flush(t, false);
            }
        });
    }

    function flush(t, beacon) {
        if (!t || !t.started || t.unsent <= 0) return;
        var seconds = t.unsent;
        t.unsent = 0;
        var p = post(cfg.heartbeatUrl, { lectureId: t.lectureId, playedSeconds: seconds, durationSeconds: t.duration }, { beacon: beacon, keepalive: beacon });
        if (p && typeof p.then === "function") {
            p.then(function (res) {
                if (res && res.success) {
                    applyStatus(t, res.status);
                } else if (res === null && !beacon) {
                    // فشل شبكة: نعيد الثواني للمحاولة في النبضة التالية (بحد أقصى لتجنب التضخم)
                    t.unsent = Math.min(t.unsent + seconds, cfg.heartbeatSeconds * 4);
                }
            });
        }
    }

    function setPlaying(t, playing) {
        if (t.playing === playing) return;
        t.playing = playing;
        t.lastTickAt = now();
        if (!playing) flush(t, false);
    }

    function handleEvent(t, evt) {
        if (!t || !evt || tracker !== t) return;
        if (evt.duration > 0) t.duration = Math.round(evt.duration);

        switch (evt.type) {
            case "ready":
                ensureStarted(t);
                break;
            case "state":
                if (evt.state === "playing") {
                    setPlaying(t, true);
                    ensureStarted(t);
                } else if (evt.state === "paused" || evt.state === "ended" || evt.state === "cued" || evt.state === "unstarted") {
                    setPlaying(t, false);
                }
                break;
            case "opened":   // مصدر غير YouTube: لا توجد أحداث تشغيل، يُحتسب وقت بقاء المشغّل مفتوحاً
            case "noapi":    // تعذر تحميل YouTube IFrame API: نفس المعاملة
                setPlaying(t, true);
                ensureStarted(t);
                break;
            case "error":
            case "destroyed":
                setPlaying(t, false);
                break;
        }
    }

    function tick(t) {
        if (tracker !== t) return;
        var n = now();
        if (t.playing && !t.finished) {
            var delta = t.lastTickAt ? Math.round((n - t.lastTickAt) / 1000) : 1;
            if (delta < 0) delta = 0;
            if (delta > 60) delta = 60; // مؤقّتات التبويب الخلفي قد تتباطأ؛ لا نضيف أكثر من دقيقة في المرة
            t.unsent += delta;
            if (t.unsent >= cfg.heartbeatSeconds) flush(t, false);
        }
        t.lastTickAt = n;
    }

    function track(container, opts) {
        stop();
        if (!container || !opts || !opts.lectureId || !global.TabseetYouTube || typeof global.TabseetYouTube.on !== "function") return null;

        var t = createTracker(container, opts);
        tracker = t;
        t.listener = function (evt) { handleEvent(t, evt); };
        global.TabseetYouTube.on(container, t.listener);
        t.lastTickAt = now();
        t.timer = setInterval(function () { tick(t); }, 1000);
        return t;
    }

    function stop() {
        var t = tracker;
        if (!t) return;
        if (t.playing) {
            tick(t);
            t.playing = false;
        }
        flush(t, false);
        tracker = null;
        if (t.timer) clearInterval(t.timer);
        t.timer = null;
        if (t.container && global.TabseetYouTube && typeof global.TabseetYouTube.off === "function") {
            global.TabseetYouTube.off(t.container, t.listener);
        }
    }

    function joinLive(lectureId) {
        if (!cfg.joinUrl || !lectureId) return Promise.resolve(null);
        return post(cfg.joinUrl, { lectureId: lectureId }, { keepalive: true }).then(function (res) {
            // إبلاغ الصفحة بحالة الحضور بعد الدخول (مثلاً لإظهار اختبار المحاضرة المباشرة فور تسجيل الدخول)
            if (res && res.success && res.status) emitLiveJoined(lectureId, res.status);
            return res;
        });
    }

    function emitLiveJoined(lectureId, status) {
        if (typeof cfg.onLiveJoined === "function") {
            try { cfg.onLiveJoined(lectureId, status); } catch (e) { }
        }
        if (typeof document !== "undefined" && typeof global.CustomEvent === "function") {
            try {
                document.dispatchEvent(new global.CustomEvent("tabseet:livejoined", { detail: { lectureId: lectureId, status: status } }));
            } catch (e) { }
        }
    }

    function formatDuration(totalSeconds) {
        var s = Math.max(0, Math.floor(Number(totalSeconds) || 0));
        var h = Math.floor(s / 3600);
        var m = Math.floor((s % 3600) / 60);
        var sec = s % 60;
        var mm = (m < 10 ? "0" : "") + m;
        var ss = (sec < 10 ? "0" : "") + sec;
        return h > 0 ? h + ":" + mm + ":" + ss : mm + ":" + ss;
    }

    if (typeof document !== "undefined") {
        document.addEventListener("click", function (e) {
            var target = e.target && e.target.closest ? e.target.closest("a.js-join-live[data-lecture-id]") : null;
            if (target) joinLive(target.getAttribute("data-lecture-id"));
        });
    }

    if (typeof global.addEventListener === "function") {
        global.addEventListener("pagehide", function () {
            if (tracker) {
                if (tracker.playing) tick(tracker);
                flush(tracker, true);
            }
        });
    }

    var api = {
        configure: configure,
        track: track,
        stop: stop,
        joinLive: joinLive,
        flush: function () { if (tracker) flush(tracker, false); },
        current: function () { return tracker; },
        formatDuration: formatDuration,
        config: cfg,
        _test: { handleEvent: handleEvent, tick: tick, flush: flush }
    };

    global.TabseetAttendance = api;
    if (typeof module !== "undefined" && module.exports) {
        module.exports = api;
    }
})(typeof window !== "undefined" ? window : (typeof globalThis !== "undefined" ? globalThis : this));
