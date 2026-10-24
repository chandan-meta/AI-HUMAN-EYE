const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const template = path.resolve(__dirname, "../../Assets/WebGLTemplates/HumanEyeLoader");
const source = fs.readFileSync(path.join(template, "TemplateData/fullscreen.js"), "utf8");
function setup(mode = "standard") {
  const events = {}, nodes = {};
  for (const id of ["he-player", "unity-canvas", "he-fullscreen", "he-fullscreen-icon", "he-fullscreen-status"]) {
    nodes[id] = { hidden: true, disabled: false, attributes: {}, events: {},
      setAttribute(name, value) { this.attributes[name] = value; },
      addEventListener(name, handler) { this.events[name] = handler; } };
  }
  const player = nodes["he-player"], button = nodes["he-fullscreen"], status = nodes["he-fullscreen-status"];
  let entered = 0, exited = 0, focused = 0;
  const document = { fullscreenEnabled: mode !== "unsupported", fullscreenElement: null,
    getElementById(id) { return nodes[id]; }, addEventListener(name, handler) { events[name] = handler; } };
  nodes["unity-canvas"].focus = () => focused++;
  player.requestFullscreen = function () {
    assert.equal(this, player, "The player container must keep the canvas and exit button together.");
    entered++; document.fullscreenElement = player; events.fullscreenchange(); return Promise.resolve();
  };
  document.exitFullscreen = function () {
    assert.equal(this, document); exited++; document.fullscreenElement = null; events.fullscreenchange(); return Promise.resolve();
  };
  if (mode === "rejected") player.requestFullscreen = () => Promise.reject(new Error("Permission denied"));
  if (mode === "throwing") player.requestFullscreen = () => { throw new Error("Unsupported view"); };
  if (mode === "webkit") {
    document.webkitFullscreenEnabled = true;
    player.webkitRequestFullscreen = function () { entered++; document.webkitFullscreenElement = player; events.webkitfullscreenchange(); };
    document.webkitExitFullscreen = function () { exited++; document.webkitFullscreenElement = null; events.webkitfullscreenchange(); };
    delete player.requestFullscreen; delete document.exitFullscreen;
  }
  vm.runInNewContext(source, { document, Promise, setTimeout() { return 1; }, clearTimeout() {} });
  return { player, button, status, document, events, entered: () => entered, exited: () => exited, focused: () => focused };
}
async function click(test) { test.button.events.click(); await Promise.resolve(); await Promise.resolve(); }

(async () => {
  const normal = setup();
  assert.equal(normal.button.hidden, false);
  assert.equal(normal.button.attributes["aria-pressed"], "false");
  assert.equal(normal.entered(), 0, "Fullscreen must wait for a click.");
  await click(normal);
  assert.equal(normal.entered(), 1); assert.equal(normal.focused(), 1);
  assert.equal(normal.button.attributes["aria-label"], "Exit fullscreen");
  assert.equal(normal.button.attributes["aria-pressed"], "true");
  await click(normal);
  assert.equal(normal.exited(), 1); assert.equal(normal.button.attributes["aria-label"], "Enter fullscreen");
  await click(normal);
  normal.document.fullscreenElement = null; normal.events.fullscreenchange(); // Browser Escape, outside the button.
  assert.equal(normal.button.attributes["aria-pressed"], "false");
  assert.equal(normal.button.title, "Enter fullscreen");

  for (const mode of ["rejected", "throwing"]) {
    const denied = setup(mode); await click(denied);
    assert.equal(denied.status.hidden, false, mode + " must explain the unavailable action.");
    assert.equal(denied.button.disabled, false, mode + " must not leave the button stuck.");
    assert.equal(denied.button.attributes["aria-pressed"], "false");
  }
  const unsupported = setup("unsupported");
  assert.equal(unsupported.button.hidden, true);
  assert.equal(unsupported.button.events.click, undefined);
  const webkit = setup("webkit"); await click(webkit);
  assert.equal(webkit.entered(), 1); assert.equal(webkit.button.attributes["aria-pressed"], "true");
  await click(webkit);
  assert.equal(webkit.exited(), 1); assert.equal(webkit.button.attributes["aria-pressed"], "false");

  const html = fs.readFileSync(path.join(template, "index.html"), "utf8");
  assert.match(html, /id="he-fullscreen"[^>]*type="button"/);
  assert.match(html, /src="TemplateData\/fullscreen\.js"/);
  const css = fs.readFileSync(path.join(template, "TemplateData/loader.css"), "utf8");
  assert.match(css, /width:\s*48px;\s*height:\s*48px/);
  assert.match(css, /safe-area-inset-bottom/);
  assert.match(css, /#he-fullscreen:focus-visible/);
  console.log("Fullscreen checks passed: gesture, enter/exit, Escape, denial, unavailable, WebKit fallback and control accessibility.");
})().catch(error => { console.error(error); process.exitCode = 1; });
