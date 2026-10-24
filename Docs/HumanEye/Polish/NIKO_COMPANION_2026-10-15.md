# Niko: rigged model, expressions, lip sync and permanent presence (15 October 2026)

The owner supplied an updated `C:\Users\ADMIN\Documents\Received Files\niko.fbx` (saved 15 October 2026, 15:19) with its own
Mixamo-named skeleton and modelled shape keys, and asked for Niko to be fully animated, expressive, lip-synced, always on
screen while the module runs, connected to Talk to Niko, and re-voiced with a cute, curious, childlike ElevenLabs voice.

## The model

| Item | Supplied | In the project |
|---|---|---|
| Skeleton | 43 bones, Mixamo names without the `mixamorig:` prefix; hands have index, ring and thumb only | kept as is; `ApplyNikoCharacter` accepts either naming and takes the fingers that exist |
| Mesh | `niko_geo1` 10,353 vertices + two pocket meshes; up to 21 weights per vertex | joined into one `Niko_Body` (one skinned renderer), at most 4 normalised weights per vertex, 1.2 m tall, feet on the origin |
| Shape keys | `Close` (closes the modelled open smile to a smile line), `Ou` (rounded lips), `Blink`, `Eye_L/R/U/D` (pupils look left/right/up/down) | all kept unchanged; one corrective `BrowUp` added (the modelled brows lift 2.6 cm) for surprise and curiosity |
| Texture | one 4096 base colour, packed | written byte for byte to `Niko_BaseColor.jpg` (same file name and GUID as before), imported at 2048 |

`Tools/HumanEye/export_niko.py` (Blender 4.5, rewritten) does the above; `Niko_ImportReport.json` records the result. The
face was rendered in Blender for every shape and the planned mixes before integration
(`Captures/niko-companion-2026-10-15/blender-*.png`): 0.9 Close is a calm closed smile, 0.5 Close an "ee", Ou + 0.3 Close an
"oo", full Close "m/b/p", BrowUp clean. Two limits of the supplied shapes: a full `Blink` shows faint streaks under the eyes
(the rig blinks to 97 %), and gaze shapes above about 0.35 expose the socket rim (the rig caps gaze at 0.32).

## Animation (one procedural driver, `NikoRig`)

The project's Niko is posed procedurally (no Animator): every frame the bones start from the rest pose and the rig
applies breathing, weight shift, look-at and two-bone arms with a pole vector, so nothing else can fight it for the same
bones or shapes, and skip, pause or a chapter change can never leave a T-pose. The brief's states are states of this
driver (named as in the brief; they are not `.anim` clips):

- **Idle:** `Niko_Idle_Breathing`, `_Curious` (head tilt, looks around), `_Happy` (sway, open smile), `_Thinking` (hand to
  chin, eyes up), cycled every 7 to 12 s while he is silent. Breathing, blinks (with an occasional double blink) and gaze
  continue while the lesson is paused.
- **Talking:** `Niko_Talk_Normal`, `_Excited`, `_Explaining`, `_Question`, `_Encouraging`, chosen from the line's mood. Each
  style has four beat poses (one rests the hands) that change every 2.2 to 3.6 s, with elbow accents and nods on syllable
  onsets, so the hands never repeat one wave.
- **Gestures:** Wave, Greet (wave and a small bow), Goodbye, RaiseHand, Present, Point (the near hand), PointLeft,
  PointRight, Pinch, Snap, Think (hand up by the cheek, the other on the hip), Surprise, Celebrate, Encourage, Listen,
  Excited. A gesture overrides the talk beats for its duration and lays its own mood over the held expression while it
  lasts; the held expression (a line's mood, Listening while Talk to Niko is open) returns afterwards.
- **Arms that fit this body:** Niko's head is wider than his shoulders and rests on them (half-width 0.36 m against
  shoulders at ±0.17 m), each arm segment is 13 cm and his belly is round, so the first version's raised hands went through
  his head. Every pose was re-authored and checked on the shipped mesh with its real skin weights
  (`Tools/HumanEye/niko_arm_clearance.py`, run in Blender; keep its table in step with `NikoRig`): the outer upper arm,
  forearm, hand and fingers stay clear of head and torso with the head anywhere in its range (rolled ±8° combined with 12°
  up to 10° down), talk accents included. Upper arms stay at or below the horizontal; raised hands go up beside the head (the
  forearm bends toward an "up and out" direction per pose); pointing and presenting use a reach between a low front and
  an outward direction chosen by where the target lies, because these arms cannot point up or across the head. The head
  roll is capped at 8° (more sinks the cheek into the shoulder); neck and head pitch together, nods included, stay within
  12° up and 10° down (further down the jaw meets the hands in front); the eyes cover steeper looks.
- **Expressions:** Happy, Curious, Excited, Surprised, Thinking, Explaining, Encouraging, Listening: the resting mouth (Close
  weight), BrowUp, head tilt, lean, gaze offset and bounce.
- **Eyes:** looks are aimed from the eyes (24 cm above the head joint), the pupils lead and cover only what the head has
  not turned yet, so in the companion picture he meets the learner's eyes; blinks every 2.4 to 5.6 s.
- **No pops:** the greeting bow, the happy sway, the question head roll, the encouraging nod, the syllable nods and brow
  lifts, the mouth's switch between resting and talking and the surprised "oo" are all eased; gesture motion fades out
  while the lesson is paused. A line held for Read & Continue counts as silence (no talking hands over a quiet voice).
- **Moods:** a gesture's mood lies over the line's mood only where it can show: Think and Listen keep the line's own mood
  while he speaks (S11_L03 stays curious).

## Lip sync

`NikoRig` drives `Close` and `Ou` from the narrator, with attack/release smoothing (24/s, 15/s):
- **Timed visemes (when available):** each clip may carry `<clip>.align.json`, the character timing returned by
  ElevenLabs' `with-timestamps` endpoint. `Builder.MouthKeys` turns it into mouth keys on the cue (letters to approximate
  visemes: a/e/i/o/u/w/q/r/l/m-b-p/f-v/consonants; a pause over 80 ms closes the mouth), `Narrator.TryMouth` samples them at
  the voice time. This is a spelling-based approximation of visemes, timed to the real audio.
- **From the audio (today's clips, which have no timing data):** `Builder.AudioKeys` reads each WAV when the dialogue data
  is built and stores keys every 30 ms: loudness against the clip's own voiced level opens the jaw, the share of energy at
  200-800 Hz against 1-4 kHz rounds the lips (calibrated on the current voice). Precomputed because WebGL cannot read
  audio output at runtime (the first version's runtime analysis left the mouth shut in the web build). This is
  audio-driven, not phoneme-accurate. The runtime loudness analysis stays only for a cue without keys.
- **The closing line** (not a ledger cue) gets keys too (`Stage.closingCue`, `Builder.ClosingKeys`) and now plays as the
  voice's clip instead of a one-shot, so its mouth follows the audio and pause stops it like any line. It waits for a replay
  or answer still playing, and while it plays the narrator counts as busy, so Replay and Talk to Niko answers queue behind
  it instead of cutting it off.
- Syllable onsets from the keys drive elbow accents, the nod and an occasional brow lift.
- The mouth closes to the expression's resting smile during silence, pause, skip and after the line.

## Always on screen (`NikoPresenter`)

While the module or its opening runs, Niko is visible in every frame: in the main view when the shot frames his face
readably (his own shots, the closing, the opening's hand-over from 29.5 s), otherwise in the live companion picture (his
portrait camera, now framing his upper body from the root so gestures and nods read; 384 px render texture with 4x MSAA),
speaking or silent. The main camera culls his layer while the picture shows him, so he is never shown twice. On the start
screen he stands on the corridor floor beside the closed gate and waves (a `MenuGate` anchor solved against the shot's lens).
The opening no longer switches him off until 30 s: he greets as the gate parts, cheers the lab, presents the title, is
amazed at the first breakup, points toward the anatomy and the brain, cheers the extraction and waves at the hand-over.
In the picture, eye contact goes to the picture camera and pointing goes toward the middle of the screen (PointLeft from
the right-hand picture, PointRight from the left-hand one).

## Context and Talk to Niko

- Every lesson line sets an expression (a table for the 21 lines whose wording calls for one, otherwise "?" curious, "!"
  excited, else explaining); lines about a part point at it; S02_L01 greets; S11_L03 thinks; authored gestures of the same
  beat (Snap, Wave) are no longer overwritten.
- Quiz: a right answer cheers (Celebrate), a wrong one encourages (Encourage).
- Eye-to-brain cinematic and Scene 4: excited as signals start, pointing toward the pathway and brain, surprised when the
  first signal reaches the brain.
- Talk to Niko (an offline, curated Q&A: no network, no generative AI in this project): he listens while the panel is open,
  thinks for 0.7 s when a question arrives, then speaks voiced answers with lip sync; text-only answers get an encouraging
  face and no fake mouth movement. The ending waves goodbye.

## Voice (ElevenLabs): prepared, not generated

`Tools/HumanEye/generate_elevenlabs_vo.py` regenerates every Niko line (the 66 ledger clips, same file names, plus the
closing line) through the owner's ElevenLabs account with per-line mood settings (stability/style; optional `eleven_v3`
audio tags), previous/next text for continuity, loudness-normalised to the existing level, and saves the timing for the
lip sync. **No ElevenLabs key exists on this machine, so no clip was regenerated and the robotic OneCore voice is still in
the build.** To finish: set `ELEVENLABS_API_KEY` as a Windows user environment variable, then

    python -I Tools/HumanEye/generate_elevenlabs_vo.py voices --search child
    python -I Tools/HumanEye/generate_elevenlabs_vo.py samples --voices ID1,ID2,ID3
    python -I Tools/HumanEye/generate_elevenlabs_vo.py all --voice CHOSEN_ID

and re-run **Apply supplied Niko character** (it rebuilds the dialogue data, which picks up the timing files).

## Verification (sandbox copy, 15 October 2026)

The sources and the patched scene that were copied back (scene SHA-256 `6783ae39…`) passed:

| Check | Result |
|---|---|
| Validator | 72 passed, 0 warnings, 0 errors |
| EditMode tests | 27/27, including `AlignmentBecomesMouthKeys` and `AudioBecomesMouthKeys` |
| PlayMode tests | 7/7 (CinematicTests 2/2: Niko visible in the picture at 0.25 s with the main view culling him, on screen through the extraction) |
| Arm clearance, `niko_arm_clearance.py` in Blender on the shipped mesh | 59 pose rows passed over 9 head states |
| Walkthrough 1600×900, stress | 10 chapters, 65 cues, 35 PASS / 0 FAIL, 0 exceptions; Niko on screen in 7,167 of 7,167 running frames; mouth 0.58 open on average while speaking, 0 when silent |
| Walkthrough 1024×768, reduced motion | 10 chapters, 65 cues, 15 PASS / 0 FAIL, 0 exceptions; 4,472 of 4,472 frames |
| Opening only, 1600×900 | 1,299 of 1,299 frames with Niko on screen |
| Scene 9 (eye to brain) | 7 PASS / 0 FAIL; 3,123 of 3,123 frames; one surprise when the first signal arrives |
| Windows player | built; the WebGL development build of the same round built (377 MB); the last review fixes are plain C# |

An independent review (three reviewers, each finding checked by a skeptic) confirmed ten defects in the first version of this
pass and rejected six; all ten are fixed above (syllable nod and brow pops, gesture moods hiding a line's mood, talking hands
during Read & Continue, the closing line being cut off by Replay, the clearance check's head range, a spectrum index on 8 kHz
devices, the pose sheet's timing and side, and the voice tool printing a key with a stray line break).

Evidence (`Captures/niko-companion-2026-10-15/`): `gesture-sheet-front-and-45.png` (every gesture from the front and from his
left), `opening-picture-crops.png` and `opening-companion-picture.png` (the companion picture through the opening),
`opening-point-toward-centre-22-24s.png`, `start-screen-beside-gate.png`, `lesson-chapter2-silent-and-narrated.png`,
`walkthrough-chapters-4-to-end.png`, the walkthrough logs (`walkthrough-*.log`, with `presence_coverage` lines) and the Blender
face renders (`blender-*.png`).

Not verified:
- **Lip sync in a browser.** The WebGL build loaded and ran in the in-app browser up to Niko's first lines, but the pane was
  hidden before the mouth could be measured, and a hidden pane pauses the player. The keyed lip sync uses only
  `AudioSource.time` and `isPlaying`, which Unity lists as supported on the web.
- **The new voice.** No ElevenLabs key exists on this machine, so all 66 clips and the closing line are still the OneCore
  voice; nothing was regenerated.
- A listening review, physical touch devices, and the owner's open Editor (it must reopen `HumanEye_Observatory.unity`).
