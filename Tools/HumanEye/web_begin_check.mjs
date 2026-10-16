// First-BEGIN check of the Human Eye WebGL build (16 October 2026): opens the page in a real, GPU-backed Chrome with a
// fresh profile (neither Unity's nor Chrome's shader cache is warm), waits for the loader to fade, presses BEGIN and reports
// the longest frames: the loader's fade, the start screen, and after BEGIN (a first-use shader compile blocks the page's
// main thread, so it shows as one long frame). Screenshots land in <out>/. With --trace (the page opened with ?heTrace) it
// writes the variants drawn after BEGIN but not on the start screen as Assets/HumanEye/Resources/ShaderWarmup.txt lines; a
// development build also logs every "Uploaded shader variant" compile (in <out>/console.txt).
//   node Tools/HumanEye/web_begin_check.mjs <url> <out dir> [--trace <list.txt>] [--post <s after BEGIN>] [--size 1920x1080 --begin 231,590]
import { spawn } from "node:child_process";
import { writeFileSync, rmSync, mkdirSync } from "node:fs";
const args = process.argv.slice(2), opt = (k, d) => (args.includes(k) ? args[args.indexOf(k) + 1] : d);
const [url, out] = args, trace = opt("--trace"), post = +opt("--post", trace ? 60 : 10);
const [W, H] = opt("--size", "1920x1080").split("x").map(Number);
const [bx, by] = opt("--begin", "231,590").split(",").map(Number), begin = { x: bx, y: by };   // BEGIN on the start card (1024x768: 231,433)
const exe = opt("--chrome", "C:/Program Files/Google/Chrome/Application/chrome.exe"), port = 9300 + Math.floor(Math.random() * 500);
const profile = `${out}/chrome-profile`; rmSync(profile, { recursive: true, force: true }); mkdirSync(out, { recursive: true });
const chrome = spawn(exe, [`--remote-debugging-port=${port}`, `--user-data-dir=${profile}`, `--window-size=${W + 16},${H + 140}`,
  "--no-first-run", "--no-default-browser-check", "--disable-background-timer-throttling", "--disable-backgrounding-occluded-windows",
  "--disable-renderer-backgrounding", "--autoplay-policy=no-user-gesture-required", "about:blank"], { stdio: "ignore" });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const report = [], logs = [], note = (s) => { report.push(s); console.log(s); };
let ws, id = 0, t0 = Date.now(), pressed = 0;
const pending = new Map();
const send = (method, params = {}) => new Promise((r) => { pending.set(++id, r); ws.send(JSON.stringify({ id, method, params })); });
const evaluate = async (expr) => (await send("Runtime.evaluate", { expression: expr, returnByValue: true })).result?.result?.value;
const shot = async (file) => writeFileSync(file, Buffer.from((await send("Page.captureScreenshot", { format: "jpeg", quality: 70 })).result.data, "base64"));
// ModuleBoot.TraceDump prints HE_VARIANT lines, then HE_TRACE_VARIANTS; each dump lists everything drawn since start-up
async function dump() {
  const from = logs.length;
  await evaluate("window.heUnityInstance.SendMessage('HumanEye_Systems', 'TraceDump')");
  for (let i = 0; i < 40 && !logs.slice(from).some((l) => l.includes("HE_TRACE_VARIANTS")); i++) await sleep(250);
  return new Set(logs.slice(from).filter((l) => l.includes("HE_VARIANT\t")).map((l) => l.split("\n")[0]).map((l) => {   // development builds append a stack trace
    const [shader, sub, pass, keywords = ""] = l.slice(l.indexOf("HE_VARIANT\t") + 11).split("\t");
    return [shader, sub, pass, keywords.split(" ").filter(Boolean).sort().join(" ")].join("\t");
  }));
}
const gaps = (from, to) => evaluate(`(() => { const f = window.__heFrames.filter(t => t >= ${from} && t <= ${to}), g = [];
  for (let i = 1; i < f.length; i++) g.push({ at: Math.round(f[i - 1] - ${from}), gap: Math.round(f[i] - f[i - 1]) });
  return { frames: f.length, worst: g.sort((a, b) => b.gap - a.gap).slice(0, 4) }; })()`);
try {
  let targets;
  for (let i = 0; i < 50 && !targets; i++) targets = await fetch(`http://127.0.0.1:${port}/json`).then((r) => r.json()).catch(() => sleep(200));
  ws = new WebSocket(targets.find((t) => t.type === "page").webSocketDebuggerUrl);
  await new Promise((r) => ws.addEventListener("open", r));
  ws.addEventListener("message", (e) => {
    const m = JSON.parse(e.data);
    if (m.id && pending.has(m.id)) { pending.get(m.id)(m); pending.delete(m.id); return; }
    if (m.method !== "Runtime.consoleAPICalled") return;
    const text = m.params.args.map((a) => a.value ?? a.description ?? "").join(" ");
    logs.push(`[${pressed ? `+${Date.now() - pressed} ms after BEGIN` : `${((Date.now() - t0) / 1000).toFixed(1)} s`}] ${m.params.type}: ${text}`);
    if (/^HE_(WARMUP|TRACE)/.test(text) || m.params.type === "error") note(logs.at(-1).slice(0, 240));
  });
  await send("Runtime.enable"); await send("Page.enable");
  await send("Emulation.setDeviceMetricsOverride", { width: W, height: H, deviceScaleFactor: 1, mobile: false });
  await send("Page.addScriptToEvaluateOnNewDocument", { source: "window.__heFrames=[];(function f(){window.__heFrames.push(performance.now());requestAnimationFrame(f);})();" });
  t0 = Date.now(); await send("Page.navigate", { url });
  note(`renderer: ${await evaluate(`(() => { const g = document.createElement('canvas').getContext('webgl2'), x = g && g.getExtension('WEBGL_debug_renderer_info'); return x ? g.getParameter(x.UNMASKED_RENDERER_WEBGL) : 'n/a'; })()`)}`);
  for (let i = 0; i < 1200 && !(await evaluate(`getComputedStyle(document.getElementById('he-loader')).display === 'none'`)); i++) await sleep(250);
  const faded = await evaluate("performance.now()");
  note(`loader gone at ${((Date.now() - t0) / 1000).toFixed(1)} s; its last second: ${JSON.stringify(await gaps(faded - 1000, faded))}`);
  note(`start-up frames over 250 ms (s after navigation: ms): ${await evaluate(`(() => { const f = window.__heFrames, g = [];
    for (let i = 1; i < f.length; i++) if (f[i] - f[i - 1] > 250) g.push((f[i - 1] / 1000).toFixed(1) + ': ' + Math.round(f[i] - f[i - 1]));
    return g.join(', '); })()`)}`);
  await sleep(6000);
  const atBegin = await evaluate("performance.now()");
  note(`start screen (6 s): ${JSON.stringify(await gaps(faded, atBegin))}`);
  await shot(`${out}/0_start.jpg`);
  const onStart = trace ? await dump() : null;
  pressed = Date.now();
  for (const type of ["mousePressed", "mouseReleased"]) await send("Input.dispatchMouseEvent", { type, ...begin, button: "left", clickCount: 1 });
  for (const ms of [300, 600, 1000, 1500, 3000]) { await sleep(Math.max(0, ms - (Date.now() - pressed))); await shot(`${out}/begin_${String(ms).padStart(4, "0")}ms.jpg`); }
  await sleep(Math.max(0, post * 1000 - (Date.now() - pressed)));
  note(`after BEGIN (${post} s): ${JSON.stringify(await gaps(atBegin, atBegin + post * 1000))}`);
  const late = logs.filter((l) => l.includes("after BEGIN") && l.includes("Uploaded shader variant"));
  note(`compiled after BEGIN: ${late.length}${late.map((l) => "\n  " + l.slice(0, 240)).join("")}`);
  if (trace) {
    const rows = [...await dump()].filter((r) => !onStart.has(r)).sort();
    writeFileSync(trace, rows.join("\n") + "\n");
    note(`trace: ${onStart.size} variants on the start screen, ${rows.length} more after BEGIN -> ${trace}`);
  }
} catch (e) { note("SCRIPT ERROR " + e.stack); }
writeFileSync(`${out}/console.txt`, logs.join("\n"));
writeFileSync(`${out}/report.txt`, report.join("\n"));
try { ws?.close(); } catch {}
chrome.kill();
process.exit(0);
