"""Builds the authoritative 66-cue dialogue ledger from HUMAN EYE.docx.

Text is taken verbatim from the storyboard paragraphs (only the speaker label and the
enclosing quotation marks are removed) and checked against the implementation
transcription supplied with the brief. Outputs:
  Assets/HumanEye/Data/DialogueLedger.json   (runtime/editor source of truth)
  Docs/HumanEye/VO/HumanEye_VO.csv            (filename,text — recording script)
  Docs/HumanEye/VO/VO_MANIFEST.csv            (cue id -> filename mapping)
  Docs/HumanEye/VO/LEDGER_CHECK.md            (docx vs transcription report)
Usage: python Tools/HumanEye/extract_ledger.py [path/to/HUMAN EYE.docx]
"""
import csv, json, os, re, sys, zipfile
import xml.etree.ElementTree as ET

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
W = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"

# (cue id, docx paragraph index, type, transcription from the brief, visual cue, completion event)
CUES = [
 ("S02_L01", 74, "Core", "Hey! I’m Niko. Your Vision Investigator Officer.", "Niko materialises and waves; medium shot", "audio_complete"),
 ("S02_L02", 75, "Core", "So you want to know how light becomes sight?", "Niko gestures toward the starlight beam", "audio_complete"),
 ("S02_L03", 76, "Core", "Then watching from out here won't help.", "Niko turns toward the subdued display; human figure appears after this line", "audio_complete"),
 ("S02_L04", 79, "Core", "We need to go inside somewhere humans can't.", "Wide: Niko and the revealed human", "audio_complete"),
 ("S02_L05", 81, "PromptVO", "Click on the SCAN button.", "Niko raises his hand; SCAN button pulses", "audio_complete_then_scan_pressed"),
 ("S02_L06", 90, "Core", "There.", "One eye illuminates as the line starts", "audio_complete"),
 ("S02_L07", 93, "Core", "Your first destination.", "Anatomical close-up of the eye inside the skull", "audio_complete"),
 ("S02_L08", 101, "Core", "Here it is. The human eye.", "Enlarged eye in front of learner, slow rotation", "audio_complete"),
 ("S02_L09", 104, "Core", "It has several parts, and each one has an important job.", "Niko gestures toward the eye", "audio_complete"),
 ("S02_L10", 107, "Core", "Click or ask Niko about each part to explore what it does.", "After Niko's snap: sclera, cornea, iris, pupil highlight in turn", "audio_complete_then_exploration"),
 ("S02_L11", 117, "Core", "This tough white outer layer is called the sclera. It protects the eye and helps maintain its shape.", "Close-up of sclera; other parts translucent", "audio_complete_marks_part"),
 ("S02_L12", 125, "Core", "This transparent, curved layer at the front is the cornea. It protects the eye and bends incoming light.", "Close-up of cornea profile; other parts translucent", "audio_complete_marks_part"),
 ("S02_L13", 133, "Core", "This coloured part is the iris. It controls the size of the pupil.", "Front close-up of iris; other parts translucent", "audio_complete_marks_part"),
 ("S02_L14", 139, "Core", "This opening is the pupil. Light enters the eye through it.", "Pupil rim highlighted as an opening; camera pulls back", "audio_complete_marks_part"),
 ("S02_L15", 143, "Core", "We’ve seen the parts we can see from the outside. Now, let’s look inside.", "Complete eye; Niko gestures", "audio_complete"),
 ("S02_L16", 144, "PromptVO", "Click on the Inside View button to see how the eye looks from the inside.", "Inside View button enabled", "audio_complete_then_inside_pressed"),
 ("S03_L01", 150, "Core", "Click or ask Niko about each part to explore what it does.", "Cutaway eye; internal parts highlight in turn", "audio_complete_then_exploration"),
 ("S03_L02", 161, "Core", "This space between the cornea and the lens contains a clear fluid called aqueous humour.", "Aqueous region highlighted, camera closer", "audio_complete"),
 ("S03_L03", 163, "Core", "It helps nourish the front parts of the eye and helps maintain pressure inside the eye.", "Hold on aqueous region", "audio_complete_marks_part"),
 ("S03_L04", 169, "Core", "Behind the pupil is the eye’s natural crystalline lens.", "Lens highlighted, camera closer", "audio_complete"),
 ("S03_L05", 172, "Core", "It is transparent, flexible and biconvex, meaning it bulges outward on both sides.", "Lens close-up, biconvex profile", "audio_complete"),
 ("S03_L06", 175, "Core", "It can change its shape to help focus light from objects at different distances.", "Lens changes shape slightly; view widens", "audio_complete_marks_part"),
 ("S03_L07", 182, "Core", "Most of the space behind the lens is filled with a clear, jelly-like substance called vitreous humour.", "Camera moves through vitreous toward the back", "audio_complete"),
 ("S03_L08", 184, "Core", "It helps maintain the shape of the eye.", "Hold on vitreous region", "audio_complete_marks_part"),
 ("S03_L09", 190, "Core", "This inner layer at the back is the retina.", "Camera very close to retinal surface", "audio_complete"),
 ("S03_L10", 193, "Core", "It contains light-sensitive cells that detect light.", "Retinal detail; camera moves slightly deeper", "audio_complete_marks_part"),
 ("S03_L11", 199, "Core", "Behind the retina is the choroid, a layer rich in blood vessels that supplies the eye with oxygen and nutrients.", "Choroid highlighted behind retina", "audio_complete_marks_part"),
 ("S03_L12", 202, "Core", "Now we’ve explored the main structures of the eye.", "Complete cutaway eye", "audio_complete"),
 ("S04_L01", 209, "Core", "But there’s something important we haven’t explored yet.", "Camera begins moving toward the back of the eye", "audio_complete"),
 ("S04_L02", 212, "Core", "We know that the retina detects light…", "Back of the eye comes closer", "audio_complete"),
 ("S04_L03", 215, "Core", "…but how does that information reach the brain?", "Nerve bundle extends into view", "audio_complete"),
 ("S04_L04", 218, "Core", "Let’s follow it.", "Bundle highlighted", "audio_complete"),
 ("S04_L05", 224, "Core", "This bundle is the optic nerve.", "Close on optic nerve leaving the eye", "audio_complete"),
 ("S04_L06", 227, "Core", "It carries visual information from the retina to the brain.", "Camera follows nerve toward the brain", "audio_complete"),
 ("S04_L07", 231, "Core", "The brain receives and processes this information so that we can see.", "Visual processing area at back of brain highlighted", "audio_complete"),
 ("S04_L08", 234, "Core", "So the eye and brain are connected through the optic nerve.", "Wide: eye, optic nerve and brain together", "audio_complete"),
 ("S05_L01", 242, "Core", "We've been looking at the eye. Now let’s follow what happens when you look at an object.", "Illuminated apple and eye", "audio_complete"),
 ("S05_L02", 245, "Core", "Vision begins when light from an object reaches the eye.", "Light travels from the object to the eye", "audio_complete"),
 ("S05_L03", 248, "Core", "First, the light passes through the cornea, which bends it.", "Rays bend at the cornea", "audio_complete"),
 ("S05_L04", 251, "Core", "It then travels through the aqueous humour and enters through the pupil.", "Rays through aqueous and pupil", "audio_complete"),
 ("S05_L05", 256, "PromptVO", "Change the brightness and observe what happens to the pupil.", "Brightness controls enabled", "audio_complete_then_brightness"),
 ("S05_L06", 260, "Core", "In bright light, the pupil becomes smaller.", "Pupil has constricted", "audio_complete"),
 ("S05_L07", 264, "Core", "In dimmer light, the pupil becomes larger.", "Pupil has dilated", "audio_complete"),
 ("S05_L08", 266, "Core", "The iris controls the size of the pupil and therefore regulates how much light enters the eye.", "Iris highlighted", "audio_complete"),
 ("S06_L01", 273, "Core", "After passing through the pupil, the light reaches the eye’s crystalline lens.", "Lens close-up; rays bend through lens", "audio_complete"),
 ("S06_L02", 280, "Core", "For a nearby object, the lens becomes more curved so that the light can be focused correctly.", "Near object; lens more curved", "audio_complete"),
 ("S06_L03", 285, "Core", "For a distant object, the lens becomes less curved.", "Distant object; lens less curved", "audio_complete"),
 ("S06_L04", 287, "Core", "This continuous adjustment of the lens to focus objects at different distances is called accommodation.", "Pull back: light continues through vitreous", "audio_complete"),
 ("S07_L01", 294, "Core", "After passing through the lens and vitreous humour, the light reaches the retina.", "Rays converge on the retina", "audio_complete"),
 ("S07_L02", 297, "Core", "The lens forms a real, inverted image of the object on the retina.", "Inverted retinal image shown clearly", "audio_complete"),
 ("S07_L03", 300, "Core", "But what exactly detects this light on the retina?", "Camera closer to retinal surface", "audio_complete"),
 ("S08_L01", 305, "Core", "The retina contains light-sensitive cells called photoreceptors.", "Magnified retina; photoreceptors visible", "audio_complete"),
 ("S08_L02", 310, "Core", "Rods are especially sensitive to low levels of light and help us see in dim conditions.", "Rods highlighted", "audio_complete"),
 ("S08_L03", 312, "Core", "They mainly provide information about brightness rather than colour.", "Rods remain highlighted", "audio_complete"),
 ("S08_L04", 317, "Core", "Cones work best in brighter light and allow us to see colours and fine details.", "Cones highlighted", "audio_complete"),
 ("S08_L05", 322, "Core", "When light reaches these cells, it is converted into electrical signals.", "Light reaches cells; electrical response", "audio_complete"),
 ("S08_L06", 325, "Core", "These signals are passed through the retina toward the optic nerve.", "Signals pass through retinal cells toward optic nerve", "audio_complete"),
 ("S09_L01", 332, "Core", "The electrical signals leave the eye through the optic nerve.", "Signals leave retina into optic nerve", "audio_complete"),
 ("S09_L02", 335, "Core", "The optic nerve carries this visual information to the brain.", "Camera follows nerve to brain", "audio_complete"),
 ("S09_L03", 339, "Core", "The brain receives and processes these signals to produce our visual perception.", "Visual processing area highlighted", "audio_complete"),
 ("S09_L04", 342, "Core", "Although the image formed on the retina is inverted, our visual experience is not simply a photograph of that retinal image. The brain processes the information carried by the signals.", "Original object shown clearly; retinal image and perception as separate views", "audio_complete"),
 ("S10_L01", 347, "Core", "So seeing is a complete process.", "Pull back to complete eye", "audio_complete"),
 ("S10_L02", 348, "Core", "Now you’ve followed the journey from the moment light enters the eye to the moment the brain processes the information.", "Complete eye, nerve and brain", "audio_complete"),
 ("S11_L01", 351, "Core", "Your eyes are constantly responding to the world around you—even when you’re not thinking about it.", "Complete eye", "audio_complete"),
 ("S11_L02", 352, "Core", "So here’s something to think about…what happens when the eye cannot focus light properly?", "Then distant object slightly blurred, near object clear", "audio_complete"),
 ("S11_L03", 354, "Core", "Why might some things look blurry, even when your eyes are working?", "Blurred object fades; eye fades; cut to black", "audio_complete"),
]
EXPECTED_COUNTS = {"Scene02": 16, "Scene03": 12, "Scene04": 8, "Scene05": 8, "Scene06": 4,
                   "Scene07": 3, "Scene08": 6, "Scene09": 4, "Scene10": 2, "Scene11": 3}


def paragraphs(docx):
    body = ET.fromstring(zipfile.ZipFile(docx).read("word/document.xml")).find(W + "body")
    return ["".join(t.text or "" for t in p.iter(W + "t")) for p in body.iter(W + "p")]


def spoken(raw):
    text = raw.strip()
    text = re.sub(r"^(Niko:|Prompt and VO:|Prompt \+ VO:)\s*", "", text)
    if len(text) >= 2 and text[0] in "“\"" and text[-1] in "”\"":
        text = text[1:-1]
    return text.strip()


def filename(seq, text):
    words = [w for w in (re.sub(r"[^\w]", "", w.lower()) for w in text.split()) if w]
    return f"{seq:02d}_" + "_".join(words[:4])


def segments(text):
    if len(text) <= 90:
        return [text]
    return [s for s in re.split(r"(?<=[.?!])\s+", text) if s]


def main():
    docx = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Docs", "Storyboard", "HUMAN EYE.docx")
    paras = paragraphs(docx)
    ledger, report = [], []
    for seq, (cid, para, kind, brief, visual, done) in enumerate(CUES, 1):
        text = spoken(paras[para])
        if text != brief:
            diff = [(i, a, b) for i, (a, b) in enumerate(zip(text, brief)) if a != b][:5]
            report.append(f"- {cid}: docx `{text}` vs brief `{brief}` (first differences {diff}, len {len(text)}/{len(brief)})")
        ledger.append({
            "id": cid, "scene": "Scene" + cid[1:3], "sequence": seq, "speaker": "Niko",
            "type": kind, "layer": "StoryboardCore", "text": text, "filename": filename(seq, text),
            "audio": f"Assets/HumanEye/Audio/VO/{filename(seq, text)}.wav",
            "segments": segments(text), "visualCue": visual, "completionEvent": done,
            "docxParagraph": para,
        })
    counts = {}
    for c in ledger:
        counts[c["scene"]] = counts.get(c["scene"], 0) + 1
    assert counts == EXPECTED_COUNTS, counts
    assert len({c["id"] for c in ledger}) == 66 and len({c["filename"] for c in ledger}) == 66
    assert sum(c["type"] == "PromptVO" for c in ledger) == 3
    out = os.path.join(ROOT, "Assets", "HumanEye", "Data", "DialogueLedger.json")
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        json.dump({"source": "Docs/Storyboard/HUMAN EYE.docx", "cues": ledger}, f, ensure_ascii=False, indent=1)
    vo = os.path.join(ROOT, "Docs", "HumanEye", "VO")
    os.makedirs(vo, exist_ok=True)
    with open(os.path.join(vo, "HumanEye_VO.csv"), "w", encoding="utf-8", newline="") as f:
        w = csv.writer(f, lineterminator="\n")
        w.writerow(["filename", "text"])
        w.writerows([c["filename"], c["text"]] for c in ledger)
    with open(os.path.join(vo, "VO_MANIFEST.csv"), "w", encoding="utf-8", newline="") as f:
        w = csv.writer(f, lineterminator="\n")
        w.writerow(["cue_id", "scene", "sequence", "speaker", "type", "filename", "audio_path"])
        w.writerows([c["id"], c["scene"], c["sequence"], c["speaker"], c["type"], c["filename"], c["audio"]] for c in ledger)
    with open(os.path.join(vo, "LEDGER_CHECK.md"), "w", encoding="utf-8", newline="\n") as f:
        f.write("# Ledger check: HUMAN EYE.docx vs brief transcription\n\n")
        f.write(f"66 cues extracted; per-scene counts {counts}; 3 PROMPT VO cues.\n\n")
        f.write("Differences (docx text is canonical):\n\n" + ("\n".join(report) if report else "- None. All 66 cues match the transcription character-for-character.") + "\n")
    print("cues", len(ledger), "differences", len(report))
    print("\n".join(report))


if __name__ == "__main__":
    main()
