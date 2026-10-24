#ifndef HE_ANATOMY_DISSOLVE
#define HE_ANATOMY_DISSOLVE
// Electric-cyan patchy disintegration (reference clip 20261014-0921-28.5960490.mp4, 14 October 2026): one seeded breakup
// field shared by every anatomy layer (Anatomy.shader) and by the surface motes (DissolveMote.shader), so skin, muscles and
// organs erode in step and the next layer appears exactly where the last one breaks. Measured from the subject's root.
// Globals are set by AnatomyRevealController; _HE_Dissolve.x = 0 leaves the plain horizontal wipe in place.
float4 _HE_Dissolve;         // x noise amplitude (m), y crack cells per metre, z seed, w filament reach (m)
float4 _HE_DissolveOrigin;   // xyz subject root (world)
float4 _HE_DissolveColor;    // rgb crack colour, w emission intensity

float3 HE_Hash3(float3 p)
{
    p = frac(p * float3(.1031, .1030, .0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yxx) * p.zyx);
}

// Worley cellular noise: x = distance to the nearest seed (0 at seeds, about 1 at borders), y = to the second nearest.
float2 HE_Worley(float3 p)
{
    float3 i = floor(p), f = frac(p); float2 d = 8;
    for (int z = -1; z <= 1; z++) for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
    {
        float3 g = float3(x, y, z); float3 r = g + HE_Hash3(i + g) - f; float s = dot(r, r);
        d.y = s < d.x ? d.x : min(d.y, s); d.x = min(d.x, s);
    }
    return sqrt(d);
}

float HE_Value(float3 p)
{
    float3 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
    float4 a = float4(HE_Hash3(i).x, HE_Hash3(i + float3(1, 0, 0)).x, HE_Hash3(i + float3(0, 1, 0)).x, HE_Hash3(i + float3(1, 1, 0)).x);
    float4 b = float4(HE_Hash3(i + float3(0, 0, 1)).x, HE_Hash3(i + float3(1, 0, 1)).x, HE_Hash3(i + float3(0, 1, 1)).x, HE_Hash3(i + float3(1, 1, 1)).x);
    float4 c = lerp(a, b, f.z); float2 e = lerp(c.xy, c.zw, f.y);
    return lerp(e.x, e.y, f.x);
}

// World height plus a seeded multi-scale offset: cell borders sit high (cracks open first), cell islands low (they vanish
// last) and fractal grain breaks the borders into jagged filaments. A layer is gone where the field lies above the front.
float HE_DissolveField(float3 positionWS, out float2 cells)
{
    float3 q = (positionWS - _HE_DissolveOrigin.xyz) * _HE_Dissolve.y + _HE_Dissolve.z;
    cells = HE_Worley(q);
    float grain = HE_Value(q * 1.9 + 7.3) * .6 + HE_Value(q * 4.1 + 3.1) * .4;
    return positionWS.y + ((cells.x - .5) * 1.1 + (grain - .5) * .9) * _HE_Dissolve.x;
}
float HE_DissolveField(float3 positionWS) { float2 cells; return HE_DissolveField(positionWS, cells); }
#endif
