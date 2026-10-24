"""Bright blue iris for the detailed eye (visual-match pass, 8 October 2026). The art boards (REF_01, REF_07) show a vivid
blue iris with white stromal fibres and a thin amber ring at the pupil; the supplied Eye_Diff.jpg paints a muted blue-grey
iris with a broad amber collarette. Every fibre is kept: the stroma is recoloured by luminance (amber in a thin band at the
pupil, azure through the middle, deep blue at the limbus) with 18 % of the original colour left in for variation; the pupil
and the sclera are untouched.
Run: python Tools/HumanEye/blue_iris.py, then whiten_sclera.py  ->  Assets/HumanEye/ThirdParty/LocalEye/Eye_Diff_Blue.jpg
"""
import pathlib
import numpy as np
from PIL import Image

folder = pathlib.Path(__file__).resolve().parents[2] / "Assets/HumanEye/ThirdParty/LocalEye"
tex = np.asarray(Image.open(folder / "Eye_Diff.jpg").convert("RGB")).astype(np.float32) / 255
h, w, _ = tex.shape
y, x = np.mgrid[0:h, 0:w]
r = np.hypot((x + .5) / w - .5, (y + .5) / h - .5)               # uv distance from the iris centre (iris edge ≈ .19)
lum = tex @ np.array([.299, .587, .114])
across = np.clip((r - .075) / (.19 - .075), 0, 1)                 # 0 at the pupil edge, 1 at the limbus
keys = np.array([0, .14, .3, .62, .9, 1])                          # amber ring → azure → deep blue → dark limbal ring
cols = np.array([[1.05, .72, .3], [.92, .72, .4], [.36, .64, .98], [.2, .45, .9], [.1, .26, .66], [.06, .14, .38]])
tone = np.stack([np.interp(across, keys, cols[:, c]) for c in range(3)], -1)
ref = tone @ np.array([.299, .587, .114])
fibres = np.clip((lum / np.maximum(ref, 1e-3)) ** 1.15, 0, 2.2)    # a little more fibre contrast than the source
blue = np.clip(tone * fibres[..., None] * 1.08, 0, 1)
iris = np.clip((.195 - r) / .008, 0, 1) * (r > .072)              # soft at the limbus, nothing in the pupil
k = (.82 * iris)[..., None]
out = tex * (1 - k) + blue * k
Image.fromarray((out * 255).astype(np.uint8)).save(folder / "Eye_Diff_Blue.jpg", quality=94)
print("BLUE_IRIS_WRITTEN", out.shape)
