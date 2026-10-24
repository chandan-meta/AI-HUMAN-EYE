"""Extract actual recorded narration samples and contact sheets; does not infer visibility from pixels."""
import argparse
import csv
import json
import re
import shutil
import subprocess
from pathlib import Path

from PIL import Image, ImageDraw


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("folder", type=Path, help="AutoPilot folder with walkthrough.log and video.mp4")
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    log = (args.folder / "walkthrough.log").read_text(encoding="utf-8-sig")
    recording = re.search(r"([\d.]+)s\s+record\s+\d+x\d+ at (\d+) fps ->", log)
    assert recording, "No recording start timestamp in the log"
    record_start, fps = float(recording[1]), int(recording[2])
    pattern = r"([\d.]+)s\s+presenter_sample\s+(\S+) (\S+) share=([\d.]+) visible=(\w+) mode=(\w+) eyeScale=([\d.]+) gesture=(\S+)"
    rows = [dict(zip(("time", "cue", "source", "share", "visible", "mode", "eyeScale", "gesture"), m))
            for m in re.findall(pattern, log)]
    assert rows, "No narration samples in the log"
    # A very short clip can end between audio-position samples. Recover its final recorded frame for visual review.
    ends = [(float(t), cue) for t, cue in re.findall(r"([\d.]+)s\s+cue_end\s+(\S+)", log)]
    recovered = 0
    for start, cue, source in re.findall(r"([\d.]+)s\s+cue_start\s+(\S+) \[(\w+)\]", log):
        start = float(start)
        end = next((t for t, c in ends if c == cue and t > start), None)
        if end is None:
            continue
        for share in (.02, .5, .94):
            if any(r['cue'] == cue and r['source'] == source and start <= float(r['time']) < end
                   and abs(float(r['share']) - share) < .001 for r in rows):
                continue
            at = max(start, min(end - .07, start + (end - start) * share))
            rows.append(dict(time=f'{at:.2f}', cue=cue, source=source, share=f'{share:.2f}',
                             visible='REVIEW_FRAME', mode='SEE_FRAME', eyeScale='-', gesture='-'))
            recovered += 1
    rows.sort(key=lambda row: float(row['time']))
    for row in rows:
        row['videoTime'] = f"{max(0, float(row['time']) - record_start):.2f}"
    assert all(float(row['videoTime']) >= 0 for row in rows)
    args.output.mkdir(parents=True, exist_ok=True)
    indices = sorted({round(float(row['videoTime']) * fps) for row in rows})
    selection = "+".join("(" + "+".join(f"eq(n\\,{i})" for i in indices[start:start+16]) + ")"
                         for start in range(0, len(indices), 16))
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(args.folder / "video.mp4"),
                    "-vf", "select=" + selection, "-fps_mode", "vfr", "-q:v", "2",
                    str(args.output / "recorded_%03d.jpg")], check=True)
    frames = sorted(args.output.glob("recorded_*.jpg"))
    assert len(frames) == len(indices), f"Expected {len(indices)} frames, got {len(frames)}"
    by_index = dict(zip(indices, frames))
    for i, row in enumerate(rows, 1):
        row['frame'] = f'sample_{i:03}.jpg'
        shutil.copyfile(by_index[round(float(row['videoTime']) * fps)], args.output / row['frame'])
    with (args.output / "samples.csv").open("w", newline="", encoding="utf-8") as file:
        writer = csv.DictWriter(file, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)
    summary = {"samples": len(rows), "recordingStartLogSeconds": record_start, "fps": fps,
               "ledgerCues": len({r["cue"] for r in rows if r["source"] == "Core"}),
               "reportedNo": [r for r in rows if r["visible"] == "NO"],
               "recoveredFramesForVisualReview": recovered,
               "coverage": re.findall(r"presenter_coverage\s+(.+)", log),
               "performance": re.findall(r"\sperf\s+(.+)", log),
               "done": re.findall(r"\sdone\s+(.+)", log),
               "note": "YES is the runtime visibility state. Inspect these recorded frames for visual occlusion."}
    (args.output / "summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    for start in range(0, len(rows), 15):
        page = rows[start:start + 15]
        sheet = Image.new("RGB", (1200, ((len(page) + 2) // 3) * 256), "#0e1b28")
        draw = ImageDraw.Draw(sheet)
        for i, row in enumerate(page):
            x, y = (i % 3) * 400, (i // 3) * 256
            with Image.open(args.output / row["frame"]) as frame:
                frame.thumbnail((400, 225))
                sheet.paste(frame, (x, y + 26))
            draw.text((x + 5, y + 6), f"{row['cue']} {row['share']} | {row['mode']} | {row['videoTime']}s", fill="white")
        sheet.save(args.output / f"contact_{start // 15 + 1:02}.jpg", quality=93)
    print(json.dumps({k: v for k, v in summary.items() if k not in ("coverage", "performance")}, indent=2))


if __name__ == "__main__":
    main()
