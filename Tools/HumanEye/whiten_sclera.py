"""Sclera texture for the detailed eye: the supplied Eye_Diff.jpg paints the whole globe flesh-pink beyond a small white
patch, which reads as a bloodshot ball from the front. Keep its veins and iris, flatten the visible front of the
globe to a warm biological white, add a soft limbal ring, and let the original pink return toward the back.
Run: python Tools/HumanEye/blue_iris.py, then python Tools/HumanEye/whiten_sclera.py  ->  Assets/HumanEye/ThirdParty/LocalEye/Eye_Diff_Sclera.jpg
"""
import pathlib
import numpy as np
from PIL import Image, ImageFilter

folder = pathlib.Path(__file__).resolve().parents[2] / "Assets/HumanEye/ThirdParty/LocalEye"
src = Image.open(folder / "Eye_Diff_Blue.jpg").convert("RGB")   # blue_iris.py first (visual-match pass): the globe's painted iris matches the iris mesh
tex = np.asarray(src).astype(np.float32) / 255
h, w, _ = tex.shape
y, x = np.mgrid[0:h, 0:w]
r = np.hypot((x + .5) / w - .5, (y + .5) / h - .5)              # uv distance from the iris centre
# low-pass of the sclera alone (the dark iris must not bleed into it, or a bright halo appears at the limbus)
fill = tex.copy(); ring = (r > .2) & (r < .26)
fill[r < .2] = np.median(tex[ring], axis=0)
base = np.asarray(Image.fromarray((fill * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(28))).astype(np.float32) / 255
white = np.array([0.93, 0.895, 0.865])                           # warm biological white, never pure white
front = np.clip((0.64 - r) / (0.64 - 0.44), 0, 1) * np.clip((r - 0.188) / 0.012, 0, 1)
front = front[..., None] ** 1.5
out = tex * (1 - front) + (white + (tex - base) * 1.7) * front   # veins and grain, strengthened for the hero close-ups (reference pass)
limbus = 1 - 0.4 * np.exp(-((r - 0.196) / 0.011) ** 2) * (r > 0.18)
out *= limbus[..., None]
Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8)).save(folder / "Eye_Diff_Sclera.jpg", quality=94)
print("SCLERA_TEXTURE_WRITTEN", out.shape)
