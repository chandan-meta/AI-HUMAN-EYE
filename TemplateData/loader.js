/* Human Eye loading page.
   - Lays the supplied artwork out as a cover background that never crops the eye, its optic nerve, the title or the bar.
   - Drives the live bar from Unity's own loading progress (the download, then the engine's start-up), eased.
   - Animates a light layer of depth particles (far specks, mid glints, near soft orbs) with a hint of pointer parallax.
   - Fades into the module once it has drawn its first frame (Assets/HumanEye/Plugins/WebGL/HumanEyePage.jslib calls
     window.heUnityReady), or a few seconds after the engine started if that call never comes. */
(function () {
  "use strict";
  var ART_W = 1672, ART_H = 941;
  var SAFE = { x0: 585, y0: 135, x1: 1195, y1: 705 };   // eye, optic nerve, title and bar (artwork pixels): always in view
  var loader = document.getElementById("he-loader"), stage = document.getElementById("he-stage");
  var bar = document.getElementById("he-bar"), fill = bar.querySelector(".he-fill");
  var unityCanvas = document.getElementById("unity-canvas"), message = document.getElementById("he-message");
  var reduced = !!(window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches);
  var touch = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent) || (navigator.maxTouchPoints > 1 && /Macintosh/.test(navigator.userAgent));
  function clamp(v, lo, hi) { return Math.max(lo, Math.min(hi, v)); }
  function rand(a, b) { return a + Math.random() * (b - a); }

  // ---------- layout: cover, but the safe box always fits and stays in view
  var view = { s: 1, w: 0, h: 0 };
  function layout() {
    var vw = window.innerWidth, vh = window.innerHeight;
    var s = Math.max(vw / ART_W, vh / ART_H);
    s = Math.min(s, vw / (SAFE.x1 - SAFE.x0 + 48), vh / (SAFE.y1 - SAFE.y0 + 48));
    var w = ART_W * s, h = ART_H * s;
    var x = clamp((vw - w) / 2, -(SAFE.x0 - 24) * s, vw - (SAFE.x1 + 24) * s);
    var y = clamp((vh - h) / 2, -(SAFE.y0 - 24) * s, vh - (SAFE.y1 + 24) * s);
    stage.style.left = x + "px"; stage.style.top = y + "px"; stage.style.width = w + "px"; stage.style.height = h + "px";
    view.s = s; view.w = vw; view.h = vh;
    sizeParticles();
  }

  // ---------- messages (errors only; warnings go to the console)
  function showError(text) {
    message.textContent = String(text || "The module could not start. Please reload the page.");
    message.classList.add("he-show");
  }

  // ---------- progress: eased toward Unity's reported value; full only when the engine has started
  var target = 0, shown = 0, started = false, firstFrame = false, finishing = false;
  function drawBar() {
    var inner = bar.clientWidth - 2, height = bar.clientHeight - 2;
    fill.style.width = (shown > 0.001 ? Math.max(height, shown * inner) : 0) + "px";
    bar.setAttribute("aria-valuenow", String(Math.round(shown * 100)));
  }
  function maybeFinish(force) {
    if (finishing || !started || !(firstFrame || force)) return;
    finishing = true; target = 1;
    var t0 = performance.now();
    (function waitFull() {
      if (shown < 0.995 && performance.now() - t0 < 1200) { requestAnimationFrame(waitFull); return; }
      shown = 1; drawBar();
      bar.classList.add("he-done");                                   // a small final glow
      setTimeout(function () {
        loader.classList.add("he-out");                                 // fade into the module (0.65 s)
        setTimeout(function () { loader.style.display = "none"; running = false; }, 700);
        unityCanvas.focus();
      }, reduced ? 150 : 250);
    })();
  }
  window.heUnityReady = function () { firstFrame = true; maybeFinish(false); };

  // ---------- particles: three depth layers drawn additively
  var pc = document.getElementById("he-particles"), ctx = pc.getContext("2d"), dpr = 1;
  var LAYERS = [
    { n: 34, r: [0.6, 1.5], v: [3, 8], a: [0.25, 0.7], halo: 3.0, depth: 0.15, tint: [140, 220, 255] },   // far specks
    { n: 14, r: [1.4, 3.0], v: [7, 14], a: [0.18, 0.42], halo: 5.0, depth: 0.45, tint: [90, 200, 255] },   // mid glints
    { n: 5, r: [26, 64], v: [2, 5], a: [0.035, 0.08], halo: 1.0, depth: 1.0, tint: [40, 150, 255] },      // near soft orbs
  ];
  var dots = [], sprites = [], pointer = { x: 0, y: 0, ex: 0, ey: 0 };
  function makeSprite(tint) {
    var c = document.createElement("canvas"); c.width = c.height = 64;
    var g = c.getContext("2d"), grad = g.createRadialGradient(32, 32, 0, 32, 32, 32);
    grad.addColorStop(0, "rgba(255,255,255,1)");
    grad.addColorStop(0.18, "rgba(" + tint.join(",") + ",0.9)");
    grad.addColorStop(0.5, "rgba(" + tint.join(",") + ",0.25)");
    grad.addColorStop(1, "rgba(" + tint.join(",") + ",0)");
    g.fillStyle = grad; g.fillRect(0, 0, 64, 64); return c;
  }
  function seed() {
    dots = []; sprites = LAYERS.map(function (l) { return makeSprite(l.tint); });
    LAYERS.forEach(function (l, li) {
      for (var i = 0; i < l.n; i++)
        dots.push({ layer: li, x: Math.random(), y: Math.random(), r: rand(l.r[0], l.r[1]), v: rand(l.v[0], l.v[1]),
                    a: rand(l.a[0], l.a[1]), phase: rand(0, 6.283), sway: rand(0.2, 0.7), tw: rand(0.6, 1.6) });
    });
  }
  function sizeParticles() {
    dpr = Math.min(window.devicePixelRatio || 1, 2);
    pc.width = Math.round(view.w * dpr); pc.height = Math.round(view.h * dpr);
  }
  function drawParticles(t, dt) {
    var w = view.w, h = view.h, k = Math.max(0.55, view.s);          // sizes follow the artwork's scale
    var calm = reduced ? 0.15 : 1;
    pointer.ex += (pointer.x - pointer.ex) * Math.min(1, dt * 2); pointer.ey += (pointer.y - pointer.ey) * Math.min(1, dt * 2);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0); ctx.clearRect(0, 0, w, h);
    ctx.globalCompositeOperation = "lighter";
    for (var i = 0; i < dots.length; i++) {
      var d = dots[i], l = LAYERS[d.layer];
      d.y -= d.v * calm * dt / h;                                      // a slow rise, each layer at its own pace
      if (d.y < -0.08) { d.y = 1.08; d.x = Math.random(); }
      var sway = Math.sin(t * 0.0004 * d.sway + d.phase) * 0.012 * calm;
      var px = (d.x + sway) * w + (reduced ? 0 : pointer.ex * 12 * l.depth);
      var py = d.y * h + (reduced ? 0 : pointer.ey * 8 * l.depth);
      var twinkle = 0.65 + 0.35 * Math.sin(t * 0.001 * d.tw + d.phase);
      var size = d.r * l.halo * 2 * k;
      ctx.globalAlpha = d.a * twinkle;
      ctx.drawImage(sprites[d.layer], px - size / 2, py - size / 2, size, size);
    }
    ctx.globalAlpha = 1; ctx.globalCompositeOperation = "source-over";
  }
  window.addEventListener("pointermove", function (e) {
    pointer.x = (e.clientX / Math.max(1, view.w)) * 2 - 1; pointer.y = (e.clientY / Math.max(1, view.h)) * 2 - 1;
  }, { passive: true });

  // ---------- one animation loop for the bar and the particles (stops when the page has faded out)
  var running = true, last = performance.now();
  function frame(now) {
    if (!running) return;
    var dt = Math.min(0.1, (now - last) / 1000); last = now;
    shown += (target - shown) * (1 - Math.exp(-dt * (finishing ? 9 : 4)));
    if (Math.abs(target - shown) < 0.0005) shown = target;
    drawBar();
    drawParticles(now, dt);
    requestAnimationFrame(frame);
  }

  // ---------- start
  window.addEventListener("resize", layout);
  layout(); seed(); requestAnimationFrame(frame);

  var gl = document.createElement("canvas").getContext("webgl2");
  if (!gl) { showError("This browser does not support WebGL 2, which the Human Eye module needs. Please use a current Chrome, Edge, Firefox or Safari."); return; }

  config.devicePixelRatio = Math.min(window.devicePixelRatio || 1, touch ? 1.5 : 2);
  config.autoSyncPersistentDataPath = true;                             // progress saves persist without the deprecated manual sync
  config.showBanner = function (msg, type) { if (type === "error") showError(msg); else console.warn(msg); };

  // A data file over GitHub's 100 MB limit is published in parts with a manifest (<data>.parts.json). Download them with
  // byte progress (kept in Cache Storage for the next visit) and hand Unity the joined file. Without a manifest, Unity
  // downloads the data itself.
  var dataShare = 0;                                                    // share of the bar the parts download takes
  function loadData(url) {
    return fetch(url + ".parts.json", { cache: "no-cache" })
      .then(function (r) { return r.ok ? r.json() : null; }, function () { return null; })
      .then(function (manifest) {
        if (!manifest || !manifest.parts) return url;
        dataShare = 0.85;
        var base = url.substring(0, url.lastIndexOf("/") + 1), got = new Array(manifest.parts.length).fill(0);
        var store = window.caches ? caches.open("human-eye-build").catch(function () { return null; }) : Promise.resolve(null);
        return store.then(function (cache) {
          if (cache) cache.keys().then(function (keys) {                // drop parts of earlier deployments
            keys.forEach(function (k) { if (!manifest.parts.some(function (p) { return k.url.endsWith("/" + p); })) cache.delete(k); });
          });
          return Promise.all(manifest.parts.map(function (name, i) {
            var partUrl = base + name;
            return (cache ? cache.match(partUrl) : Promise.resolve(null)).then(function (hit) {
              if (hit) return hit.arrayBuffer().then(function (b) { got[i] = b.byteLength; report(); return b; });
              return fetch(partUrl).then(function (res) {
                if (!res.ok) throw new Error("Download failed: " + name + " (" + res.status + ")");
                var reader = res.body.getReader(), chunks = [];
                return (function pump() {
                  return reader.read().then(function (step) {
                    if (step.done) {
                      var blob = new Blob(chunks);
                      if (cache) cache.put(partUrl, new Response(blob)).catch(function () {});
                      return blob.arrayBuffer();
                    }
                    chunks.push(step.value); got[i] += step.value.byteLength; report();
                    return pump();
                  });
                })();
              });
            });
          }));
        }).then(function (buffers) { return URL.createObjectURL(new Blob(buffers)); });
        function report() {
          var sum = got.reduce(function (a, b) { return a + b; }, 0);
          target = Math.max(target, dataShare * Math.min(1, sum / manifest.size));
        }
      });
  }

  var script = document.createElement("script");
  script.src = loaderUrl;
  script.onload = function () {
    loadData(config.dataUrl).then(function (dataUrl) {
      config.dataUrl = dataUrl;
      return createUnityInstance(unityCanvas, config, function (progress) {   // full only once the engine has started
        target = Math.max(target, Math.min(dataShare + (1 - dataShare) * progress, 0.995));
      });
    }).then(function (instance) {
      window.heUnityInstance = instance;
      started = true; target = 0.995;
      maybeFinish(false);
      setTimeout(function () { maybeFinish(true); }, 5000);           // no first-frame call: fade anyway
    }).catch(function (err) { showError(err); });
  };
  script.onerror = function () { showError("The module files could not be downloaded. Please check the connection and reload the page."); };
  document.body.appendChild(script);
})();
