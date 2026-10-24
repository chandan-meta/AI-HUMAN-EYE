# Human Eye module: audio implementation report

Audio overhaul of 15 October 2026, following `HUMAN_EYE_AUDIO_MASTER_PROMPT.md` and its 62-cue sheet. The work covers
downloading, processing, importing, assigning and mixing all music, ambience and effects, then testing them in the running
module. Companion files:

- `Docs/HumanEye/Audio/AUDIO_CUE_SHEET_IMPLEMENTED.csv`: 69 rows, the owner's 62 cues plus 7 events found in the project.
- `Assets/HumanEye/Audio/Licenses/AUDIO_SOURCES.csv`: 91 rows, one per source file per processed clip.
- `Assets/HumanEye/Audio/Licenses/AUDIO_CREDITS.md`: the credits page.

**Status.** Implemented, and verified in the sandbox copy (`C:\he-sandbox`) on 15 October 2026:
- the editor patch and the validator (72/0/0);
- EditMode 27/27 and PlayMode 8/8;
- Windows and WebGL player builds;
- six scripted walkthroughs: both openings, two full runs and two real-time runs, with 0 failures.

**Not verified:** a human listening pass, since no one could listen here (selection and levels were set by measurement),
and playback of the WebGL build in a browser. See section 9.

## 1. Audit before the change

| Area | What the project had | What changed |
|---|---|---|
| Manager | One `AudioDirector` with three sources: `music` (one 32 s pad, `observatory_pad.wav`, for the menu, lab, lessons and ending), `introMusic` (the opening's score, `intro_discovery.wav`) and one shared `sfx` source. | Same component, rewritten (section 5). |
| Voice | `Narrator.voice`, with lip sync from the clip's time (`NikoRig`, `Narrator.TryMouth`). | Untouched apart from routing it to the Voice bus. |
| Effects | About 60 keys. Most were synthesised in-project by `Tools/HumanEye/generate_*_sfx.py`; 10 were Kenney CC0 clips imported earlier (`ThirdParty/Kenney/Audio`). | Re-clipped to the new library by key (section 4). |
| Mixer | None. | `HumanEyeMixer.mixer` with six buses. |
| Ambience | None. | Lab hum and eye hum. |
| Loops | None. | Gate servo, two crackle loops, travelling pulse. |
| Music per state | None: the same pad played throughout. | Eight beds chosen per state. |
| Ducking | Music to 0.4 (-8 dB) while Niko spoke. | Section 6. |
| Timeline | No Timeline or audio tracks, animation events or signal receivers. | Cues are fired from the module's own events: the opening's clock and state, content steps, the HUD, the check controller, the signal system. |

## 2. Downloads and licences

Every source below was downloaded on 15 October 2026 from its public page, without logging in, by
`Tools/HumanEye/fetch_audio_sources.py`. The downloads are in `C:\he-sandbox\audio_src`, each folder holding the file and
its page metadata (`manifest.json`, `pages.json`). Every page states CC0 1.0, so no attribution is required; the credits
page credits everyone anyway. All downloads succeeded.

| Key | Title | Author | Page | Used for |
|---|---|---|---|---|
| M01 | Exploration Theme | CleytonKauffman | opengameart.org/content/exploration-theme | start screen, gate, opening |
| M02 | The Cogs | turtletooth | opengameart.org/content/the-cogs | the lab after the opening |
| M03 | First Light Particles | Yoiyami | opengameart.org/content/first-light-particles-–-cc0-atmospheric-pianoambient-track | teaching chapters |
| M04 | Claimed by the Void (loop version) | vitalezzz | opengameart.org/content/claimed-by-the-void | the opening's layer changes |
| M05 | Outer Space Loop | wipics | opengameart.org/content/outer-space-loop | eye exploration |
| M06 | Dark Sci-Fi Audio Pack, "Airy" | SRG774 | opengameart.org/content/dark-sci-fi-audio-pack | retina, optic nerve, brain |
| M07 | Simple BGM Loop | Theforeshadower | opengameart.org/content/simple-bgm-loop | checks |
| M08 | Space City | MintoDog | opengameart.org/content/space-city | ending |
| K1 | Sci-Fi Sounds | Kenney | kenney.nl/assets/sci-fi-sounds | servo, ambience, layer change |
| K2 | Interface Sounds | Kenney | kenney.nl/assets/interface-sounds | interface, checks, eye cues |
| K3 | UI Audio | Kenney | kenney.nl/assets/ui-audio | hover, click, crackle grains |
| K4 | Impact Sounds | Kenney | kenney.nl/assets/impact-sounds | downloaded, not used (impacts are too heavy for this module) |
| O1 | Magic Spell SFX | JaggedStone | opengameart.org/content/magic-spell-sfx | shimmers, reveals, Niko's hello |
| O2 | Teleport Spell | Ogrebane | opengameart.org/content/teleport-spell | gate entry, organs, air layers |
| O3 | 18 random video game sound effects | bart | opengameart.org/content/18-random-video-game-sound-effects | whooshes |

- **Not used: Mixkit.** The brief listed it as optional. Mixkit stock music is not licensed for games, and the CC0 packs
  covered every cue.
- **Licence texts** that came with the downloads are copied to `Assets/HumanEye/Audio/Licenses/`: the three Kenney
  `License.txt` files and the Exploration Theme readme.
- **Earlier sounds still played.** Five events keep sounds that predate the overhaul. Each has a row at the end of
  `AUDIO_SOURCES.csv`:
  - `discovery` and `unlock`: Kenney Interface Sounds `glass_001` and `bong_001` (CC0, also listed in
    `Docs/HumanEye/THIRD_PARTY_NOTICES.md`).
  - `chapter`, `lightmove` and `titlehit`: synthesised in this project (`generate_ui_sfx.py`, `generate_reference_sfx.py`).

## 3. Selection and processing

**How sounds were auditioned.** No one could listen in this environment. Each candidate was therefore measured:

- integrated loudness and loudness range (EBU R128), and true peak;
- spectral centroid and low-end share;
- level variation over time and estimated tempo;
- the level jump at the loop seam;
- waveform and spectrogram sheets (`C:\he-sandbox\audio_src\_measure_*`).

Choices were then made against the brief's direction: bright, curious, calm, never dark or punitive.

| Bed | Source | Decision |
|---|---|---|
| Start, gate | M01 Exploration Theme | Calm (67 bpm, centroid 298 Hz) and seamless by its author. Raised to -18 LUFS, held at -18.4 by its peak. |
| Lab | M02 The Cogs | Very bass-heavy (87 % of energy low) and hot (-10.5 LUFS). A -5 dB peaking cut at 160 Hz; MP3 padding trimmed. |
| Teaching | M03 First Light Particles | Soft piano and pad, the quietest source. Loop made with a 4 s crossfade at the seam (the file fades out). |
| Layer changes | M04 Claimed by the Void (loop version) | Bright arpeggios with no sub-bass (15 % low), the most energetic bed, so it plays only during the breakup (15–25.5 s). Top rolled off at 9 kHz. |
| Eye | M05 Outer Space Loop | Floating and slow. Bass trimmed, MP3 padding removed. |
| Nerve and brain | M06 "Airy" | **The pack's "Pulse" was rejected.** 99 % of its energy is sub-bass, its loudness range is 15.6 LU and it swells like a tension cue, which is wrong for children. The same pack's "Airy" (6 % low, range 2.6 LU) carries the sequence instead, as the brief allows ("Pulse or Airy"). Loop with a 3 s crossfade. |
| Checks | M07 Simple BGM Loop | Playful and level (range 2.4 LU), -2.5 dB under the other beds. The lossy file opens with 45 ms of near-silence, which dropped out at every loop. It is trimmed with a 10 ms seam crossfade, leaving 47.98 beats at 65 bpm. |
| Ending | M08 Space City | Optimistic, 115 bpm, steady. -2 dB. |

All music is normalised to -18 LUFS integrated and encoded as OGG q7 (bit-exact, so a rebuild writes the same bytes). Loops
are cut on their own repeat or crossfaded tail-into-head.

**Built loops and Web.** The room tones, travelling pulse, servo and crackles each get a 2048-sample lead-in made from their
own tail, plus a WAV `smpl` loop point after it. The reason is that Unity Web's AAC encoder alters a clip's first 1024
samples, which otherwise ticks at every loop (Unity's "Audio in Web" manual). A sandbox probe confirmed that Unity on
Windows also restarts a looping clip at its `smpl` start (sample 8192 in the probe, never 0), so the lead-in plays once.

Effects are filtered (high-pass, low-pass, band limits), trimmed and peak-normalised. Layered effects follow the brief,
for example:

- servo, whoosh and chime for the gate;
- the layer change as an ignition, a crackle loop whose level follows the dissolve, and a completion shimmer;
- the travelling pulse as a Magic Spell tone with a 5 Hz shimmer and teleport air: "living information", not a laser.

The crackle is scattered Kenney glitch and switch grains with a fixed random seed, so it rebuilds identically. Every clip
comes from the listed downloads; nothing is synthesised from nothing. The tool is `Tools/HumanEye/build_audio_assets.py`
and is reproducible.

## 4. Unity assets and cue assignments

```
Assets/HumanEye/Audio/
  Music/bgm_start_exploration_theme.ogg, bgm_lab_the_cogs.ogg, bgm_niko_first_light_particles.ogg,
        bgm_transition_claimed_by_the_void.ogg, bgm_eye_outer_space_loop.ogg, bgm_pulse_airy.ogg,
        bgm_quiz_simple_bgm_loop.ogg, bgm_finale_space_city.ogg
  Ambience/amb_lab_hum.wav, amb_eye_hum.wav
  SFX/Gate/       gate_start_press, gate_iris_scan, gate_unlock, gate_servo_loop, gate_open_whoosh, gate_entry,
                  lab_reveal_sting, hologram_on, camera_sweep
  SFX/Anatomy/    dissolve_start, dissolve_crackle_loop, dissolve_crackle_loop_high, layer_reveal, organs_reveal
  SFX/Eye/        eye_appear, eye_zoom, part_scan, cornea_ping, iris_shimmer, pupil_dilate, pupil_constrict,
                  lens_focus, retina_shimmer, label_appear
  SFX/OpticNerveBrain/ neural_signal_start, optic_pulse_travel_loop, pulse_near_brain, pulse_brain_arrival,
                  brain_reveal, cortex_highlight
  SFX/UI/         ui_hover, ui_click, ui_open, ui_close, ui_skip, return_menu, ui_unavailable
  SFX/Quiz/       quiz_open, quiz_correct, quiz_wrong, quiz_hint, quiz_complete
  SFX/Companion/  niko_hello, niko_listen, niko_think, niko_reply, niko_error, ending_farewell
  Licenses/       AUDIO_SOURCES.csv, AUDIO_CREDITS.md, licence texts
  HumanEyeMixer.mixer
```

**Import settings.** Music: Vorbis 0.55, Compressed In Memory, Load In Background. Effects and ambience: Vorbis 0.7,
Decompress On Load (low latency). Every new file and folder has a `.meta` whose GUID is derived from its path, so the
owner's Editor and the sandbox import the same asset IDs.

**Key table.** The game plays sounds by stable keys, and `Builder.CueClips` binds each key to a clip, a trim in dB and a
channel. That gives 63 keys over 93 effect entries. Existing keys were re-clipped in place, so every call site already in
the game plays the new sound. The cue sheet names each row's real trigger, asset, bus, level, playback and code. Of the
owner's 62 cues:

- **59 are implemented.**
- **3 are not applicable, because their events do not exist in the module:**
  - `SKELETON_REVEAL`: the skeleton beat is omitted in production.
  - `ORGAN_HIGHLIGHT`: organs are never highlighted.
  - `VASCULAR_SCAN`: vessels arrive with the organs.

**Cues fired by real events, not guessed times.**

- **Times on the opening's clock.** The opening is a deterministic 32.5 s cinematic driven by its own clock, which pauses
  with the game. Its accents, and the named cues at 0.3, 15, 20, 25.5 and 29.5 s, sit on that clock at the beats its
  animation is authored to: gate leaves 1–3.5 s, title 9 s, layer changes at 17 and 22 s, extraction 26.5 s.
- **Measured events in the opening.**
  - The doorway crossing is detected by measured camera distance.
  - Muscle and organ reveals fire on the reveal controller's state changes.
  - The servo and crackle loop levels follow the gate's travel and the breakup's progress.
- **Events elsewhere in the module:**
  - part selection, isolation and focus;
  - signal emission, the lead pulse's position and arrival;
  - pupil movement (the pupil's target versus its radius);
  - lens focus, HUD panels and Ask Niko;
  - every check event (open, right, wrong, hint, complete), chapter entry, the closing line and skip.

## 5. Audio system

`Assets/HumanEye/Scripts/Core/AudioDirector.cs` is the one audio manager.

- **Music.** Eight beds by key, one at a time, with equal-power crossfades between two bed sources (`music`, `musicB`).
  The opening's score plays on `introMusic` over the bed (`BeginIntroMusic(key)` / `EndIntroMusic`).
- **Who picks the bed.** `Stage.Audio.AudioFrame` picks it every frame from the module's state, so a jump, restart, skip,
  menu or check can never leave the wrong music playing:
  - the start theme on the menu;
  - the opening's own score while it runs;
  - the check bed while a check card is up;
  - otherwise the chapter's bed, set on entry, by `Do("music", key)` content steps, by Scene 4's signal stream, and by the
    closing (finale).
- **One-shots.** Played by key on three channels:
  - Effects: stopped on chapter entry and on Skip.
  - Interface: never paused or stopped, so modal clicks, the pause menu and a click that starts a chapter still sound.
  - Cinematic: stopped by Skip and by the learner leaving (Home, Menu, Restart, a chapter jump), but not by an automatic
    chapter advance, so the Explore Inside chime carries into Scene 3.
- **Retrigger and hover limits.** A key is never retriggered within 120 ms, tracked per key. Hover has a 200 ms cooldown
  and never sounds on touch.
- **Stacking rules.**
  - A finished check's fanfare (or the Explore Inside chime) stands alone: no check-open or chapter chime within 1.5 s of it.
  - Label ticks and a signal stream's start are soft cues (`AudioDirector.Soft`). They play at the end of the frame, and
    only if no other cue started in that frame.
  - A panel closed by a click that makes its own sound closes silently. This applies to a chapter pick, Confirm, and Menu
    with Ask Niko open.
  - A text answer that takes the learner to a part is answered by that part's selection cue.
  - Part close-ups sound their highlight only, with no zoom whoosh on top.
  - The journey replay sounds once per step.
  - Skip lands on Niko without a camera whoosh.
  - Niko's voiced answer waits for a lesson line still playing, and its reply cue leads the voice.
- **Loops.** The gate servo, two crackles and the travelling pulse belong to the event that starts them. That event
  levels, places and stops them; pause pauses them; menu, restart, chapter change and Skip end them all.
- **Spatial sound.** The gate servo is 3D at the gate, and the pulse loop is 3D at the lead pulse (linear roll-off, 3 to
  40 m). Music, voice, interface and ambience stay 2D.
- **Ambience.** One near-subliminal room tone at a time: the lab hum in the lab and corridor, the eye hum inside the cut
  eye and the retina, and silence in the eye-to-brain cinematic.

**Mixer.** `Assets/HumanEye/Audio/HumanEyeMixer.mixer`: Master > Voice, Music, Ambience, SFX, UI (-2 dB), Cinematic.
Every scene source is routed to its bus by the editor patch (`Builder.ApplyAudioOverhaul`), and runtime loops go to SFX.

**Snapshots.** For the Audio Mixer, Unity's Web platform documents group volume only (no mixer effects). Snapshot
transitions are not documented as supported there. The brief's snapshots are therefore implemented as eased source
levels that behave the same on Windows and Web, and `AudioDirector.MixState` reports which one is active:

| Snapshot | Implementation |
|---|---|
| Intro | start theme; the opening's score |
| Exploration | chapter beds (eye, Niko, pulse) |
| Transition | the layer-change score over the bed |
| NikoSpeaking | music -10 dB, ambience -5 dB, effects -2.5 dB, cinematic -3 dB |
| ChatbotListening | music -4 dB while Ask Niko is open |
| Quiz | the check bed |
| Paused | every source paused except the interface channel |

The existing settings carry through unchanged: narration, music and effects sliders, and Sound on/off. Browsers allow
audio only after the learner's first click or tap (Unity's "Audio in Web" manual), so on the Web the start theme begins
with the first interaction on the start screen.

## 6. Niko's voice: ducking and lip sync

- **Ducking.** While Niko speaks, `musicDuck` = 0.32, which is -9.9 dB, inside the brief's 8–12 dB band. Ambience drops
  5 dB and effects 2.5 dB. The levels ease down at 1.5/s and back up when the voice ends. "Speaking" is the narration or
  the supplemental voice (closing line, voiced answers).
- **Voice clips.** Not changed: no clip, timing or caption was touched.
- **Lip sync.**
  - `NikoRig` reads the voice clip's time and the mouth keys (ElevenLabs alignment, or `Builder.AudioKeys` from the WAV).
    Routing the voice source to a mixer bus changes neither `AudioSource.time` nor `isPlaying`, so the mouth follows the
    voice as before.
  - The walkthroughs log Niko's mouth activity per cue (section 8).
- **ElevenLabs voice.** The childlike voice approved for Niko has still not been generated, because no
  `ELEVENLABS_API_KEY` exists on this machine (Niko task). This audio work keeps the current voice and its lip sync.

## 7. Files changed by the audio work

**New files.**

- Scripts and tests:
  - `Scripts/Stage/Stage.Audio.cs`
  - `Scripts/Core/AutoPilot.Audio.cs`
  - `Editor/Builder.Audio.cs`
  - `Tests/PlayMode/AudioOverhaulTests.cs`
- Tools:
  - `Tools/HumanEye/fetch_audio_sources.py`
  - `Tools/HumanEye/build_audio_assets.py`
  - `Tools/HumanEye/audio_cue_sheet.py`
- Assets: `Assets/HumanEye/Audio/{Music,Ambience,SFX/*,Licenses}` and `HumanEyeMixer.mixer`.
- Docs: `Docs/HumanEye/Audio/*`. Also updated: `Docs/HumanEye/THIRD_PARTY_NOTICES.md` (section 9),
  `Docs/HumanEye/ASSET_REGISTER.csv` (14 rows), `CLAUDE.md` (the audio rule) and `HANDOFF.md`.

**Rewritten.** `Scripts/Core/AudioDirector.cs`.

**Hooks added or re-keyed.** The trigger changes:

| File | Change |
|---|---|
| `Stage/Stage.Opening.cs` | Accent table re-keyed; cues on the opening's clock; doorway crossing; reveal-state cues; servo and crackle loops; score hand-over; Skip stops effects. |
| `Stage/Stage.Scene02.cs` | Music for the full, reduced and skipped openings; the Niko cut; reset stops loops. |
| `Stage/Stage.cs` | `music` content action; bed per chapter; per-part isolate keys; chapter chime rule; soft label ticks; ability buttons click once. |
| `Stage/Stage.Signals.cs` | Scene 4's stream bed; soft stream start; arrival latch; brain reveal. |
| `Stage/Stage.EyeBrain.cs` | Skip stops the streams and effects and sounds Skip; one cue per pathway label (the cortex for the last); no stream-start one-shot in reduced motion. |
| `Stage/Stage.Optics.cs` | Lens focus. |
| `Stage/Stage.Scene03.cs` | Lens flex. |
| `Stage/Stage.Ending.cs` | Finale bed; closing line; one sound per replay step. |
| `Core/ChapterRunner.cs` | Cinematic tails stop when the learner leaves (menu, restart, chapter jump). |
| `UI/Hud.cs` | Start, panels, menu, info label (soft), Ask Niko listen, think, reply and error; silent close when the click makes its own sound. |
| `UI/HudState.cs` | Hover and click events. |
| `Learning/ActivityController.cs` | Check open (not straight after a finished check), right, wrong, hint and complete. |
| `Niko/NikoGuide.cs` | Voiced reply, after any lesson line still playing. |
| `Editor/HumanEyeContent.cs` | `Do("music", "eye")`, `Do("sfx", …)` steps in Scenes 2, 4 and 5. |
| `Core/AutoPilot*.cs` | Audio log, summary and audio checks. |

**Scene and data.** `HumanEye_Observatory.unity` (sources, routing, tracks, effects) and the chapter and dialogue assets
regenerated by `BuildData`. These are applied by **Tools > Human Eye > Authoring > Apply audio overhaul (keeps scene
edits)**, the last patch after `ApplyCorridorGate`.

## 8. QA

### Final test round

The final round ran in the sandbox on 15 October 2026 (round 7b, synced 21:26). The walkthroughs drove a Windows player
through `AutoPilot`, with the real user interface clicked by pointer events. Evidence is in
`Docs/HumanEye/Polish/Captures/audio-2026-10-15/`:

- `opening_cues.png`: every opening cue on the frame captured at that moment;
- `*_audio.log`: each walk's audio timeline, mix states, checks and summaries;
- the test results files;
- `loop-point-probe.txt`;
- `patch-and-validator.txt`.

| Check | Result |
|---|---|
| Editor patch | `HUMAN_EYE_AUDIO_APPLIED tracks=8 effects=93 keys=63` with buses Voice, Music, Ambience, SFX, UI and Cinematic |
| Validator | 72 passed, 0 warnings, 0 errors |
| EditMode tests | 27/27 |
| PlayMode tests | 8/8. `AudioOverhaulTests` checks the crossfade, a second change inside a crossfade (no cut), the same key again, the -10 dB duck and its release, pause holding music and loops but not the interface channel, mute, loops, ambience and the per-key retrigger guard. `IntroAudioTests` also passed. |
| Windows player build | Succeeded |
| WebGL build | Succeeded (393 MB: 292 MB data, 118 MB wasm); not played in a browser |
| Opening, 1600×900, real time | 25 cues (21 distinct), 0 repeats, at most 1 loop at once; contact sheet |
| Opening, reduced motion | 9 cues, 0 repeats; transition bed, then the lab bed at the cut to Niko |
| Full walk 1600×900, 6× clock, stress flows | All 10 chapters and 65 cues; 44 checks passed, 0 failed, 0 exceptions; all seven chapter beds; ambience lab, eye and off |
| Full walk 1024×768 (4:3), reduced motion, 6× clock | All 10 chapters and 65 cues; 17 checks passed, 0 failed; all eight beds |
| Scene 9, real time | 7 checks passed; 40 cues; 0 repeats |
| Scenes 2–3, real time | 8 checks passed; 88 cues (38 distinct); 0 repeats; duck measured at -9.9 dB |
| Cue coverage, all six walks | 62 distinct cue keys, all eight beds and the opening's score, ambience lab, eye and off |
| Niko presence and lip sync | No presence failures in any walk. Mouth open 0.53–0.58 on average while speaking; at most 0.002 while silent. |
| Performance, stress walk | 52.8–60 fps average per chapter; memory peak 602 MB; at most 1 loop playing at once |

**The repeats in the two 6× walks are not double triggers.** The stress walk logged 19 and the 1024 walk 7:

- Some are cues about a second apart at real speed that the 6× clock packs within 150–220 ms: journey-replay steps,
  eye-to-brain labels and the highlight sweep.
- The rest are the autopilot closing four panels within 0.3 s.

Both real-time walks log zero repeats.

### Pass A: downloads, licences, import and mapping

- All 15 listed sources were downloaded from their public pages, with no login. 14 are used; Kenney Impact Sounds is not.
  Every page states CC0, and so does each Kenney `License.txt`.
- The ledger has 91 rows: every processed clip names every source file in it, including all six crackle grains.
- The cue sheet has 69 rows, and a script check found every asset path present.
- Every asset has a `.meta` with a path-derived GUID. The importer settings Unity wrote are copied back to the project.
- An independent review had three reviewers (mixing, events, assets) and an adversarial verifier per finding. It produced
  23 findings: **14 confirmed, all fixed**, and 9 rejected.
  - **Confirmed and fixed:**
    - a hard cut when two beds change inside one crossfade;
    - the Explore Inside chime cut by the next chapter's entry;
    - the eye-to-brain Skip leaving effects playing and restarting the pulse loop;
    - cinematic tails resuming over the menu;
    - a doubled click on the ability buttons;
    - Niko's reply cue landing over a lesson line;
    - stacked cues in the reduced-motion pathway beat;
    - the pupil cue firing under Freeze;
    - the quiz bed's silent gap at its loop;
    - missing crackle sources in the ledger;
    - wrong chapter numbers in the cue sheet;
    - the Web loop seams (section 3);
    - zip extraction allowing paths outside its folder.
  - **Rejected with reasons, including three changes reverted after verification:**
    - Moving the eye-to-brain cues after Niko's line. The cues already mark on-screen changes, and the move stacked two cues
      in reduced motion.
    - A Web load-type override for iOS Silent Mode. A looping music bed keeps the page's audio session audible, and the
      override would only add latency.
    - Removing the sweep at the opening's end. The picture re-composes there, and reduced motion has a real cut.

### Pass B: continuity, ducking, event precision and scientific flow

- **Music.**
  - One bed at a time, with crossfades.
  - The chapter sequence in the full walk is start, then the opening's score over the lab bed, eye, quiz, Niko, pulse
    (Scenes 8–9 and Scene 4's signal stream), quiz again where checks run, and finale through the end menu.
  - The start theme returns on Home.
  - Returning from Ask Niko keeps the chapter's bed. While Ask Niko is open it plays 4 dB lower.
- **Ducking.** -9.9 dB under Niko, measured in the walks; the release is eased.
- **Precision.** Every opening cue lands on its frame (`opening_cues.png`). Real-time runs have zero repeats. The gate
  servo sounds only while the leaves move, and the crackle only while the dissolve runs.
- **Scientific order in Scene 9.** Light reaches the retina (light cue), then the signal starts, then the travel loop
  rides the lead pulse in 3D, then the near-brain lift, the arrival, the pathway labels, the cortex, the brain rise and the
  title. Light and signals never share a sound or a path.

### Pass C: full dry run, alternate flows and resources

The walkthroughs exercised these alternate flows:

- Skip, in the opening and in the eye-to-brain cinematic.
- Pause and resume: the music holds and continues.
- Help, Settings and Review: the music holds behind them.
- The music and effects sliders at zero, and Sound off and on.
- Ask Niko with an unsupported question (error cue with text) and an explore question.
- Eight rapid Replay requests (no overlapping demonstration).
- Freeze mid-change.
- Home during a demonstration: loops stop and the start theme plays.
- Save and reload, a new-session Confirm and Cancel, Continue, and a completed save resuming Scene 11.
- Reduced motion at 4:3.

All their checks passed.

**Not exercised by automation:**

- the voiced Ask Niko answers (`nikothink`, `nikoreply`);
- the free-mode Continue button's sound (the autopilot calls Continue directly);
- volume settings between 0 and full;
- tablet touch hardware.

Resources: eight fixed sources plus at most one runtime loop playing at once, and an average of 52.8–60 fps per chapter.

## 9. Missing, blocked and not verified

**Nothing is missing from the download list.** Every listed source was downloaded and used, except:
- Kenney Impact Sounds: downloaded and audited, not needed.
- Mixkit: optional, not used.

**Not verified here:**

- **A listening pass by a person.** No one could listen in this environment. Every choice and level was set by
  measurement: loudness, peak, spectrum, dynamics, tempo, loop seams and spectrograms. Please listen to:
  - the opening (gate to Niko);
  - one teaching chapter with a check;
  - Scene 9's eye-to-brain cinematic;
  - the ending.

  Then tune any level in `Builder.CueClips` (dB) or `AudioDirector.musicDuck`.
- **WebGL in a browser.** The Web build was made, not played: the in-app browser cannot run it here. Still unheard on Web:
  - the loop-point fix (verified on Windows by the probe);
  - autoplay after the first click;
  - the mix.
- **iOS Silent Mode.** By design, interface ticks heard only under the pause menu (where the music is paused) follow the
  device's Silent switch on iPhone and iPad browsers. Everywhere else a looping music bed keeps the page audible.
- **Music beds on Web.** Their own loop point may soften once per loop (every 44–195 s). This comes from the AAC encoder's
  first samples, and Unity's `smpl` workaround does not apply to compressed-in-memory music.

**Blocked: the ElevenLabs voice.** Niko's new childlike voice has not been generated, because no `ELEVENLABS_API_KEY`
exists on this machine. The current voice, its captions and its lip sync are unchanged. To generate it:
1. Set the key as a Windows user environment variable.
2. Run `python Tools/HumanEye/generate_elevenlabs_vo.py voices`, then `samples`, then `all`.
3. Re-run **Apply supplied Niko character**.

The audio patch needs no change for the new voice.

**Not applicable:**

- **Cue-sheet events:** `SKELETON_REVEAL`, `ORGAN_HIGHLIGHT` and `VASCULAR_SCAN` have no event in this module.
- **Timeline:** the module has no Timeline, Signal or animation-event assets. Cues are fired by the module's own clock and
  events, as section 4 describes.
- **Mixer snapshots:** implemented as eased source levels, because of the Web platform (section 5).

**Found by the review, not audio: fixed on 16 October 2026.** With Settings > Wait for Continue on, Scene 9's
eye-to-brain cinematic stopped at its second beat until Skip was pressed, because the Continue button lives in the
caption panel, which is hidden during cinematics. `ModuleBoot` now waits for Continue only outside cinematics. See
`HANDOFF.md` and `Docs/HumanEye/Polish/Captures/wait-continue-2026-10-16/`.

**Owner action.** The patched scene, mixer, chapter assets and audio import settings were copied back to the project
(originals in `_backups/AI-HUMAN-EYE_2026-10-15_pre-audio/`). If `HumanEye_Observatory.unity` is open in the Editor,
reopen it before pressing Play.

## 10. Credits

Music: Cleyton Kauffman (Exploration Theme), turtletooth (The Cogs), Yoiyami (First Light Particles), vitalezzz (Claimed by
the Void), wipics (Outer Space Loop), SRG774 (Dark Sci-Fi Audio Pack), Theforeshadower (Simple BGM Loop), MintoDog (Space
City). Effects: Kenney (www.kenney.nl: Sci-Fi Sounds, Interface Sounds, UI Audio), JaggedStone (Magic Spell SFX), Ogrebane
(Teleport Spell), bart (18 random video game sound effects). All CC0 1.0. See `Assets/HumanEye/Audio/Licenses/`.
