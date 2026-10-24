"""Daylight sky for the new lab's windows (visual-match pass, 8 October 2026): a bright blue-white day with soft cumulus,
a hazy blue-green horizon and a sun placed where the module's key light comes from, as an equirectangular Radiance HDR
for Unity's Skybox/Panoramic (rotation 0). Made here rather than downloaded, so it carries no licence terms. The galaxy
seen overhead on the art boards is a separate interior projection (GalaxyDome.shader), never this sky.
Run: python Tools/HumanEye/generate_daylight_sky.py  ->  Assets/HumanEye/Generated/Textures/DaylightSky.hdr (+ a preview PNG
next to the script's output when PREVIEW is given as the first argument).
"""
import pathlib
import sys
import numpy as np

W, H = 4096, 2048
SUN = np.array([-.37, .62, -.70]); SUN /= np.linalg.norm(SUN)        # opposite of the Key Light's forward (.37, -.62, .70)
out_path = pathlib.Path(__file__).resolve().parents[2] / "Assets/HumanEye/Generated/Textures/DaylightSky.hdr"
rng = np.random.default_rng(20261008)

# Unity Skybox/Panoramic: u = .5 - atan2(z, x) / 2pi, v = 1 - acos(y) / pi (image row 0 = straight up)
u = (np.arange(W) + .5) / W
v = (np.arange(H) + .5) / H
lon = (.5 - u) * 2 * np.pi
theta = v * np.pi
st, ct = np.sin(theta)[:, None], np.cos(theta)[:, None]
x = st * np.cos(lon)[None, :]; z = st * np.sin(lon)[None, :]; y = np.broadcast_to(ct, (H, W))
elev = np.arcsin(np.clip(y, -1, 1))                                    # radians above the horizon


def lattice(n):
    return rng.random((n, n), dtype=np.float32)


def value_noise(px, py, grid):
    """Tileable bilinear value noise: px, py in lattice units."""
    n = grid.shape[0]
    x0 = np.floor(px).astype(np.int64); y0 = np.floor(py).astype(np.int64)
    fx = px - x0; fy = py - y0
    fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy)
    x0 %= n; y0 %= n; x1 = (x0 + 1) % n; y1 = (y0 + 1) % n
    a = grid[y0, x0]; b = grid[y0, x1]; c = grid[y1, x0]; d = grid[y1, x1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def fbm(px, py, octaves, grids):
    total = np.zeros_like(px, dtype=np.float32); amp, norm = .5, 0.0
    for o in range(octaves):
        f = 2 ** o
        total += amp * value_noise(px * f + 17.3 * o, py * f - 9.1 * o, grids[o]); norm += amp; amp *= .5
    return total / norm


# ---- sky gradient (linear radiance) ----
up = np.clip(y, 0, 1)
zenith = np.array([.16, .36, .92]); horizon = np.array([.78, .88, 1.0])
t = 1 - np.exp(-up * 4.2)
sky = horizon * (1 - t)[..., None] + zenith * t[..., None]
sky *= 1.35
cos_sun = x * SUN[0] + y * SUN[1] + z * SUN[2]
glow = np.exp((cos_sun - 1) * 28) * 1.6 + np.exp((cos_sun - 1) * 4) * .35
sky += np.array([1.0, .92, .78]) * glow[..., None]

# ---- cumulus on a plane above the viewer, compressed toward the horizon by perspective ----
grids = [lattice(256) for _ in range(6)]
above = y > .015
with np.errstate(divide='ignore', invalid='ignore'):
    px = np.where(above, x / np.maximum(y, .015), 0) * 2.2
    pz = np.where(above, z / np.maximum(y, .015), 0) * 2.2
warp = fbm(px * 1.5 + 11, pz * 1.5 - 4, 3, grids[3:])                     # domain warp: billowy, less blobby shapes
far = np.clip(y / .35, 0, 1)                                             # fewer octaves toward the horizon (no speckle)
shape = fbm(px * 2.2 + 40 + warp * .9, pz * 2.2 + 40 - warp * .9, 6, grids) * (.75 + .25 * far) + .5 * (.25 - .25 * far)
cover = np.clip((shape - .53) / .17, 0, 1) ** 1.3
cover *= np.clip((y - .04) / .22, 0, 1) ** 1.5                           # thin out into the horizon haze
detail = fbm(px * 9 + 3, pz * 9 + 7, 4, grids[2:])
shade = .72 + .5 * detail + .35 * np.clip(cos_sun, 0, 1)                 # sunlit tops, greyer bellies
cloud = np.array([1.0, .99, .97]) * (1.55 * shade)[..., None]
cloud += np.array([1.0, .95, .85]) * (np.exp((cos_sun - 1) * 9) * 2.5 * cover)[..., None]   # silver lining near the sun
sky = sky * (1 - cover[..., None]) + cloud * cover[..., None]

# ---- below the horizon: hazy blue-green land and a soft distant tree/hill line ----
below = np.clip(-y, 0, 1)
ground = np.array([.30, .36, .30]) * .9
haze = np.array([.72, .82, .9])
land = haze * np.exp(-below * 9)[..., None] + ground * (1 - np.exp(-below * 9))[..., None]
sky = np.where((y < 0)[..., None], land, sky)
# a far city skyline in the haze (blue-grey, aerial perspective), then a nearer park treeline (muted green)
deg = np.degrees(lon[None, :] % (2 * np.pi)) * np.ones((H, 1))
edeg = np.degrees(elev)
sky_line = np.zeros(W)
a = 0.0
while a < 360:
    w_ = rng.uniform(.4, 1.8); h_ = rng.uniform(.4, 3.6) * (1.6 if rng.random() < .15 else 1)
    i0, i1 = int(a / 360 * W), int(min(a + w_, 360) / 360 * W)
    sky_line[i0:i1] = np.maximum(sky_line[i0:i1], h_ if rng.random() < .7 else 0)
    a += w_ * rng.uniform(.6, 1.4)
city = (edeg < sky_line[None, :]) & (edeg > -.3)
sky = np.where(city[..., None], sky * .55 + np.array([.55, .64, .74]) * .45, sky)
trees = 1.1 + 1.3 * fbm(np.cos(lon)[None, :] * 9 + 5 + 0 * y, np.sin(lon)[None, :] * 9 + 5 + 0 * y, 5, grids)
treeline = (edeg < trees) & (edeg > -1.2)
sky = np.where(treeline[..., None], sky * .3 + np.array([.32, .42, .36]) * .7, sky)

# ---- the sun disk (modest: the key light carries direct sun; this only gives reflections a bright spot) ----
disk = cos_sun > np.cos(np.radians(.9))
sky = np.where(disk[..., None], np.array([60.0, 56.0, 50.0]), sky)
sky = sky.astype(np.float32)


def write_hdr(path, img):
    h, w, _ = img.shape
    m = img.max(axis=2)
    mant, expo = np.frexp(m)
    scale = np.where(m > 1e-32, mant * 256.0 / np.maximum(m, 1e-32), 0)
    rgbe = np.zeros((h, w, 4), np.uint8)
    rgbe[..., :3] = np.clip(img * scale[..., None], 0, 255).astype(np.uint8)
    rgbe[..., 3] = np.where(m > 1e-32, expo + 128, 0).astype(np.uint8)
    with open(path, "wb") as f:
        f.write(b"#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n" + f"-Y {h} +X {w}\n".encode())
        f.write(rgbe.tobytes())                                          # flat scanlines (no RLE): every reader accepts them


out_path.parent.mkdir(parents=True, exist_ok=True)
write_hdr(out_path, sky)
print("DAYLIGHT_SKY_WRITTEN", out_path, sky.shape, float(sky.mean()))
if len(sys.argv) > 1:
    from PIL import Image
    ldr = np.clip((sky / (1 + sky)) ** (1 / 2.2) * 1.15, 0, 1)
    Image.fromarray((ldr * 255).astype(np.uint8)).resize((2048, 1024)).save(sys.argv[1])
