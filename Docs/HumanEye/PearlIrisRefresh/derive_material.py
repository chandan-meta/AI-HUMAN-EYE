"""Derive conservative candidate maps from one AI base-colour source; no geometric AO inferred."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageFilter

root = Path(__file__).resolve().parent
source = Image.open(root / 'EyeInterior_BaseColor_Candidate.png').convert('RGB')
rgb = np.asarray(source, dtype=np.float32) / 255
grey = np.asarray(source.convert('L'), dtype=np.float32) / 255
low = np.asarray(source.convert('L').filter(ImageFilter.GaussianBlur(4)), dtype=np.float32) / 255
height = grey - low
dx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) * .35
dy = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) * .35
normal = np.stack((-dx, dy, np.ones_like(dx)), axis=-1)
normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
rough = np.clip(.62 + height * .15, .55, .7)
maps = {'BaseColor': rgb, 'Normal': normal * .5 + .5, 'Roughness': rough,
        'Smoothness': 1 - rough, 'SpecularMask': np.clip(.72 - height * .1, 0, 1)}
for name, values in maps.items():
    im = Image.fromarray(np.uint8(np.clip(values, 0, 1) * 255))
    im.save(root / f'EyeInterior_{name}.png')
    for size in (2048, 1024):
        im.resize((size, size), Image.Resampling.LANCZOS).save(root / f'EyeInterior_{name}_{size}.png')
assert np.isfinite(normal).all() and np.max(np.abs(np.linalg.norm(normal, axis=-1)-1)) < 1e-5
assert all(v.shape[:2] == grey.shape for v in maps.values())
(root / 'material-status.json').write_text(json.dumps({
    'status': 'Candidate — UV fit and normal orientation unverified',
    'source_resolution': list(source.size),
    'runtime_variants': '2048 and 1024 resampled from actual source; 2048 may be upscaled',
    'normal': 'Conservative tangent-space estimate from high-pass luminance; not geometry baked',
    'roughness': 'Art-directed estimate, not measured',
    'ao': 'Omitted: cannot infer geometric occlusion from colour',
    'shader': 'HumanEye/Anatomy: _BaseMap sRGB, _BumpMap normal import, _SpecMap linear red. _Gloss is scalar; roughness is handoff only.',
    'vessels': 'Original geometry and projected overlay must remain separate and unchanged'
}, indent=2))
print('Aligned candidate maps saved; numerical checks passed.', source.size)
