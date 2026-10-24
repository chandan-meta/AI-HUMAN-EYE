# Chapter / camera cue sheet

Generated from the chapter definitions and the scene's CinemachineCamera shots (Tools > Human Eye > Validate Module). Dialogue text is quoted exactly from the ledger.

Scene-to-chapter mapping: one launch scene `Assets/HumanEye/Scenes/HumanEye_Observatory.unity`; chapters `Scene02`–`Scene11` are `ChapterDefinition` assets in `Assets/HumanEye/Generated/Data/Chapters/`, played in order by `ChapterRunner`.

## Scene02 — Meet Niko

Checkpoint (restart/resume state): Dark corridor facing the closed gate; the opening (skippable) lifts the gate, enters the lab, scans the subject once, flies the selected eye to the central display and brings Niko in beside it; nothing explored.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Do |  |  |  |  | intro | Pause, Help, Settings | Action complete |
| 2 | Do |  |  |  |  | niko(Wave) | Pause, Help, Settings | Action complete |
| 3 | Shot | Niko introduction · medium | checkpoint → S02_NikoMedium (-1.97,2.75,-1.94 · yaw 360 pitch 7 · 36°) | Ease-in-out blend 1.6 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 4 | Say |  |  |  | S02_L01: “Hey! I’m Niko. Your Vision Investigator Officer.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 5 | Say |  |  |  | S02_L02: “So you want to know how light becomes sight?” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 6 | Say |  |  |  | S02_L03: “Then watching from out here won't help.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 7 | Say |  |  |  | S02_L04: “We need to go inside somewhere humans can't.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 8 | Do |  |  |  |  | nikoLook(eye) | Pause, Help, Settings | Action complete |
| 9 | Do |  |  |  |  | niko(Present) | Pause, Help, Settings | Action complete |
| 10 | Do |  |  |  |  | music(eye) | Pause, Help, Settings | Action complete |
| 11 | Do |  |  |  |  | sfx(eyeappear) | Pause, Help, Settings | Action complete |
| 12 | Shot | Enlarged complete eye · medium | S02_NikoMedium → S02_EyeHero (-0.30,3.15,-3.20 · yaw 1 pitch 6 · 36°) | Ease-in-out blend 2.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 13 | Do |  |  |  |  | nikoMove(EyeLeft) | Pause, Help, Settings | Starts; runs alongside |
| 14 | Do |  |  |  |  | turntable(on) | Pause, Help, Settings | Action complete |
| 15 | Say |  |  |  | S02_L06: “There.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 16 | Say |  |  |  | S02_L07: “Your first destination.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 17 | Say |  |  |  | S02_L08: “Here it is. The human eye.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 18 | Do |  |  |  |  | niko(Present) | Pause, Help, Settings | Action complete |
| 19 | Say |  |  |  | S02_L09: “It has several parts, and each one has an important job.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 20 | Do |  |  |  |  | niko(Snap) | Pause, Help, Settings | Action complete |
| 21 | Do |  |  |  |  | sequence(sclera,cornea,iris,pupil) | Pause, Help, Settings | Starts; runs alongside |
| 22 | Say |  |  |  | S02_L10: “Click or ask Niko about each part to explore what it does.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 23 | Explore |  |  |  |  |  | Tap a highlighted part, its label or the Parts list; Ask Niko; orbit/zoom | Every listed part's explanation finished |
| | **Part: Sclera** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   24 | Say |  |  |  | S02_L11: “This tough white outer layer is called the sclera. It protects the eye and helps maintain its shape.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| | **Part: Cornea** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   25 | Say |  |  |  | S02_L12: “This transparent, curved layer at the front is the cornea. It protects the eye and bends incoming light.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| | **Part: Iris** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   26 | Say |  |  |  | S02_L13: “This coloured part is the iris. It controls the size of the pupil.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| | **Part: Pupil** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   27 | Say |  |  |  | S02_L14: “This opening is the pupil. Light enters the eye through it.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 28 | Offer |  |  |  |  | topic:external_structures | Answer, hint, Show me (after two misses), Continue anyway (after a miss) | Check answered (help after misses; answer shown after three) |
| 29 | Shot | Enlarged complete eye · medium | S02_EyeHero → S02_EyeHero (-0.30,3.15,-3.20 · yaw 1 pitch 6 · 36°) | Ease-in-out blend 1 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 30 | Do |  |  |  |  | niko(Present) | Pause, Help, Settings | Action complete |
| 31 | Say |  |  |  | S02_L15: “We’ve seen the parts we can see from the outside. Now, let’s look inside.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 32 | Say |  |  |  | S02_L16 [PROMPT VO]: “Click on the Inside View button to see how the eye looks from the inside.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 33 | Await |  |  |  |  | inside | The awaited control (click/tap); orbit/zoom; Pause; Ask Niko | Learner action 'inside' |
| 34 | Do |  |  |  |  | sfx(unlock) | Pause, Help, Settings | Action complete |

## Scene03 — Inside the Eye

Checkpoint (restart/resume state): Closed eye at the hero point in the Scene 2 orientation; Niko beside it; camera S02_EyeHero.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Do |  |  |  |  | corneaClear | Pause, Help, Settings | Action complete |
| 2 | Shot | Complete cutaway eye · medium | checkpoint → S03_InsideHero (-0.25,3.15,-3.10 · yaw 1 pitch 6 · 36°) | Ease-in-out blend 2.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 3 | Do |  |  |  |  | turnToSide | Pause, Help, Settings | Action complete |
| 4 | Do |  |  |  |  | cutaway(open) | Pause, Help, Settings | Action complete |
| 5 | Do |  |  |  |  | niko(Snap) | Pause, Help, Settings | Action complete |
| 6 | Do |  |  |  |  | sequence(aqueous,lens,vitreous,retina,choroid) | Pause, Help, Settings | Starts; runs alongside |
| 7 | Say |  |  |  | S03_L01: “Click or ask Niko about each part to explore what it does.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 8 | Explore |  |  |  |  |  | Tap a highlighted part, its label or the Parts list; Ask Niko; orbit/zoom | Every listed part's explanation finished |
| | **Part: Aqueous humour** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   9 | Say |  |  |  | S03_L02: “This space between the cornea and the lens contains a clear fluid called aqueous humour.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   10 | Say |  |  |  | S03_L03: “It helps nourish the front parts of the eye and helps maintain pressure inside the eye.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| | **Part: Crystalline lens** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   11 | Say |  |  |  | S03_L04: “Behind the pupil is the eye’s natural crystalline lens.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   12 | Shot | Biconvex lens close-up · close-up | S03_InsideHero → S03_Lens_Close (0.79,2.96,-0.69 · yaw 293 pitch 2 · 26°) | Ease-in-out blend 1.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
|   13 | Say |  |  |  | S03_L05: “It is transparent, flexible and biconvex, meaning it bulges outward on both sides.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   14 | Do |  |  |  |  | lensFlex | Pause, Help, Settings | Starts; runs alongside |
|   15 | Say |  |  |  | S03_L06: “It can change its shape to help focus light from objects at different distances.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| | **Part: Vitreous humour** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   16 | Shot | Through the vitreous toward the back · wide | S03_Lens_Close → S03_Vitreous_Back (1.19,2.99,-0.48 · yaw 294 pitch 2 · 38°) | Ease-in-out blend 1.8 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
|   17 | Say |  |  |  | S03_L07: “Most of the space behind the lens is filled with a clear, jelly-like substance called vitreous humour.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   18 | Say |  |  |  | S03_L08: “It helps maintain the shape of the eye.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| | **Part: Retina** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   19 | Say |  |  |  | S03_L09: “This inner layer at the back is the retina.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   20 | Shot | Retinal surface detail · close-up | S03_Vitreous_Back → S03_Retina_Close (0.52,2.87,-0.05 · yaw 310 pitch 9 · 28°) | Ease-in-out blend 1.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
|   21 | Do |  |  |  |  | retinaDetail | Pause, Help, Settings | Action complete |
|   22 | Say |  |  |  | S03_L10: “It contains light-sensitive cells that detect light.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| | **Part: Choroid** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   23 | Shot | Retina → choroid → sclera layers · medium | S03_Retina_Close → S03_Choroid_Deep (0.53,3.20,-0.04 · yaw 302 pitch 355 · 30°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
|   24 | Say |  |  |  | S03_L11: “Behind the retina is the choroid, a layer rich in blood vessels that supplies the eye with oxygen and nutrients.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 25 | Offer |  |  |  |  | topic:internal_structures | Answer, hint, Show me (after two misses), Continue anyway (after a miss) | Check answered (help after misses; answer shown after three) |
| 26 | Shot | Complete cutaway eye · medium | S03_Choroid_Deep → S03_InsideHero (-0.25,3.15,-3.10 · yaw 1 pitch 6 · 36°) | Ease-in-out blend 1.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 27 | Do |  |  |  |  | niko(Present) | Pause, Help, Settings | Action complete |
| 28 | Say |  |  |  | S03_L12: “Now we’ve explored the main structures of the eye.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |

## Scene04 — How Is the Eye Connected to the Brain?

Checkpoint (restart/resume state): Cutaway eye at the cut point; only the nerve stub visible; brain hidden; camera S03_InsideHero.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Shot | Around to the back of the eye · medium | checkpoint → S04_EyeRear (1.35,3.30,-1.45 · yaw 327 pitch 11 · 31°) | Ease-in-out blend 2.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 2 | Say |  |  |  | S04_L01: “But there’s something important we haven’t explored yet.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 3 | Shot | Back of the eye, closer · medium | S04_EyeRear → S04_EyeRearClose (1.00,3.17,-0.95 · yaw 329 pitch 14 · 29°) | Ease-in-out blend 1.8 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 4 | Say |  |  |  | S04_L02: “We know that the retina detects light…” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 5 | Do |  |  |  |  | nerveReveal | Pause, Help, Settings | Starts; runs alongside |
| 6 | Say |  |  |  | S04_L03: “…but how does that information reach the brain?” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 7 | Do |  |  |  |  | glow(optic_nerve) | Pause, Help, Settings | Action complete |
| 8 | Do |  |  |  |  | labels(focus:optic_nerve) | Pause, Help, Settings | Action complete |
| 9 | Say |  |  |  | S04_L04: “Let’s follow it.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 10 | Shot | Optic nerve leaving the eye · medium | S04_EyeRearClose → S04_NerveClose (0.61,3.22,-0.62 · yaw 351 pitch 20 · 30°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 11 | Say |  |  |  | S04_L05: “This bundle is the optic nerve.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 12 | Do |  |  |  |  | brainReveal | Pause, Help, Settings | Starts; runs alongside |
| 13 | Shot | Following the nerve to the brain · medium | S04_NerveClose → S04_NerveMid (1.48,3.12,-0.82 · yaw 348 pitch 14 · 34°) | Ease-in-out blend 2.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 14 | Do |  |  |  |  | brainGhost(on) | Pause, Help, Settings | Action complete |
| 15 | Do |  |  |  |  | signals(nerve) | Pause, Help, Settings | Action complete |
| 16 | Say |  |  |  | S04_L06: “It carries visual information from the retina to the brain.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 17 | Shot | Visual processing area (back of brain) · medium | S04_NerveMid → S04_BrainClose (3.30,3.63,-1.55 · yaw 328 pitch 13 · 36°) | Ease-in-out blend 2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 18 | Do |  |  |  |  | sfx(cortex) | Pause, Help, Settings | Action complete |
| 19 | Do |  |  |  |  | glow(visual_area) | Pause, Help, Settings | Action complete |
| 20 | Do |  |  |  |  | labels(focus:visual_area) | Pause, Help, Settings | Action complete |
| 21 | Say |  |  |  | S04_L07: “The brain receives and processes this information so that we can see.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 22 | Do |  |  |  |  | signals(nerve) | Pause, Help, Settings | Action complete |
| 23 | Do |  |  |  |  | brainGhost(off) | Pause, Help, Settings | Action complete |
| 24 | Do |  |  |  |  | glow(optic_nerve,visual_area) | Pause, Help, Settings | Action complete |
| 25 | Shot | Eye, optic nerve and brain together · wide | S04_BrainClose → S04_Wide (1.20,3.60,-4.70 · yaw 0 pitch 7 · 38°) | Ease-in-out blend 2.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 26 | Do |  |  |  |  | labels(focus:optic_nerve,brain) | Pause, Help, Settings | Action complete |
| 27 | Say |  |  |  | S04_L08: “So the eye and brain are connected through the optic nerve.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 28 | Do |  |  |  |  | labels | Pause, Help, Settings | Action complete |
| 29 | Offer |  |  |  |  | item:nerve_tap | Answer, hint, Show me (after two misses), Continue anyway (after a miss) | Check answered (help after misses; answer shown after three) |

## Scene05 — Light Enters the Eye

Checkpoint (restart/resume state): Cutaway eye, nerve and brain; no object yet; pupil at default size; brightness 50%; camera S05_ObjectWide.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Do |  |  |  |  | objectAppear | Pause, Help, Settings | Action complete |
| 2 | Say |  |  |  | S05_L01: “We've been looking at the eye. Now let’s follow what happens when you look at an object.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 3 | Do |  |  |  |  | lightTravel(1) | Pause, Help, Settings | Starts; runs alongside |
| 4 | Shot | Following light to the eye · medium | checkpoint → S05_FollowLight (-1.25,3.15,-2.30 · yaw 9 pitch 5 · 34°) | Ease-in-out blend 2.5 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 5 | Say |  |  |  | S05_L02: “Vision begins when light from an object reaches the eye.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 6 | Shot | Light bending at the cornea · medium | S05_FollowLight → S05_Cornea (-0.38,3.03,-1.04 · yaw 8 pitch 4 · 32°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 7 | Do |  |  |  |  | isolate(cornea) | Pause, Help, Settings | Action complete |
| 8 | Do |  |  |  |  | labels(focus:cornea) | Pause, Help, Settings | Action complete |
| 9 | Do |  |  |  |  | lightTravel(1.5) | Pause, Help, Settings | Starts; runs alongside |
| 10 | Say |  |  |  | S05_L03: “First, the light passes through the cornea, which bends it.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 11 | Shot | Aqueous humour and pupil · medium | S05_Cornea → S05_AqueousPupil (-0.32,3.01,-0.88 · yaw 5 pitch 4 · 30°) | Ease-in-out blend 1.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 12 | Do |  |  |  |  | isolateKeep(aqueous,pupil,iris) | Pause, Help, Settings | Action complete |
| 13 | Do |  |  |  |  | labels(focus:aqueous) | Pause, Help, Settings | Action complete |
| 14 | Do |  |  |  |  | lightTravel(2) | Pause, Help, Settings | Starts; runs alongside |
| 15 | Say |  |  |  | S05_L04: “It then travels through the aqueous humour and enters through the pupil.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 16 | Do |  |  |  |  | restore | Pause, Help, Settings | Action complete |
| 17 | Do |  |  |  |  | labels | Pause, Help, Settings | Action complete |
| 18 | Do |  |  |  |  | pupilView | Pause, Help, Settings | Action complete |
| 19 | Do |  |  |  |  | brightnessLive | Pause, Help, Settings | Action complete |
| 20 | Say |  |  |  | S05_L05 [PROMPT VO]: “Change the brightness and observe what happens to the pupil.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 21 | Await |  |  |  |  | brighter | The awaited control (click/tap); orbit/zoom; Pause; Ask Niko | Learner action 'brighter' |
| 22 | Do |  |  |  |  | pupilSettle | Pause, Help, Settings | Action complete |
| 23 | Say |  |  |  | S05_L06: “In bright light, the pupil becomes smaller.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 24 | Await |  |  |  |  | dimmer | The awaited control (click/tap); orbit/zoom; Pause; Ask Niko | Learner action 'dimmer' |
| 25 | Do |  |  |  |  | pupilSettle | Pause, Help, Settings | Action complete |
| 26 | Say |  |  |  | S05_L07: “In dimmer light, the pupil becomes larger.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 27 | Do |  |  |  |  | sfx(iris) | Pause, Help, Settings | Action complete |
| 28 | Do |  |  |  |  | glow(iris) | Pause, Help, Settings | Action complete |
| 29 | Do |  |  |  |  | labels(focus:iris) | Pause, Help, Settings | Action complete |
| 30 | Say |  |  |  | S05_L08: “The iris controls the size of the pupil and therefore regulates how much light enters the eye.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 31 | Do |  |  |  |  | glow(iris) | Pause, Help, Settings | Action complete |
| 32 | Do |  |  |  |  | labels | Pause, Help, Settings | Action complete |
| 33 | Offer |  |  |  |  | item:pupil_predict | Answer, hint, Show me (after two misses), Continue anyway (after a miss) | Check answered (help after misses; answer shown after three) |
| 34 | Free |  |  |  |  | brightness | Experiment controls, Freeze/Replay, view controls, Continue | Learner taps Continue |

## Scene06 — The Lens Focuses the Light

Checkpoint (restart/resume state): Cutaway eye; apple at medium distance with the lens matched; light reaching the pupil; camera S05_AqueousPupil.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Shot | Rays through the lens · wide | checkpoint → S06_LensClose (-0.04,2.97,-1.22 · yaw 1 pitch 2 · 38°) | Ease-in-out blend 1.8 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 2 | Do |  |  |  |  | isolate(lens) | Pause, Help, Settings | Action complete |
| 3 | Do |  |  |  |  | labels(focus:lens) | Pause, Help, Settings | Action complete |
| 4 | Do |  |  |  |  | lightTravel(4) | Pause, Help, Settings | Starts; runs alongside |
| 5 | Say |  |  |  | S06_L01: “After passing through the pupil, the light reaches the eye’s crystalline lens.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 6 | Do |  |  |  |  | restore | Pause, Help, Settings | Action complete |
| 7 | Do |  |  |  |  | distanceControls | Pause, Help, Settings | Action complete |
| 8 | Await |  |  |  |  | near | The awaited control (click/tap); orbit/zoom; Pause; Ask Niko | Learner action 'near' |
| 9 | Shot | Near object and eye · medium | S06_LensClose → S06_NearWide (-1.05,3.20,-3.00 · yaw 2 pitch 5 · 34°) | Ease-in-out blend 1.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 10 | Do |  |  |  |  | focusView(on) | Pause, Help, Settings | Action complete |
| 11 | Do |  |  |  |  | nearApproach | Pause, Help, Settings | Action complete |
| 12 | Shot | Rays through the lens · wide | S06_NearWide → S06_LensClose (-0.04,2.97,-1.22 · yaw 1 pitch 2 · 38°) | Ease-in-out blend 1.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.2 s; short fade when reduced motion) |
| 13 | Do |  |  |  |  | lensFocus(near) | Pause, Help, Settings | Starts; runs alongside |
| 14 | Say |  |  |  | S06_L02: “For a nearby object, the lens becomes more curved so that the light can be focused correctly.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 15 | Await |  |  |  |  | distant | The awaited control (click/tap); orbit/zoom; Pause; Ask Niko | Learner action 'distant' |
| 16 | Shot | Distant object beyond the eye · wide | S06_LensClose → S06_FarView (1.20,3.20,-1.55 · yaw 284 pitch 2 · 40°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 17 | Do |  |  |  |  | distantApproach | Pause, Help, Settings | Action complete |
| 18 | Shot | Rays through the lens · wide | S06_FarView → S06_LensClose (-0.04,2.97,-1.22 · yaw 1 pitch 2 · 38°) | Ease-in-out blend 1.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.2 s; short fade when reduced motion) |
| 19 | Do |  |  |  |  | lensFocus(distant) | Pause, Help, Settings | Starts; runs alongside |
| 20 | Say |  |  |  | S06_L03: “For a distant object, the lens becomes less curved.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 21 | Do |  |  |  |  | ciliaryDemo | Pause, Help, Settings | Starts; runs alongside |
| 22 | Say |  |  |  | acc_ciliary [supplemental] |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 23 | Say |  |  |  | S06_L04: “This continuous adjustment of the lens to focus objects at different distances is called accommodation.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 24 | Do |  |  |  |  | glow(lens) | Pause, Help, Settings | Action complete |
| 25 | Do |  |  |  |  | labels | Pause, Help, Settings | Action complete |
| 26 | Do |  |  |  |  | focusView(off) | Pause, Help, Settings | Action complete |
| 27 | Shot | Light continuing through the vitreous · medium | S06_LensClose → S06_Vitreous (-0.04,3.05,-1.36 · yaw 2 pitch 4 · 34°) | Ease-in-out blend 2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 28 | Do |  |  |  |  | lightTravel(5) | Pause, Help, Settings | Action complete |
| 29 | Offer |  |  |  |  | topic:accommodation | Answer, hint, Show me (after two misses), Continue anyway (after a miss) | Check answered (help after misses; answer shown after three) |

## Scene07 — Image Forms on the Retina

Checkpoint (restart/resume state): Cutaway eye; apple at 6 eye-radii with the lens matched; light through the lens; no image yet; camera S06_Vitreous.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Shot | Toward the back of the eye · medium | checkpoint → S07_Vitreous (0.02,3.03,-1.00 · yaw 2 pitch 6 · 32°) | Ease-in-out blend 2.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 2 | Do |  |  |  |  | isolateKeep(vitreous,lens,retina) | Pause, Help, Settings | Action complete |
| 3 | Do |  |  |  |  | labels(focus:vitreous) | Pause, Help, Settings | Action complete |
| 4 | Do |  |  |  |  | lightTravel(5) | Pause, Help, Settings | Starts; runs alongside |
| 5 | Say |  |  |  | S07_L01: “After passing through the lens and vitreous humour, the light reaches the retina.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 6 | Do |  |  |  |  | isolate(retina) | Pause, Help, Settings | Action complete |
| 7 | Do |  |  |  |  | labels(focus:retina) | Pause, Help, Settings | Action complete |
| 8 | Shot | Rays converging; inverted image · close-up | S07_Vitreous → S07_Retina (0.14,2.97,-0.76 · yaw 13 pitch 1 · 28°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 9 | Do |  |  |  |  | imageForm | Pause, Help, Settings | Starts; runs alongside |
| 10 | Do |  |  |  |  | imageInset | Pause, Help, Settings | Action complete |
| 11 | Say |  |  |  | S07_L02: “The lens forms a real, inverted image of the object on the retina.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 12 | Do |  |  |  |  | restore | Pause, Help, Settings | Action complete |
| 13 | Do |  |  |  |  | imageInset(hide) | Pause, Help, Settings | Action complete |
| 14 | Shot | Retinal surface · close-up | S07_Retina → S07_RetinaCloser (0.22,2.96,-0.50 · yaw 13 pitch 1 · 24°) | Ease-in-out blend 1.6 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 15 | Say |  |  |  | S07_L03: “But what exactly detects this light on the retina?” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 16 | Do |  |  |  |  | labels | Pause, Help, Settings | Action complete |

## Scene08 — Photoreceptors Detect Light

Checkpoint (restart/resume state): Image on the retina (end of Scene 7); magnified retina prepared with light and signals off.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Do |  |  |  |  | intoRetina | Pause, Help, Settings | Action complete |
| 2 | Say |  |  |  | S08_L01: “The retina contains light-sensitive cells called photoreceptors.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 3 | Do |  |  |  |  | microLight(0.3) | Pause, Help, Settings | Action complete |
| 4 | Do |  |  |  |  | microFocus(rods) | Pause, Help, Settings | Action complete |
| 5 | Shot | Rods · medium | checkpoint → S08_Rods (58.10,1.35,-3.30 · yaw 0 pitch 9 · 30°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 6 | Do |  |  |  |  | labels(focus:rods) | Pause, Help, Settings | Action complete |
| 7 | Say |  |  |  | S08_L02: “Rods are especially sensitive to low levels of light and help us see in dim conditions.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 8 | Say |  |  |  | S08_L03: “They mainly provide information about brightness rather than colour.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 9 | Do |  |  |  |  | microLight(0.95) | Pause, Help, Settings | Action complete |
| 10 | Do |  |  |  |  | microFocus(cones) | Pause, Help, Settings | Action complete |
| 11 | Shot | Cones · medium | S08_Rods → S08_Cones (59.64,1.20,-3.30 · yaw 0 pitch 8 · 30°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 12 | Do |  |  |  |  | labels(focus:cones) | Pause, Help, Settings | Action complete |
| 13 | Say |  |  |  | S08_L04: “Cones work best in brighter light and allow us to see colours and fine details.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 14 | Do |  |  |  |  | microFocus(all) | Pause, Help, Settings | Action complete |
| 15 | Shot | One photoreceptor column: light arriving, a signal leaving (phototransduction) · medium | S08_Cones → S08_Transduce (59.70,1.90,-4.60 · yaw 0 pitch 5 · 36°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 16 | Do |  |  |  |  | microLight(0.65) | Pause, Help, Settings | Action complete |
| 17 | Do |  |  |  |  | labels(scan) | Pause, Help, Settings | Action complete |
| 18 | Do |  |  |  |  | microSignals | Pause, Help, Settings | Action complete |
| 19 | Say |  |  |  | S08_L05: “When light reaches these cells, it is converted into electrical signals.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 20 | Shot | Signals through retinal cells · wide | S08_Transduce → S08_Signals (62.20,3.00,-4.90 · yaw 0 pitch 9 · 38°) | Ease-in-out blend 1.8 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 21 | Say |  |  |  | S08_L06: “These signals are passed through the retina toward the optic nerve.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 22 | Shot | Photoreceptors, retina and optic nerve together · wide | S08_Signals → S08_PullBack (61.00,2.30,-10.80 · yaw 0 pitch 5 · 48°) | Ease-in-out blend 2.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 23 | Offer |  |  |  |  | topic:rods_cones | Answer, hint, Show me (after two misses), Continue anyway (after a miss) | Check answered (help after misses; answer shown after three) |
| 24 | Free |  |  |  |  | photoreceptors | Experiment controls, Freeze/Replay, view controls, Continue | Learner taps Continue |

## Scene09 — Signals Travel to the Brain

Checkpoint (restart/resume state): Cutaway eye, full optic nerve and brain; apple placed at medium distance; camera on the back of the eye.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Do |  |  |  |  | eyeToBrain | Pause, Help, Settings | Starts; runs alongside |
| 2 | Do |  |  |  |  | cineBeat(signals) | Pause, Help, Settings | Action complete |
| 3 | Say |  |  |  | S09_L01: “The electrical signals leave the eye through the optic nerve.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 4 | Do |  |  |  |  | cineBeat(pathway) | Pause, Help, Settings | Action complete |
| 5 | Say |  |  |  | S09_L02: “The optic nerve carries this visual information to the brain.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 6 | Do |  |  |  |  | cineBeat(brain) | Pause, Help, Settings | Action complete |
| 7 | Say |  |  |  | S09_L03: “The brain receives and processes these signals to produce our visual perception.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 8 | Do |  |  |  |  | cineBeat(end) | Pause, Help, Settings | Action complete |
| 9 | Do |  |  |  |  | perception | Pause, Help, Settings | Action complete |
| 10 | Say |  |  |  | S09_L04: “Although the image formed on the retina is inverted, our visual experience is not simply a photograph of that retinal image. The brain processes the information carried by the signals.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 11 | Do |  |  |  |  | glow(visual_area) | Pause, Help, Settings | Action complete |
| 12 | Do |  |  |  |  | labels | Pause, Help, Settings | Action complete |
| 13 | Do |  |  |  |  | imageInset(hide) | Pause, Help, Settings | Action complete |
| 14 | Shot | Eye, nerve and brain (Scene 4 continuity) · wide | checkpoint → S09_Wide (1.20,3.60,-4.70 · yaw 0 pitch 7 · 38°) | Ease-in-out blend 2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 15 | Offer |  |  |  |  | topic:optic_nerve | Answer, hint, Show me (after two misses), Continue anyway (after a miss) | Check answered (help after misses; answer shown after three) |
| 16 | Offer |  |  |  |  | topic:inverted_image | Answer, hint, Show me (after two misses), Continue anyway (after a miss) | Check answered (help after misses; answer shown after three) |

## Scene10 — The Complete Working

Checkpoint (restart/resume state): Complete scene: apple, light path to the retina, eye, optic nerve and brain; camera S09_Wide.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Shot | Whole pathway: object → eye → nerve → brain · wide | checkpoint → S10_Journey (-0.10,3.85,-7.00 · yaw 2 pitch 7 · 48°) | Ease-in-out blend 2.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 2 | Say |  |  |  | S10_L01: “So seeing is a complete process.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 3 | Do |  |  |  |  | journey | Pause, Help, Settings | Action complete |
| 4 | Do |  |  |  |  | overview | Pause, Help, Settings | Action complete |
| 5 | Say |  |  |  | S10_L02: “Now you’ve followed the journey from the moment light enters the eye to the moment the brain processes the information.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |

## Scene11 — Final Curiosity

Checkpoint (restart/resume state): Cutaway eye closing to the complete eye; near apple and a distant tree placed, both sharp; camera S10_Complete.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Do |  |  |  |  | closeEye | Pause, Help, Settings | Action complete |
| 2 | Do |  |  |  |  | faceLearner | Pause, Help, Settings | Starts; runs alongside |
| 3 | Shot | Complete eye · wide | checkpoint → S11_Eye (1.05,3.10,-3.80 · yaw 0 pitch 9 · 40°) | Ease-in-out blend 1.8 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 4 | Say |  |  |  | S11_L01: “Your eyes are constantly responding to the world around you—even when you’re not thinking about it.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 5 | Do |  |  |  |  | turnToSide | Pause, Help, Settings | Starts; runs alongside |
| 6 | Shot | Near apple and distant tree together (both sharp) · wide | S11_Eye → S11_NearFar (2.00,3.20,-1.90 · yaw 284 pitch 6 · 44°) | Ease-in-out blend 2.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 7 | Say |  |  |  | S11_L02: “So here’s something to think about…what happens when the eye cannot focus light properly?” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 8 | Say |  |  |  | S11_L03: “Why might some things look blurry, even when your eyes are working?” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 9 | Do |  |  |  |  | closing | Pause, Help, Settings | Action complete |

