// Fullscreen the player container so the Unity canvas and its exit control stay together.
(function () {
  "use strict";
  var player = document.getElementById("he-player");
  var canvas = document.getElementById("unity-canvas");
  var button = document.getElementById("he-fullscreen");
  var icon = document.getElementById("he-fullscreen-icon");
  var status = document.getElementById("he-fullscreen-status"), messageTimer;
  var request = player.requestFullscreen || player.webkitRequestFullscreen;
  var exit = document.exitFullscreen || document.webkitExitFullscreen;
  var supported = !!(request && exit && (player.requestFullscreen ? document.fullscreenEnabled !== false : document.webkitFullscreenEnabled !== false));
  if (!supported) return;

  function fullscreenElement() { return document.fullscreenElement || document.webkitFullscreenElement; }
  function sync() {
    var active = !!fullscreenElement(), label = active ? "Exit fullscreen" : "Enter fullscreen";
    button.setAttribute("aria-label", label); button.setAttribute("aria-pressed", String(active)); button.title = label;
    icon.setAttribute("d", active ? "M3 8h5V3m8 0v5h5M8 21v-5H3m18 0h-5v5" : "M8 3H3v5m13-5h5v5M3 16v5h5m13-5v5h-5");
    button.disabled = false;
  }
  function failed() {
    sync();
    status.textContent = "Fullscreen is unavailable in this view."; status.hidden = false;
    clearTimeout(messageTimer); messageTimer = setTimeout(function () { status.hidden = true; }, 4000);
  }
  function changed() { status.hidden = true; sync(); }
  button.addEventListener("click", function () {
    button.disabled = true; status.hidden = true;
    try {
      // Call directly in the click handler: browsers require the learner's gesture.
      var operation = fullscreenElement() ? exit.call(document) : request.call(player);
      Promise.resolve(operation).then(function () { sync(); canvas.focus(); }, failed);
    } catch (error) { failed(); }
  });
  document.addEventListener("fullscreenchange", changed);
  document.addEventListener("webkitfullscreenchange", changed);
  document.addEventListener("fullscreenerror", failed);
  document.addEventListener("webkitfullscreenerror", failed);
  sync(); button.hidden = false;
})();
