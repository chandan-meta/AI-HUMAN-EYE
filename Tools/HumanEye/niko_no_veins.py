"""Paint the red veins out of Niko's head (22 October 2026 brief: the bloodshot eyeball head looked creepy).
A grey closing (max then min filter) erases thin dark lines; only pixels on the pink head skin that the closing lightened
take the closed colour, so the blushes, eyes, text and coat keep their paint. Run once after export_niko.py (which copies
the supplied texture byte for byte):  python -I Tools/HumanEye/niko_no_veins.py [--preview out.png]"""
import pathlib, sys
import numpy as np
from PIL import Image, ImageFilter

TEX = pathlib.Path(__file__).resolve().parents[2] / "Assets/HumanEye/ThirdParty/UserModels/Niko/Niko_BaseColor.jpg"

def clean(img, size=11):
    closed = np.dstack([np.asarray(c.filter(ImageFilter.MaxFilter(size)).filter(ImageFilter.MinFilter(size)).filter(ImageFilter.GaussianBlur(3)), np.int16)
                        for c in img.split()])   # the blur hides the filters' square footprint
    a = np.asarray(img, np.int16)
    r, g, b = closed[..., 0], closed[..., 1], closed[..., 2]
    skin = (r > 215) & (g > 160) & (b > 150) & (r - b < 90)            # the pale pink head skin around the line
    vein = skin & ((closed[..., 1] - a[..., 1]) > 8) & (a[..., 0] - a[..., 2] > 30) & (a[..., 0] > 170)   # a darker, reddish line (not the coat's blue text)
    vein = np.asarray(Image.fromarray(vein.astype(np.uint8) * 255).filter(ImageFilter.MaxFilter(5))) > 0   # and its soft edge
    out = np.where(vein[..., None], closed, a).astype(np.uint8)
    return Image.fromarray(out), int(vein.sum())

if __name__ == "__main__":
    img = Image.open(TEX).convert("RGB")
    out, n = clean(img)
    assert n > 0, "no vein pixels found"
    if "--preview" in sys.argv: out.save(sys.argv[sys.argv.index("--preview") + 1]); print("preview", n)
    else: out.save(TEX, quality=95); print("veins removed", n, "px ->", TEX)
