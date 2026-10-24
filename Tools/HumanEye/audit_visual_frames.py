"""Decode every recorded frame, measure visual outliers, and make paged review sheets.

These thresholds identify review candidates; they do not judge artistic or anatomical quality.
Usage: py -3 Tools/HumanEye/audit_visual_frames.py video.mp4 --log walkthrough.log --output audit
"""
import argparse
import bisect
import csv
import json
import math
import re
import tempfile
from pathlib import Path

import imageio_ffmpeg
import numpy as np
from PIL import Image, ImageDraw

try:
    import cv2
except ImportError:
    cv2 = None


def read_log(path):
    events, transitions, offset = [], [], 0.0
    if not path:
        return events, transitions, offset
    for line in Path(path).read_text(encoding="utf-8-sig", errors="replace").splitlines():
        match = re.match(r"\s*([\d.]+)s\t([^\t]+)\t(.*)", line)
        if not match:
            continue
        at, kind, detail = float(match[1]), match[2], match[3]
        if kind == "record" and not any(e[1] == "record" for e in events):
            offset = at
        events.append((at, kind, detail))
        if kind == "chapter" or kind == "fade" or (kind == "state" and detail in ("Entering", "Cinematic")):
            transitions.append((at - .35, at + .65, f"{kind}: {detail}"))
    return events, transitions, offset


def measure(rgb, previous):
    if cv2 is not None:
        grey = cv2.cvtColor(rgb, cv2.COLOR_RGB2GRAY)
        small = cv2.resize(grey, (320, 180), interpolation=cv2.INTER_AREA)
        sharpness = float(cv2.Laplacian(small, cv2.CV_32F).var())
    else:
        grey = np.asarray(Image.fromarray(rgb).convert("L"))
        small = np.asarray(Image.fromarray(grey).resize((320, 180), Image.Resampling.BOX))
        g = small.astype(np.float32)
        sharpness = float((np.diff(g, axis=0) ** 2).mean() + (np.diff(g, axis=1) ** 2).mean())
    delta = 0.0 if previous is None else float(np.abs(small.astype(np.float32) - previous).mean())
    return {
        "mean_luma": float(grey.mean()),
        "near_black_percent": float(np.count_nonzero(grey <= 5) * 100 / grey.size),
        "near_white_percent": float(np.count_nonzero(grey >= 250) * 100 / grey.size),
        "edge_energy": sharpness,
        "mean_frame_delta": delta,
    }, small.astype(np.float32)


def audit(video, log, output, interval=2.0):
    video, output = Path(video), Path(output)
    if not video.is_file():
        raise FileNotFoundError(video)
    if interval <= 0:
        raise ValueError("Contact-sheet interval must be positive.")
    output.mkdir(parents=True, exist_ok=True)
    sheets_dir, flags_dir = output / "contact-sheets", output / "flagged-frames"
    sheets_dir.mkdir(exist_ok=True)
    flags_dir.mkdir(exist_ok=True)
    events, transitions, offset = read_log(log)
    chapters = [(t, d) for t, k, d in events if k == "chapter"]
    chapter_times = [x[0] for x in chapters]
    frames = imageio_ffmpeg.read_frames(str(video), pix_fmt="rgb24")
    metadata = next(frames)
    width, height = metadata["size"]
    fps = float(metadata["fps"])
    if fps <= 0:
        frames.close()
        raise ValueError("Video frame rate is unavailable; timestamps cannot be audited.")
    columns = ["frame", "video_seconds", "log_seconds", "chapter", "expected_transition",
               "mean_luma", "near_black_percent", "near_white_percent", "edge_energy", "mean_frame_delta", "flags"]
    count, outlier_count, expected_count, saved_flags = 0, 0, 0, []
    previous, thumbnails, sheet_paths = None, [], []
    next_thumbnail, last_saved_flag = 0.0, -100.0
    low_luma, high_luma, high_white, high_delta = math.inf, -math.inf, 0.0, 0.0
    flag_counts = {}

    def save_sheet():
        if not thumbnails:
            return
        sheet = Image.new("RGB", (4 * 320, 6 * 205), "#111827")
        draw = ImageDraw.Draw(sheet)
        for n, (thumb, caption) in enumerate(thumbnails):
            x, y = (n % 4) * 320, (n // 4) * 205
            sheet.paste(thumb, (x, y))
            draw.text((x + 6, y + 184), caption, fill="white")
        path = sheets_dir / f"sheet-{len(sheet_paths) + 1:03}.jpg"
        sheet.save(path, quality=90)
        sheet_paths.append(str(path.resolve()))
        thumbnails.clear()

    with (output / "per-frame.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.DictWriter(stream, fieldnames=columns)
        writer.writeheader()
        try:
            for number, data in enumerate(frames):
                rgb = np.frombuffer(data, dtype=np.uint8).reshape(height, width, 3)
                seconds, at = number / fps, number / fps + offset
                chapter_index = bisect.bisect_right(chapter_times, at) - 1
                chapter = chapters[chapter_index][1] if chapter_index >= 0 else "menu"
                transition = next((label for start, end, label in transitions if start <= at <= end), "")
                metrics, small = measure(rgb, previous)
                previous = small
                flags = []
                if metrics["near_black_percent"] >= 98.5 and metrics["mean_luma"] < 6:
                    flags.append("black_frame")
                if metrics["near_white_percent"] >= 35:
                    flags.append("highlight_clipping")
                if metrics["edge_energy"] < 1 and 12 < metrics["mean_luma"] < 240:
                    flags.append("very_low_detail")
                if number and metrics["mean_frame_delta"] >= 45:
                    flags.append("large_temporal_change")
                for flag in flags:
                    flag_counts[flag] = flag_counts.get(flag, 0) + 1
                if flags:
                    if transition:
                        expected_count += 1
                    else:
                        outlier_count += 1
                        if seconds - last_saved_flag >= 1 and len(saved_flags) < 48:
                            path = flags_dir / f"frame-{number:07}-{seconds:07.2f}s.png"
                            Image.fromarray(rgb).save(path)
                            saved_flags.append({"frame": number, "seconds": seconds, "flags": flags, "path": str(path.resolve())})
                            last_saved_flag = seconds
                writer.writerow({"frame": number, "video_seconds": f"{seconds:.6f}", "log_seconds": f"{at:.6f}",
                                 "chapter": chapter, "expected_transition": transition,
                                 **{k: f"{v:.6f}" for k, v in metrics.items()}, "flags": ";".join(flags)})
                if seconds + 1e-6 >= next_thumbnail:
                    thumb = Image.fromarray(rgb)
                    thumb.thumbnail((320, 180), Image.Resampling.LANCZOS)
                    tile = Image.new("RGB", (320, 180), "black")
                    tile.paste(thumb, ((320 - thumb.width) // 2, (180 - thumb.height) // 2))
                    thumbnails.append((tile, f"{seconds:6.1f}s | {chapter} | frame {number}"))
                    next_thumbnail += interval
                    if len(thumbnails) == 24:
                        save_sheet()
                count += 1
                low_luma = min(low_luma, metrics["mean_luma"])
                high_luma = max(high_luma, metrics["mean_luma"])
                high_white = max(high_white, metrics["near_white_percent"])
                high_delta = max(high_delta, metrics["mean_frame_delta"])
        finally:
            frames.close()
    if count == 0:
        raise ValueError("Video contains no decoded frames.")
    save_sheet()
    summary = {
        "video": str(video.resolve()), "log": str(Path(log).resolve()) if log else None,
        "decoded_frames": count, "csv_rows": count, "all_decoded_frames_measured": True,
        "fps": fps, "size": [width, height], "decoded_duration_seconds": count / fps,
        "container_duration_seconds": metadata.get("duration"), "log_timestamp_offset_seconds": offset,
        "metrics_resolution": {"luma_and_clipping": [width, height], "edges_and_delta": [320, 180]},
        "edge_metric": "Laplacian variance" if cv2 is not None else "squared gradient energy",
        "thresholds": {"black_frame": ">=98.5% luma<=5 and mean<6", "highlight_clipping": ">=35% luma>=250",
                       "very_low_detail": "edge_energy<1 with mean luma 12..240", "large_temporal_change": "mean delta>=45/255"},
        "observed": {"minimum_mean_luma": low_luma, "maximum_mean_luma": high_luma,
                     "maximum_near_white_percent": high_white, "maximum_mean_frame_delta": high_delta},
        "flag_counts_including_transitions": flag_counts, "flagged_frames_outside_expected_transitions": outlier_count,
        "flagged_frames_within_expected_transitions": expected_count,
        "expected_transition_policy": "Chapter/fade/Entering/Cinematic log events: -0.35s to +0.65s; flags remain in CSV but are excluded from outlier count.",
        "saved_flagged_frames": saved_flags, "contact_sheet_interval_seconds": interval, "contact_sheets": sheet_paths,
        "result": "REVIEW_REQUIRED" if outlier_count else "NO_THRESHOLD_OUTLIERS",
        "limits": "Thresholds are review aids. Recorded cuts may be intentional. Metrics do not establish visual, medical, UI, or performance correctness.",
    }
    (output / "summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    return summary


def self_test():
    with tempfile.TemporaryDirectory(prefix="human-eye-frame-audit-") as temp:
        root = Path(temp)
        writer = imageio_ffmpeg.write_frames(str(root / "test.mp4"), (64, 64), fps=30, codec="libx264", output_params=["-crf", "0"])
        writer.send(None)
        writer.send(np.zeros((64, 64, 3), dtype=np.uint8).tobytes())
        writer.send(np.full((64, 64, 3), 255, dtype=np.uint8).tobytes())
        checker = ((np.indices((64, 64)).sum(axis=0) // 4) % 2 * 150 + 50).astype(np.uint8)
        writer.send(np.repeat(checker[:, :, None], 3, axis=2).tobytes())
        writer.close()
        result = audit(root / "test.mp4", None, root / "audit")
        assert result["decoded_frames"] == result["csv_rows"] == 3
        assert result["flag_counts_including_transitions"]["black_frame"] == 1
        assert result["flag_counts_including_transitions"]["highlight_clipping"] == 1
        assert result["contact_sheets"] and (root / "audit/per-frame.csv").is_file()
        print("PASS: all three known frames measured; black and clipped-white frames detected.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("video", nargs="?")
    parser.add_argument("--log", help="AutoPilot walkthrough.log, used for chapter labels and transition timing")
    parser.add_argument("--output", type=Path)
    parser.add_argument("--interval", type=float, default=2.0, help="Contact-sheet sampling interval; every frame is still measured")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        self_test()
    elif not args.video or not args.output:
        parser.error("video and --output are required")
    else:
        result = audit(args.video, args.log, args.output, args.interval)
        print(json.dumps({key: result[key] for key in ("decoded_frames", "decoded_duration_seconds", "result", "flagged_frames_outside_expected_transitions", "contact_sheets")}))


if __name__ == "__main__":
    main()
