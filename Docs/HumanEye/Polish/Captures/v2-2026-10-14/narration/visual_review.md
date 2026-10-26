# Recorded narration visibility review

Reviewed all 204 samples across contact_01.jpg through contact_14.jpg on 14 October 2026.
Source: the complete 446.5-second walkthrough in `Builds/Checks/v2-20261014/full-run/`.
This recording precedes the owner's final waist-up opening correction. That correction changes the unvoiced opening;
the narrated lesson covered here is unchanged. A separate final opening recording is linked in the main report.

- 65 core cues × opening/middle/ending = 195 frames.
- Two replayed cues × three moments = six frames.
- Supplemental closing × three moments = three frames.
- Niko visibly recognizable: **YES in all 204 sampled frames**. Main stage for S02_L01–L04; live upper-left PiP
  for the other ordinary/wide-route samples; upper-right PiP for S09_L02/L03 head/brain shots.
- Face/eyes remain unobscured. Gestures and expression vary between sampled frames, consistent with the live rig;
  these stills alone do not establish phoneme lip sync or every intervening rendered frame.
- Subtitles and activity controls remain outside the presenter panel. At extreme close-ups, PiP overlays the peripheral
  background/model edge but does not cover the central explained structure in the reviewed samples.

S02_L06 is unusually short. Its ending runtime sample was missed between audio-position updates. The recovered frame
is at video 43.70 s (log 44.15 s), `sample_015.jpg`: **Niko visible YES, upper-left PiP, eyes/face and raised arm clear**.
The raw CSV preserves `visible=REVIEW_FRAME` and `mode=SEE_FRAME`; this is a visual confirmation, not a fabricated
runtime sample. Full-frame checks also included S09_L03's brain close-up and the supplemental closing.

The extraction tool accounts for the 0.45-second delay between log start and recording start. `samples.csv` contains
both clocks. Contact-sheet headings use video time, with approximately one-frame/rounded-log precision.
Runtime state coverage separately reports 14,789 narrated frames and zero failures; sampled visual review cannot
prove absence of a transient visual occlusion between samples. The 1024×768 stress run adds 2,755 narrated frames
with zero runtime presenter failures.

Contact sheets: [01](contact_01.jpg), [02](contact_02.jpg), [03](contact_03.jpg), [04](contact_04.jpg),
[05](contact_05.jpg), [06](contact_06.jpg), [07](contact_07.jpg), [08](contact_08.jpg), [09](contact_09.jpg),
[10](contact_10.jpg), [11](contact_11.jpg), [12](contact_12.jpg), [13](contact_13.jpg), [14](contact_14.jpg).
Raw sample metadata: [CSV](samples.csv), [summary](summary.json).
