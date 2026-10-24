# Reference shot list — Human Brain intro → Human Eye intro (8 October 2026)

Source studied: the owner's screen recording of the Human Brain lesson (`20261008-0506-18.9676369.mp4`; on this machine
`C:\Users\ADMIN\Videos\Screen Recordings\Screen Recording 2026-10-08 103844.mp4`, 140.2 s, 1770×996, 30 fps, AAC) and the
12-frame sheet `human_brain_to_eye_camera_reference_sheet.jpg` (Downloads). Frames were extracted with FFmpeg at 1, 2, 4 and
10 fps around every transition; audio was measured in 0.25 s RMS windows. Browser chrome and desktop are ignored. The
reference picture is itself letterboxed (rows 114–880 of 996, ≈ 2.31:1) — the same ratio as this module's intro letterbox
(bars at 11.5 % top and bottom), so the comparison sheets crop both to 2.31:1.

The eye visual reference named in the brief, `image(20261008-050928).png`, was **not found** on this machine (Downloads,
Desktop, Pictures, Videos, Documents, OneDrive, D:\ were searched). The eye's look follows the brief's written description.

## 1 · What the reference does (video time)

| τ (s) | Shot | Camera | Subject / framing | Transition in | Effects and sound |
|---|---|---|---|---|---|
| 6.0–10.1 | Corridor | slow straight dolly toward the door, eye height, centred | door ≈ 65 → 75 % of picture height | fade from the menu | warm wall lights; music bed |
| 10.15–12.6 | Low angle | very low, close, looking up; slight lateral drift | lower half of the door panel, frame edge left | **hard cut** | — |
| 12.65–14.5 | Door lift | centred medium, nearly still | door + frame + side lights | **hard cut** | panel lifts (≈ 1.8 s); sparks at the gap; mechanical accent |
| 14.5–22.0 | Door reveal / push | slow push toward the opening | lab beyond: dome, human tiny on the platform; lab title on the dome | — | gold dust pours toward camera (14.5–17); swell |
| 22.0–24.0 | Threshold | continues through the frame | frame edges leave the picture; lab lights come up at 22.9–23.3 | — | light shaft from above |
| 24.0–35.0 | Lab dolly | slow push and slight descent toward the human | human ≈ 20 → 55 % of height, centred, platform below | — | — |
| 35.0–41.0 | Full body | slow push | human ≈ 55 → 70 % | — | nervous-system glow lines (brain-specific; not adapted) |
| 41.0–49.0 | Face dolly | continuous push, decelerating; camera rises to eye level; FOV constant | full body → waist → chest → head and shoulders → face (head ≈ 40 %) | — | captions: "Every second, your body is doing something." / "But where is all of it being controlled?" |
| 49.0–54.5 | Skin → muscle | still | face | **top-down wipe 50.5–51.0 with a thin bright line** | — |
| 54.5–60.5 | Muscle → skull | still | head | top-down wipe 54.5–55.0 | "Protected inside your skull…" |
| 60.5–67.5 | Skull → glass | wipe 60.5–61.5, then slow orbit ≈ 35° to one side, slightly lower | glass skull, organ inside | wipe | "…is one of the most extraordinary organs… The brain." |
| 67.5–72.0 | Extraction | continuous crane up following the rising organ | organ rises out of the head; ghost head fades by ≈ 69 | — | gold motes trail the organ |
| 72.0–76.0 | Floating organ | reframes, organ centred, lab windows behind | organ ≈ 40 % | — | slow rotation |
| 76.0–84.5 | Hero title | holds | organ under the title | — | horizontal gold burst at 76.0; title typed letter by letter 76.2–78.6; subtitle 79.5 |
| 84.5–97.0 | Companion | separate spot, medium | particles gather on the floor (85.5), vortex (86.5–88), flash ring and reveal (88), wave and greeting | **hard cut** | particle swirl; chime |

Audio: a continuous music bed (−20 to −30 dB RMS with a ≈ 5.3 s loop structure) with broadband accents at the door
(6–8 s, 14–15 s), the reveals (54 s, 60–63 s), the extraction (69–72 s) and the companion (88–89 s); narration above it.

## 2 · Implementation (Stage.Scene02.cs `Intro`, one code-driven dolly `S02_IntroDolly`)

The dolly runs on the reference clock τ. Profile **Reference match** plays τ = 6 + t (91 s). Profile **Production** reaches
each beat's τ in its production length through a monotone cubic time warp (Fritsch–Butland), so holds are shortened and the
path is unchanged: 29.8 s. The profile is `Stage.timing` in the Inspector (Cinematics). Camera keys are a non-uniform
Catmull–Rom spline per continuous segment (velocity continuous, zero at segment ends); a new segment is a cut. Positions
below are relative to the scan subject, who is turned to face the gate for the opening (he turns back behind the cut to
Niko): *d* = metres in front of the eyes along the face direction, *E* = eye height, *P* = platform top.

| Beat | τ | Production (s) | Camera position → target | FOV | Notes |
|---|---|---|---|---|---|
| corridor | 6.0–10.15 | 2.0 | (−29.5, 2.0, 0.67) → (−27.3, 2.0, 0.67); gate (−17.16, 3.3) | 50 | fade up 6.0–6.7 |
| gate-low | 10.15–12.65 | 1.2 | (−19.25, 0.85, 0.05) → (−19.15, 0.9, 0.55); up at the panel (4.6–4.7 m) | 52 | cut |
| gate-lift | 12.65–14.5 | 1.6 | (−25.0, 1.85) → (−24.8, 1.85); gate | 50 | cut; bolt then panel lift 12.7–14.7 (`LabEntrance`, lifts 6.2 m); sparks |
| gate-push | 14.5–22.0 | 2.0 | → (−22.6, 1.95) → (−19.9, 2.1); target moves from the gate to the subject | 48→46 | gold dust stream 14.3–21; lab at 30 % light |
| threshold | 22.0–24.0 | 1.0 | through the frame to d 14.6, P+1.45 | 44 | lab lights 22.4–23.6 |
| lab | 24.0–35.0 | 2.8 | d 14.6 → 9.2 → 4.5; P+1.45 → 1.08; target P+0.9 | 44→40 | fine dust around the subject |
| body | 35.0–41.0 | 0.8 | d 4.5 → 3.3 | 40 | caption 1 |
| face | 41.0–49.0 | 2.8 | d 3.3 → 1.8 → 1.1 → 0.84 → 0.78; E−0.2 → E | 40 | caption 2; face key ramps in |
| skin | 49.0–54.5 | 1.8 | d 0.78 → 0.76 | 40 | skin → muscle wipe 50.5–51.2 (crown → collar seam); orbicularis oculi glows 51.3–54.4 |
| muscle | 54.5–60.5 | 1.8 | d 0.76 → 0.74 | 40 | muscle → bone wipe 54.5–55.1; caption 3 |
| xray | 60.5–67.5 | 2.8 | orbit 0 → 10 → 24 → 34° toward the selected eye, d 0.76, E−0.04 | 40 | bone → glass wipe 60.5–61.5; eye muscles, optic nerves and chiasm fade in 61–63; caption 4 |
| extract | 67.5–72.0 | 2.2 | crane: Bézier from the orbit pose, up and back, to 3 m in front of the presentation point | 40 | selected eye: socket → presentation point (1.1 m in front of the face), radius 16 mm → 0.4 m (exponential); head, skin and other eye fade 68–70; gold motes trail |
| float | 72.0–76.0 | 1.2 | slow push 18 cm | 40 | turntable sway on; eye key and rim |
| title | 76.0–84.5 | 2.8 | (continues) | 40 | gold disc burst 76.0; "INSIDE YOUR EYES" typed 76.2–78.6; "Discover How You See the World" 79.4–80.4 |
| niko | 84.5–97.0 | 3.0 | cut to Niko's anchor (1.75 m in front, 0.62 m up); blend to `S02_NikoMedium` from 93.5 | 38 | gold vortex 85.3; Niko scales in at 87.9 with a burst |

Reduced motion: four stills through black (the lab, the glass skull with the eyes in their orbits, the floating eye under its
title, Niko) — 7.3 s, no travel, no particles. Skip: the finished state at once (eyes in their sockets, subject facing the
learner, lights, grade, title and particles restored, Niko at his anchor on `S02_NikoMedium`).

## 3 · Eye → brain (Scene 9, Stage.EyeBrain.cs, camera `S09_Follow`)

| Beat | Production (s) | Picture | Line |
|---|---|---|---|
| light | 3.0 | side view of the cut-open eye; warm rays from the apple cross cornea, lens and vitreous to the retina | — |
| signals | 3.5 | rays fade; cyan comets leave the retina along the optic nerve; camera moves to the back of the eye | S09_L01 |
| pathway | 7.0 | the eye flies back into its socket in an X-ray head (glass skull, ghost skin); pulses from both eyes along the optic nerves, crossing at the chiasm, the optic tracts, the lateral geniculate nuclei, the optic radiations, to the primary visual cortex; each stage labelled as the pulse reaches it; camera rises over the head | S09_L02 |
| brain | 7.0 | the visual system (eyes, pathway, ghosted brain) rises out of the head, grows ×3.6 and turns; the visual cortex lights on every arrival; gold burst; "FROM EYE TO BRAIN" / "Where Visual Signals Become Perception" | S09_L03 |

Each beat starts only after the previous line has finished; each line waits for its beat (`cineBeat`). Reference-match
timing multiplies the beat lengths by 1.6.
