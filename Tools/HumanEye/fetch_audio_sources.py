"""Download the owner-listed CC0 audio (15 October 2026) into SRC/<key>/ (default C:/he-sandbox/audio_src, outside the
project), one folder per source, unpack zips there, and record each OpenGameArt page's title, author, licence and notice.
Writes manifest.json (url, file, bytes, sha256, date) and pages.json for Tools/HumanEye/build_audio_assets.py.
Nothing downloaded is executed.

  python -I Tools/HumanEye/fetch_audio_sources.py [SRC]"""
import hashlib, json, pathlib, urllib.request, zipfile, datetime, re, html, sys
ROOT = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else "C:/he-sandbox/audio_src")
FILES = {
    "M01": ["https://opengameart.org/sites/default/files/exploration_theme.zip"],
    "M02": ["https://opengameart.org/sites/default/files/The%20Cogs.mp3"],
    "M03": ["https://opengameart.org/sites/default/files/first_light_particles_0.wav"],
    "M04": ["https://opengameart.org/sites/default/files/claimed_by_the_void_loop.flac", "https://opengameart.org/sites/default/files/claimed_by_the_void.flac"],
    "M05": ["https://opengameart.org/sites/default/files/outer_space_2.mp3"],
    "M06": ["https://opengameart.org/sites/default/files/ogg.zip"],
    "M07": ["https://opengameart.org/sites/default/files/LoopWIP1_0.ogg"],
    "M08": ["https://opengameart.org/sites/default/files/space_city_bpm115_0.ogg"],
    "O1": [f"https://opengameart.org/sites/default/files/{n}.ogg" for n in ("magical_1_0", "magical_2", "magical_3", "magical_4", "magical_5", "magical_6_0", "magical_7_0")],
    "O2": ["https://opengameart.org/sites/default/files/teleport.wav"],
    "O3": ["https://opengameart.org/sites/default/files/sound_effects.zip"],
    "K1": ["https://kenney.nl/media/pages/assets/sci-fi-sounds/6b296f9ecf-1677589334/kenney_sci-fi-sounds.zip"],
    "K2": ["https://kenney.nl/media/pages/assets/interface-sounds/fa43c1dd4d-1677589452/kenney_interface-sounds.zip"],
    "K3": ["https://kenney.nl/media/pages/assets/ui-audio/490d233f68-1677590494/kenney_ui-audio.zip"],
    "K4": ["https://kenney.nl/media/pages/assets/impact-sounds/87b4ddecda-1677589768/kenney_impact-sounds.zip"],
}
manifest = []
for key, urls in FILES.items():
    d = ROOT / key; d.mkdir(parents=True, exist_ok=True)
    for url in urls:
        name = urllib.request.unquote(url.rsplit("/", 1)[1]); target = d / name
        if not target.exists():
            req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0 (HumanEye audio sourcing)"})
            with urllib.request.urlopen(req, timeout=180) as r: target.write_bytes(r.read())
        data = target.read_bytes()
        manifest.append(dict(key=key, url=url, file=str(target), bytes=len(data), sha256=hashlib.sha256(data).hexdigest(), date=datetime.date.today().isoformat()))
        print(key, name, len(data))
        if name.endswith(".zip"):
            with zipfile.ZipFile(target) as z:
                base = (d / "unzipped").resolve()
                for m in z.infolist():   # unpack audio and licence text only, never outside the folder
                    low = m.filename.lower(); out = (base / m.filename).resolve()
                    if m.is_dir() or not out.is_relative_to(base) or not low.endswith((".ogg", ".wav", ".mp3", ".flac", ".txt", ".md", ".url")): continue
                    out.parent.mkdir(parents=True, exist_ok=True); out.write_bytes(z.read(m))
(ROOT / "manifest.json").write_text(json.dumps(manifest, indent=1), encoding="utf-8")
print("files", len(manifest))

# ---- page metadata (OpenGameArt pages; the Kenney packs carry License.txt in their zips)
PAGES = {k: f"https://opengameart.org/content/{v}" for k, v in {
    "M01": "exploration-theme", "M02": "the-cogs", "M03": "first-light-particles-%E2%80%93-cc0-atmospheric-pianoambient-track",
    "M04": "claimed-by-the-void", "M05": "outer-space-loop", "M06": "dark-sci-fi-audio-pack", "M07": "simple-bgm-loop", "M08": "space-city",
    "O1": "magic-spell-sfx", "O2": "teleport-spell", "O3": "18-random-video-game-sound-effects"}.items()}
out = {}
for key, url in PAGES.items():
    info = {"url": url}
    req = urllib.request.Request(info["url"], headers={"User-Agent": "Mozilla/5.0 (HumanEye audio sourcing)"})
    page = urllib.request.urlopen(req, timeout=60).read().decode("utf-8", "replace")
    author = re.search(r'field-name-author-submitter.*?<a href="/users/[^"]+"[^>]*>([^<]+)</a>', page, re.S)
    body = re.search(r'field-name-body.*?<div class="field-item even"[^>]*>(.*?)</div>', page, re.S)
    lic = re.search(r'field-name-field-art-licenses.*?(<div class="field-items">.*?</div>\s*</div>)', page, re.S)
    attr = re.search(r'field-name-field-copyright-notice.*?<div class="field-item even"[^>]*>(.*?)</div>', page, re.S)
    text = lambda s: re.sub(r"\s+", " ", html.unescape(re.sub(r"<[^>]+>", " ", s))).strip() if s else ""
    title = re.search(r"<title>([^<]+)</title>", page)
    out[key] = dict(url=info["url"], title=html.unescape(title.group(1)).split(" | ")[0].strip() if title else "", author=text(author.group(1)) if author else "",
                    licence=text(lic.group(1)) if lic else "", attribution=text(attr.group(1)) if attr else "", description=text(body.group(1))[:700] if body else "")
    print(f"{key} | {out[key]['title']} | by {out[key]['author']} | {out[key]['licence']} | attribution: {out[key]['attribution'][:80]}\n    {out[key]['description'][:400]}\n")
(ROOT / "pages.json").write_text(json.dumps(out, indent=1, ensure_ascii=False), encoding="utf-8")
