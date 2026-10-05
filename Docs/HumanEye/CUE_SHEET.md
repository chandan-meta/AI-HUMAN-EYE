# Chapter / camera cue sheet

Generated from the chapter definitions and the scene's CinemachineCamera shots (Tools > Human Eye > Validate Module). Dialogue text is quoted exactly from the ledger.

Scene-to-chapter mapping: one launch scene `Assets/HumanEye/Scenes/HumanEye_Observatory.unity`; chapters `Scene02`–`Scene11` are `ChapterDefinition` assets in `Assets/HumanEye/Generated/Data/Chapters/`, played in order by `ChapterRunner`.

## Scene02 — Meet Niko

Checkpoint (restart/resume state): Dark room from the door end; the cinematic power-up (skippable) ends on the establishing view with a faint human silhouette on the pedestal; Niko hidden; the selected eye in its socket; nothing explored.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Do |  |  |  |  | intro | Pause, Help, Settings | Action complete |
| 2 | Do |  |  |  |  | nikoAppear | Pause, Help, Settings | Action complete |
| 3 | Shot | Niko introduction · medium | checkpoint → S02_NikoMedium (-1.97,1.30,-1.34 · yaw 353 pitch 6 · 36°) | Ease-in-out blend 1.8 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.8 s; short fade when reduced motion) |
| 4 | Say |  |  |  | S02_L01: “Hey! I’m Niko. Your Vision Investigator Officer.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 5 | Say |  |  |  | S02_L02: “So you want to know how light becomes sight?” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 6 | Do |  |  |  |  | nikoLook(human) | Pause, Help, Settings | Action complete |
| 7 | Say |  |  |  | S02_L03: “Then watching from out here won't help.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 8 | Shot | Niko and the revealed human · wide | S02_NikoMedium → S02_DisplayWide (-0.45,2.29,-3.70 · yaw 1 pitch 4 · 40°) | Ease-in-out blend 1.8 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 9 | Do |  |  |  |  | humanReveal | Pause, Help, Settings | Action complete |
| 10 | Say |  |  |  | S02_L04: “We need to go inside somewhere humans can't.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 11 | Do |  |  |  |  | niko(RaiseHand) | Pause, Help, Settings | Action complete |
| 12 | Say |  |  |  | S02_L05 [PROMPT VO]: “Click on the SCAN button.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 13 | Await |  |  |  |  | scan | The awaited control (click/tap); orbit/zoom; Pause; Ask Niko | Learner action 'scan' |
| 14 | Shot | Full figure for the scan wave · wide | S02_DisplayWide → S02_ScanFront (0.00,1.94,-2.80 · yaw 0 pitch 2 · 44°) | Ease-in-out blend 1.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.2 s; short fade when reduced motion) |
| 15 | Do |  |  |  |  | scan | Pause, Help, Settings | Action complete |
| 16 | Do |  |  |  |  | illuminate | Pause, Help, Settings | Action complete |
| 17 | Say |  |  |  | S02_L06: “There.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 18 | Shot | Selected eye inside the skull (anatomical position) · medium | S02_ScanFront → S02_EyeInSkull (0.02,2.64,1.13 · yaw 354 pitch 3 · 32°) | Ease-in-out blend 2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (2 s; short fade when reduced motion) |
| 19 | Say |  |  |  | S02_L07: “Your first destination.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 20 | Hold |  |  |  |  |  | Pause, Help, Settings | 1.5 s inspection hold |
| 21 | Do |  |  |  |  | nikoLook(eye) | Pause, Help, Settings | Action complete |
| 22 | Shot | Niko's pinch gesture · medium | S02_EyeInSkull → S02_PinchNiko (-1.87,1.35,-0.89 · yaw 348 pitch 5 · 34°) | Ease-in-out blend 1 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1 s; short fade when reduced motion) |
| 23 | Do |  |  |  |  | pinch | Pause, Help, Settings | Action complete |
| 24 | Shot | Enlarged complete eye · medium | S02_PinchNiko → S02_EyeHero (-0.30,1.70,-2.60 · yaw 1 pitch 6 · 36°) | Ease-in-out blend 2.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 25 | Do |  |  |  |  | enlarge | Pause, Help, Settings | Action complete |
| 26 | Do |  |  |  |  | nikoMove(EyeLeft) | Pause, Help, Settings | Action complete |
| 27 | Say |  |  |  | S02_L08: “Here it is. The human eye.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 28 | Do |  |  |  |  | niko(Present) | Pause, Help, Settings | Action complete |
| 29 | Say |  |  |  | S02_L09: “It has several parts, and each one has an important job.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 30 | Do |  |  |  |  | niko(Snap) | Pause, Help, Settings | Action complete |
| 31 | Do |  |  |  |  | sequence(sclera,cornea,iris,pupil) | Pause, Help, Settings | Action complete |
| 32 | Say |  |  |  | S02_L10: “Click or ask Niko about each part to explore what it does.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 33 | Explore |  |  |  |  |  | Tap a highlighted part, its label or the Parts list; Ask Niko; orbit/zoom | Every listed part's explanation finished |
| | **Part: Sclera** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   34 | Say |  |  |  | S02_L11: “This tough white outer layer is called the sclera. It protects the eye and helps maintain its shape.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   35 | Hold |  |  |  |  |  | Pause, Help, Settings | 0.8 s inspection hold |
| | **Part: Cornea** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   36 | Say |  |  |  | S02_L12: “This transparent, curved layer at the front is the cornea. It protects the eye and bends incoming light.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   37 | Hold |  |  |  |  |  | Pause, Help, Settings | 0.8 s inspection hold |
| | **Part: Iris** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   38 | Say |  |  |  | S02_L13: “This coloured part is the iris. It controls the size of the pupil.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   39 | Hold |  |  |  |  |  | Pause, Help, Settings | 0.8 s inspection hold |
| | **Part: Pupil** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   40 | Say |  |  |  | S02_L14: “This opening is the pupil. Light enters the eye through it.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   41 | Hold |  |  |  |  |  | Pause, Help, Settings | 0.8 s inspection hold |
| 42 | Offer |  |  |  |  | topic:external_structures | Answer, Hint/Show me, Skip/Continue anyway | Activity finished or skipped |
| 43 | Shot | Enlarged complete eye · medium | S02_EyeHero → S02_EyeHero (-0.30,1.70,-2.60 · yaw 1 pitch 6 · 36°) | Ease-in-out blend 1 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1 s; short fade when reduced motion) |
| 44 | Do |  |  |  |  | niko(Present) | Pause, Help, Settings | Action complete |
| 45 | Say |  |  |  | S02_L15: “We’ve seen the parts we can see from the outside. Now, let’s look inside.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 46 | Say |  |  |  | S02_L16 [PROMPT VO]: “Click on the Inside View button to see how the eye looks from the inside.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 47 | Await |  |  |  |  | inside | The awaited control (click/tap); orbit/zoom; Pause; Ask Niko | Learner action 'inside' |
| 48 | Do |  |  |  |  | sfx(unlock) | Pause, Help, Settings | Action complete |

## Scene03 — Inside the Eye

Checkpoint (restart/resume state): Closed eye at the hero point in the Scene 2 orientation; Niko beside it; camera S02_EyeHero.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Do |  |  |  |  | corneaClear | Pause, Help, Settings | Action complete |
| 2 | Shot | Complete cutaway eye · medium | checkpoint → S03_InsideHero (-0.25,1.70,-2.50 · yaw 1 pitch 6 · 36°) | Ease-in-out blend 2.6 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 3 | Do |  |  |  |  | turnToSide | Pause, Help, Settings | Action complete |
| 4 | Do |  |  |  |  | cutaway(open) | Pause, Help, Settings | Action complete |
| 5 | Do |  |  |  |  | niko(Snap) | Pause, Help, Settings | Action complete |
| 6 | Do |  |  |  |  | sequence(aqueous,lens,vitreous,retina,choroid) | Pause, Help, Settings | Action complete |
| 7 | Say |  |  |  | S03_L01: “Click or ask Niko about each part to explore what it does.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 8 | Explore |  |  |  |  |  | Tap a highlighted part, its label or the Parts list; Ask Niko; orbit/zoom | Every listed part's explanation finished |
| | **Part: Aqueous humour** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   9 | Say |  |  |  | S03_L02: “This space between the cornea and the lens contains a clear fluid called aqueous humour.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   10 | Say |  |  |  | S03_L03: “It helps nourish the front parts of the eye and helps maintain pressure inside the eye.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   11 | Hold |  |  |  |  |  | Pause, Help, Settings | 0.6 s inspection hold |
| | **Part: Crystalline lens** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   12 | Say |  |  |  | S03_L04: “Behind the pupil is the eye’s natural crystalline lens.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   13 | Shot | Biconvex lens close-up · close-up | S03_InsideHero → S03_Lens_Close (0.73,1.50,0.01 · yaw 293 pitch 3 · 28°) | Ease-in-out blend 1.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.2 s; short fade when reduced motion) |
|   14 | Say |  |  |  | S03_L05: “It is transparent, flexible and biconvex, meaning it bulges outward on both sides.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   15 | Do |  |  |  |  | lensFlex | Pause, Help, Settings | Starts; runs alongside |
|   16 | Say |  |  |  | S03_L06: “It can change its shape to help focus light from objects at different distances.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   17 | Shot | Lens in position · medium | S03_Lens_Close → S03_Part_lens (0.90,1.55,-0.05 · yaw 293 pitch 3 · 30°) | Ease-in-out blend 1.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.2 s; short fade when reduced motion) |
| | **Part: Vitreous humour** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   18 | Shot | Through the vitreous toward the back · medium | S03_Part_lens → S03_Vitreous_Back (0.42,1.54,0.51 · yaw 322 pitch 5 · 34°) | Ease-in-out blend 2.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
|   19 | Say |  |  |  | S03_L07: “Most of the space behind the lens is filled with a clear, jelly-like substance called vitreous humour.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   20 | Say |  |  |  | S03_L08: “It helps maintain the shape of the eye.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| | **Part: Retina** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   21 | Say |  |  |  | S03_L09: “This inner layer at the back is the retina.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   22 | Shot | Retinal surface detail · close-up | S03_Vitreous_Back → S03_Retina_Close (0.52,1.42,0.55 · yaw 310 pitch 9 · 28°) | Ease-in-out blend 1.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.2 s; short fade when reduced motion) |
|   23 | Do |  |  |  |  | retinaDetail | Pause, Help, Settings | Action complete |
|   24 | Say |  |  |  | S03_L10: “It contains light-sensitive cells that detect light.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   25 | Shot | Retina → choroid → sclera layers · close-up | S03_Retina_Close → S03_Choroid_Deep (0.39,1.30,0.65 · yaw 305 pitch 8 · 24°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.4 s; short fade when reduced motion) |
|   26 | Hold |  |  |  |  |  | Pause, Help, Settings | 0.8 s inspection hold |
| | **Part: Choroid** | close-up (focus shot), others translucent | | | | isolate + highlight | | Explanation complete → return to complete eye |
|   27 | Shot | Retina → choroid → sclera layers · close-up | S03_Choroid_Deep → S03_Choroid_Deep (0.39,1.30,0.65 · yaw 305 pitch 8 · 24°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.4 s; short fade when reduced motion) |
|   28 | Say |  |  |  | S03_L11: “Behind the retina is the choroid, a layer rich in blood vessels that supplies the eye with oxygen and nutrients.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
|   29 | Hold |  |  |  |  |  | Pause, Help, Settings | 0.6 s inspection hold |
| 30 | Offer |  |  |  |  | topic:internal_structures | Answer, Hint/Show me, Skip/Continue anyway | Activity finished or skipped |
| 31 | Shot | Complete cutaway eye · medium | S03_Choroid_Deep → S03_InsideHero (-0.25,1.70,-2.50 · yaw 1 pitch 6 · 36°) | Ease-in-out blend 1.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.2 s; short fade when reduced motion) |
| 32 | Say |  |  |  | S03_L12: “Now we’ve explored the main structures of the eye.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |

## Scene04 — How Is the Eye Connected to the Brain?

Checkpoint (restart/resume state): Cutaway eye at the cut point; only the nerve stub visible; brain hidden; camera S03_InsideHero.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Shot | Around to the back of the eye · medium | checkpoint → S04_EyeRear (1.35,1.85,-0.85 · yaw 327 pitch 11 · 31°) | Ease-in-out blend 3 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 2 | Say |  |  |  | S04_L01: “But there’s something important we haven’t explored yet.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 3 | Shot | Back of the eye, closer · medium | S04_EyeRear → S04_EyeRearClose (1.00,1.72,-0.35 · yaw 329 pitch 14 · 29°) | Ease-in-out blend 2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 4 | Say |  |  |  | S04_L02: “We know that the retina detects light…” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 5 | Do |  |  |  |  | nerveReveal | Pause, Help, Settings | Starts; runs alongside |
| 6 | Say |  |  |  | S04_L03: “…but how does that information reach the brain?” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 7 | Do |  |  |  |  | glow(optic_nerve) | Pause, Help, Settings | Action complete |
| 8 | Say |  |  |  | S04_L04: “Let’s follow it.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 9 | Shot | Optic nerve leaving the eye · medium | S04_EyeRearClose → S04_NerveClose (0.61,1.77,-0.02 · yaw 351 pitch 20 · 30°) | Ease-in-out blend 1.6 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.6 s; short fade when reduced motion) |
| 10 | Say |  |  |  | S04_L05: “This bundle is the optic nerve.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 11 | Do |  |  |  |  | brainReveal | Pause, Help, Settings | Starts; runs alongside |
| 12 | Shot | Following the nerve to the brain · medium | S04_NerveClose → S04_NerveMid (1.48,1.67,-0.22 · yaw 348 pitch 14 · 34°) | Ease-in-out blend 2.6 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 13 | Say |  |  |  | S04_L06: “It carries visual information from the retina to the brain.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 14 | Shot | Visual processing area (back of brain) · medium | S04_NerveMid → S04_BrainClose (3.30,2.18,-0.95 · yaw 328 pitch 13 · 36°) | Ease-in-out blend 2.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (2.2 s; short fade when reduced motion) |
| 15 | Do |  |  |  |  | glow(visual_area) | Pause, Help, Settings | Action complete |
| 16 | Say |  |  |  | S04_L07: “The brain receives and processes this information so that we can see.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 17 | Do |  |  |  |  | glow(optic_nerve,visual_area) | Pause, Help, Settings | Action complete |
| 18 | Shot | Eye, optic nerve and brain together · wide | S04_BrainClose → S04_Wide (1.20,2.15,-4.10 · yaw 0 pitch 7 · 38°) | Ease-in-out blend 2.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (2.4 s; short fade when reduced motion) |
| 19 | Say |  |  |  | S04_L08: “So the eye and brain are connected through the optic nerve.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 20 | Offer |  |  |  |  | item:nerve_tap | Answer, Hint/Show me, Skip/Continue anyway | Activity finished or skipped |
| 21 | Free |  |  |  |  | routeTrace | Abilities (SIMULATE/TRACE/FREEZE/REPLAY), view controls, Continue | Learner taps Continue |

## Scene05 — Light Enters the Eye

Checkpoint (restart/resume state): Cutaway eye, nerve and brain; no object yet; pupil at default size; brightness 50%; camera S05_ObjectWide.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Do |  |  |  |  | objectAppear | Pause, Help, Settings | Action complete |
| 2 | Say |  |  |  | S05_L01: “We've been looking at the eye. Now let’s follow what happens when you look at an object.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 3 | Do |  |  |  |  | lightTravel(1) | Pause, Help, Settings | Starts; runs alongside |
| 4 | Shot | Following light to the eye · medium | checkpoint → S05_FollowLight (-1.25,1.70,-1.70 · yaw 9 pitch 5 · 34°) | Ease-in-out blend 3 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 5 | Say |  |  |  | S05_L02: “Vision begins when light from an object reaches the eye.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 6 | Shot | Light bending at the cornea · close-up | S05_FollowLight → S05_Cornea (-0.46,1.60,-0.04 · yaw 7 pitch 9 · 26°) | Ease-in-out blend 1.6 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.6 s; short fade when reduced motion) |
| 7 | Do |  |  |  |  | glow(cornea) | Pause, Help, Settings | Action complete |
| 8 | Do |  |  |  |  | lightTravel(1.5) | Pause, Help, Settings | Starts; runs alongside |
| 9 | Say |  |  |  | S05_L03: “First, the light passes through the cornea, which bends it.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 10 | Do |  |  |  |  | glow(cornea) | Pause, Help, Settings | Action complete |
| 11 | Shot | Aqueous humour and pupil · close-up | S05_Cornea → S05_AqueousPupil (-0.38,1.55,0.12 · yaw 6 pitch 6 · 23°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.4 s; short fade when reduced motion) |
| 12 | Do |  |  |  |  | glow(aqueous) | Pause, Help, Settings | Action complete |
| 13 | Do |  |  |  |  | lightTravel(2) | Pause, Help, Settings | Starts; runs alongside |
| 14 | Say |  |  |  | S05_L04: “It then travels through the aqueous humour and enters through the pupil.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 15 | Do |  |  |  |  | glow(aqueous) | Pause, Help, Settings | Action complete |
| 16 | Do |  |  |  |  | pupilView | Pause, Help, Settings | Action complete |
| 17 | Do |  |  |  |  | brightnessLive | Pause, Help, Settings | Action complete |
| 18 | Say |  |  |  | S05_L05 [PROMPT VO]: “Change the brightness and observe what happens to the pupil.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 19 | Await |  |  |  |  | brighter | The awaited control (click/tap); orbit/zoom; Pause; Ask Niko | Learner action 'brighter' |
| 20 | Do |  |  |  |  | pupilSettle | Pause, Help, Settings | Action complete |
| 21 | Say |  |  |  | S05_L06: “In bright light, the pupil becomes smaller.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 22 | Await |  |  |  |  | dimmer | The awaited control (click/tap); orbit/zoom; Pause; Ask Niko | Learner action 'dimmer' |
| 23 | Do |  |  |  |  | pupilSettle | Pause, Help, Settings | Action complete |
| 24 | Say |  |  |  | S05_L07: “In dimmer light, the pupil becomes larger.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 25 | Do |  |  |  |  | glow(iris) | Pause, Help, Settings | Action complete |
| 26 | Say |  |  |  | S05_L08: “The iris controls the size of the pupil and therefore regulates how much light enters the eye.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 27 | Do |  |  |  |  | glow(iris) | Pause, Help, Settings | Action complete |
| 28 | Offer |  |  |  |  | item:pupil_predict | Answer, Hint/Show me, Skip/Continue anyway | Activity finished or skipped |
| 29 | Free |  |  |  |  | brightness | Abilities (SIMULATE/TRACE/FREEZE/REPLAY), view controls, Continue | Learner taps Continue |

## Scene06 — The Lens Focuses the Light

Checkpoint (restart/resume state): Cutaway eye; apple at medium distance with the lens matched; light reaching the pupil; camera S05_AqueousPupil.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Shot | Rays through the lens · medium | checkpoint → S06_LensClose (-0.22,1.48,-0.44 · yaw 1 pitch 4 · 30°) | Ease-in-out blend 2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (2 s; short fade when reduced motion) |
| 2 | Do |  |  |  |  | glow(lens) | Pause, Help, Settings | Action complete |
| 3 | Do |  |  |  |  | lightTravel(4) | Pause, Help, Settings | Starts; runs alongside |
| 4 | Say |  |  |  | S06_L01: “After passing through the pupil, the light reaches the eye’s crystalline lens.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 5 | Do |  |  |  |  | glow(lens) | Pause, Help, Settings | Action complete |
| 6 | Do |  |  |  |  | distanceControls | Pause, Help, Settings | Action complete |
| 7 | Await |  |  |  |  | near | The awaited control (click/tap); orbit/zoom; Pause; Ask Niko | Learner action 'near' |
| 8 | Shot | Near object and eye · medium | S06_LensClose → S06_NearWide (-1.05,1.75,-2.40 · yaw 2 pitch 5 · 34°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 9 | Do |  |  |  |  | nearDemo | Pause, Help, Settings | Action complete |
| 10 | Shot | Rays through the lens · medium | S06_NearWide → S06_LensClose (-0.22,1.48,-0.44 · yaw 1 pitch 4 · 30°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.4 s; short fade when reduced motion) |
| 11 | Say |  |  |  | S06_L02: “For a nearby object, the lens becomes more curved so that the light can be focused correctly.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 12 | Await |  |  |  |  | distant | The awaited control (click/tap); orbit/zoom; Pause; Ask Niko | Learner action 'distant' |
| 13 | Shot | Distant object beyond the eye · wide | S06_LensClose → S06_FarView (1.20,1.75,-0.95 · yaw 284 pitch 2 · 40°) | Ease-in-out blend 1.6 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 14 | Do |  |  |  |  | distantDemo | Pause, Help, Settings | Action complete |
| 15 | Hold |  |  |  |  |  | Pause, Help, Settings | 1 s inspection hold |
| 16 | Shot | Rays through the lens · medium | S06_FarView → S06_LensClose (-0.22,1.48,-0.44 · yaw 1 pitch 4 · 30°) | Ease-in-out blend 1.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.4 s; short fade when reduced motion) |
| 17 | Say |  |  |  | S06_L03: “For a distant object, the lens becomes less curved.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 18 | Say |  |  |  | S06_L04: “This continuous adjustment of the lens to focus objects at different distances is called accommodation.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 19 | Shot | Light continuing through the vitreous · medium | S06_LensClose → S06_Vitreous (-0.04,1.60,-0.76 · yaw 2 pitch 4 · 34°) | Ease-in-out blend 2.2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (2.2 s; short fade when reduced motion) |
| 20 | Do |  |  |  |  | lightTravel(5) | Pause, Help, Settings | Action complete |
| 21 | Offer |  |  |  |  | topic:accommodation | Answer, Hint/Show me, Skip/Continue anyway | Activity finished or skipped |
| 22 | Free |  |  |  |  | focus | Abilities (SIMULATE/TRACE/FREEZE/REPLAY), view controls, Continue | Learner taps Continue |

## Scene07 — Image Forms on the Retina

Checkpoint (restart/resume state): Cutaway eye; apple at 6 eye-radii with the lens matched; light through the lens; no image yet; camera S06_Vitreous.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Shot | Toward the back of the eye · medium | checkpoint → S07_Vitreous (0.04,1.56,-0.20 · yaw 10 pitch 4 · 30°) | Ease-in-out blend 2.5 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 2 | Do |  |  |  |  | lightTravel(5) | Pause, Help, Settings | Action complete |
| 3 | Say |  |  |  | S07_L01: “After passing through the lens and vitreous humour, the light reaches the retina.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 4 | Offer |  |  |  |  | item:img_predict | Answer, Hint/Show me, Skip/Continue anyway | Activity finished or skipped |
| 5 | Do |  |  |  |  | imageForm | Pause, Help, Settings | Action complete |
| 6 | Shot | Rays converging; inverted image · close-up | S07_Vitreous → S07_Retina (0.14,1.52,-0.16 · yaw 13 pitch 1 · 28°) | Ease-in-out blend 1.6 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.6 s; short fade when reduced motion) |
| 7 | Do |  |  |  |  | imageInset | Pause, Help, Settings | Action complete |
| 8 | Say |  |  |  | S07_L02: “The lens forms a real, inverted image of the object on the retina.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 9 | Free |  |  |  |  | inspectImage | Abilities (SIMULATE/TRACE/FREEZE/REPLAY), view controls, Continue | Learner taps Continue |
| 10 | Shot | Retinal surface · close-up | S07_Retina → S07_RetinaCloser (0.22,1.51,0.10 · yaw 13 pitch 1 · 24°) | Ease-in-out blend 1.8 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.8 s; short fade when reduced motion) |
| 11 | Say |  |  |  | S07_L03: “But what exactly detects this light on the retina?” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |

## Scene08 — Photoreceptors Detect Light

Checkpoint (restart/resume state): Image on the retina (end of Scene 7); magnified retina prepared with light and signals off.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Do |  |  |  |  | intoRetina | Pause, Help, Settings | Action complete |
| 2 | Do |  |  |  |  | labels(scan) | Pause, Help, Settings | Action complete |
| 3 | Say |  |  |  | S08_L01: “The retina contains light-sensitive cells called photoreceptors.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 4 | Do |  |  |  |  | glow(rods) | Pause, Help, Settings | Action complete |
| 5 | Shot | Rods · medium | checkpoint → S08_Rods (58.40,0.95,-3.10 · yaw 0 pitch 6 · 34°) | Ease-in-out blend 1.6 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.6 s; short fade when reduced motion) |
| 6 | Say |  |  |  | S08_L02: “Rods are especially sensitive to low levels of light and help us see in dim conditions.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 7 | Say |  |  |  | S08_L03: “They mainly provide information about brightness rather than colour.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 8 | Do |  |  |  |  | glow(rods) | Pause, Help, Settings | Action complete |
| 9 | Do |  |  |  |  | glow(cones) | Pause, Help, Settings | Action complete |
| 10 | Shot | Cones · medium | S08_Rods → S08_Cones (61.20,0.95,-3.10 · yaw 0 pitch 7 · 34°) | Ease-in-out blend 1.6 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.6 s; short fade when reduced motion) |
| 11 | Say |  |  |  | S08_L04: “Cones work best in brighter light and allow us to see colours and fine details.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 12 | Do |  |  |  |  | glow(cones) | Pause, Help, Settings | Action complete |
| 13 | Shot | Magnified retina: photoreceptors · wide | S08_Cones → S08_MicroWide (60.40,2.10,-7.40 · yaw 0 pitch 5 · 42°) | Ease-in-out blend 1.6 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.6 s; short fade when reduced motion) |
| 14 | Do |  |  |  |  | microLight(0.6) | Pause, Help, Settings | Action complete |
| 15 | Hold |  |  |  |  |  | Pause, Help, Settings | 1.2 s inspection hold |
| 16 | Say |  |  |  | S08_L05: “When light reaches these cells, it is converted into electrical signals.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 17 | Do |  |  |  |  | microSignals | Pause, Help, Settings | Action complete |
| 18 | Shot | Signals through retinal cells · wide | S08_MicroWide → S08_Signals (62.30,2.60,-4.70 · yaw 0 pitch 8 · 38°) | Ease-in-out blend 2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Starts; continues during next steps |
| 19 | Say |  |  |  | S08_L06: “These signals are passed through the retina toward the optic nerve.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 20 | Shot | Photoreceptors, retina and optic nerve together · wide | S08_Signals → S08_PullBack (61.00,2.00,-10.40 · yaw 0 pitch 4 · 48°) | Ease-in-out blend 2.5 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (2.5 s; short fade when reduced motion) |
| 21 | Hold |  |  |  |  |  | Pause, Help, Settings | 1.5 s inspection hold |
| 22 | Offer |  |  |  |  | topic:rods_cones | Answer, Hint/Show me, Skip/Continue anyway | Activity finished or skipped |
| 23 | Free |  |  |  |  | photoreceptors | Abilities (SIMULATE/TRACE/FREEZE/REPLAY), view controls, Continue | Learner taps Continue |

## Scene09 — Signals Travel to the Brain

Checkpoint (restart/resume state): Cutaway eye, full optic nerve and brain; apple placed at medium distance; camera on the back of the eye.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Do |  |  |  |  | signals(nerve) | Pause, Help, Settings | Action complete |
| 2 | Say |  |  |  | S09_L01: “The electrical signals leave the eye through the optic nerve.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 3 | Do |  |  |  |  | followSignal(nerve) | Pause, Help, Settings | Starts; runs alongside |
| 4 | Say |  |  |  | S09_L02: “The optic nerve carries this visual information to the brain.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 5 | Shot | Eye, nerve and brain (Scene 4 continuity) · wide | checkpoint → S09_Wide (1.20,2.15,-4.10 · yaw 0 pitch 7 · 38°) | Ease-in-out blend 1.8 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (1.8 s; short fade when reduced motion) |
| 6 | Offer |  |  |  |  | item:brain_predict | Answer, Hint/Show me, Skip/Continue anyway | Activity finished or skipped |
| 7 | Shot | Visual processing area (back of brain) · medium | S09_Wide → S04_BrainClose (3.30,2.18,-0.95 · yaw 328 pitch 13 · 36°) | Ease-in-out blend 2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (2 s; short fade when reduced motion) |
| 8 | Do |  |  |  |  | glow(visual_area) | Pause, Help, Settings | Action complete |
| 9 | Say |  |  |  | S09_L03: “The brain receives and processes these signals to produce our visual perception.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 10 | Do |  |  |  |  | signals(nerve) | Pause, Help, Settings | Action complete |
| 11 | Do |  |  |  |  | perception | Pause, Help, Settings | Action complete |
| 12 | Say |  |  |  | S09_L04: “Although the image formed on the retina is inverted, our visual experience is not simply a photograph of that retinal image. The brain processes the information carried by the signals.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 13 | Do |  |  |  |  | glow(visual_area) | Pause, Help, Settings | Action complete |
| 14 | Shot | Eye, nerve and brain (Scene 4 continuity) · wide | S04_BrainClose → S09_Wide (1.20,2.15,-4.10 · yaw 0 pitch 7 · 38°) | Ease-in-out blend 2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (2 s; short fade when reduced motion) |
| 15 | Offer |  |  |  |  | topic:optic_nerve | Answer, Hint/Show me, Skip/Continue anyway | Activity finished or skipped |
| 16 | Free |  |  |  |  | signals | Abilities (SIMULATE/TRACE/FREEZE/REPLAY), view controls, Continue | Learner taps Continue |

## Scene10 — The Complete Working

Checkpoint (restart/resume state): Complete scene: apple, light path to the retina, eye, optic nerve and brain; camera S09_Wide.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Shot | Complete eye · medium | checkpoint → S10_Complete (-0.20,1.70,-2.65 · yaw 4 pitch 4 · 34°) | Ease-in-out blend 2.4 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (2.4 s; short fade when reduced motion) |
| 2 | Say |  |  |  | S10_L01: “So seeing is a complete process.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 3 | Say |  |  |  | S10_L02: “Now you’ve followed the journey from the moment light enters the eye to the moment the brain processes the information.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 4 | Shot | Whole pathway: object → eye → nerve → brain · wide | S10_Complete → S10_Journey (-0.30,2.35,-6.10 · yaw 0 pitch 7 · 46°) | Ease-in-out blend 2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (2 s; short fade when reduced motion) |
| 5 | Do |  |  |  |  | journey | Pause, Help, Settings | Action complete |
| 6 | Offer |  |  |  |  | item:rebuild_journey | Answer, Hint/Show me, Skip/Continue anyway | Activity finished or skipped |
| 7 | Offer |  |  |  |  | topic:inverted_image | Answer, Hint/Show me, Skip/Continue anyway | Activity finished or skipped |
| 8 | Free |  |  |  |  | summary | Abilities (SIMULATE/TRACE/FREEZE/REPLAY), view controls, Continue | Learner taps Continue |

## Scene11 — Final Curiosity

Checkpoint (restart/resume state): Cutaway eye closing to the complete eye; near apple and a distant tree placed; no blur; camera S10_Complete.

| # | Step | Subject · framing | Start → end camera pose | Transition | Dialogue | Animation / visual cue | Permitted input | Exit condition |
|---|---|---|---|---|---|---|---|---|
| 1 | Do |  |  |  |  | closeEye | Pause, Help, Settings | Action complete |
| 2 | Shot | Complete eye · medium | checkpoint → S11_Eye (0.10,1.60,-2.00 · yaw 358 pitch 2 · 30°) | Ease-in-out blend 2 s (reduced motion: fade cut) |  |  | Pause, Help, Settings | Blend complete (2 s; short fade when reduced motion) |
| 3 | Say |  |  |  | S11_L01: “Your eyes are constantly responding to the world around you—even when you’re not thinking about it.” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 4 | Say |  |  |  | S11_L02: “So here’s something to think about…what happens when the eye cannot focus light properly?” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 5 | Do |  |  |  |  | blurDemo | Pause, Help, Settings | Action complete |
| 6 | Say |  |  |  | S11_L03: “Why might some things look blurry, even when your eyes are working?” |  | Pause, Help, Settings, Ask Niko (held to the end of the line) | Clip finished (or explicit Read & Continue when audio is missing) |
| 7 | Do |  |  |  |  | fadeBlurred | Pause, Help, Settings | Action complete |
| 8 | Do |  |  |  |  | eyeFade | Pause, Help, Settings | Action complete |
| 9 | Do |  |  |  |  | cutToBlack | Pause, Help, Settings | Action complete |
| 10 | Hold |  |  |  |  |  | Pause, Help, Settings | 2.5 s inspection hold |

