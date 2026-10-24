// The module tells its loading page it has drawn its first frames, so the page fades into it (WebGL template
// HumanEyeLoader, TemplateData/loader.js). A page without the hook ignores the call.
mergeInto(LibraryManager.library, {
  HumanEyePageReady: function () {
    if (typeof window.heUnityReady === "function") window.heUnityReady();
  },
});
