using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HumanEye.EditorTools
{
    /// Project-owned textures generated deterministically (fixed seeds), so no texture with unclear rights is needed.
    public static class ProceduralTextures
    {
        public const string Folder = "Assets/HumanEye/Generated/Textures/";

        static Texture2D Save(string name, Color[] px, int w, int h, bool srgb = true, TextureWrapMode wrap = TextureWrapMode.Clamp, bool alpha = false)
        {
            Directory.CreateDirectory(Folder);
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels(px); t.Apply();
            var path = Folder + name + ".png";
            File.WriteAllBytes(path, t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.sRGBTexture = srgb; ti.wrapMode = wrap; ti.alphaIsTransparency = alpha; ti.mipmapEnabled = true;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float Hash(int x, int seed) { unchecked { uint h = (uint)(x * 374761393 + seed * 668265263); h = (h ^ (h >> 13)) * 1274126177; return (h ^ (h >> 16)) / (float)uint.MaxValue; } }
        static float Noise1(float x, int seed) { int i = Mathf.FloorToInt(x); float f = x - i; f = f * f * (3 - 2 * f); return Mathf.Lerp(Hash(i, seed), Hash(i + 1, seed), f); }

        /// Subtle seamless nebula behind the separate star geometry; project-owned and deterministic.
        public static Texture2D ObservatorySky()
        {
            const int w = 1024, h = 512;
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w, v = y / (float)h;
                    float a = u * Mathf.PI * 2, nx = 4 + Mathf.Cos(a) * 2, ny = 4 + Mathf.Sin(a) * 2;
                    float noise = Mathf.PerlinNoise(nx + v * 4, ny) * .6f + Mathf.PerlinNoise(nx * 4, ny * 4 + v * 15) * .3f + Mathf.PerlinNoise(nx * 16, ny * 16 + v * 60) * .1f;
                    float band = Mathf.Exp(-Mathf.Pow((v - .25f - .14f * Mathf.Sin(a * 2)) / .15f, 2));
                    float cloud = Mathf.Pow(Mathf.Clamp01((noise - .25f) * 2.2f), .7f) * band;
                    pixels[y * w + x] = Color.Lerp(new Color(.008f, .014f, .033f), new Color(.17f, .27f, .45f), cloud);
                }
            return Save("ObservatorySky", pixels, w, h, true, TextureWrapMode.Repeat);
        }

        /// Polar iris map: u = angle around the pupil, v = 0 at the pupil margin → 1 at the iris root.
        public static Texture2D Iris()
        {
            int w = 1024, h = 256; var px = new Color[w * h];
            var rnd = new System.Random(7);
            var crypts = new Vector3[60];
            for (int i = 0; i < crypts.Length; i++) crypts[i] = new Vector3((float)rnd.NextDouble(), .3f + .5f * (float)rnd.NextDouble(), .004f + .008f * (float)rnd.NextDouble());
            Color pupilRuff = new(.16f, .09f, .05f), collarette = new(.78f, .56f, .26f), stroma = new(.5f, .74f, .88f), stromaDark = new(.22f, .42f, .58f), limbal = new(.08f, .14f, .2f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w, v = y / (float)(h - 1);
                    // Radial fibres at a frequency the mip chain keeps (≈ 6–15 px per stripe).
                    float warp = Noise1(u * 12 + v * 2, 3) * .02f;
                    float fibre = Mathf.Clamp01(Noise1((u + warp) * 70, 11) * .65f + Noise1((u + warp) * 160 + v * 4, 12) * .35f);
                    fibre = Mathf.SmoothStep(0, 1, fibre);
                    var c = Color.Lerp(stromaDark, stroma, fibre);
                    float col = Mathf.Exp(-Mathf.Pow((v - .3f - .03f * Noise1(u * 24, 5)) / .09f, 2));
                    c = Color.Lerp(c, collarette * (.8f + .4f * fibre), col * .85f);
                    c = Color.Lerp(c, pupilRuff, Mathf.SmoothStep(.12f, 0, v));
                    c = Color.Lerp(c, limbal, Mathf.SmoothStep(.82f, 1, v));
                    foreach (var k in crypts)
                    {
                        float du = Mathf.Min(Mathf.Abs(u - k.x), 1 - Mathf.Abs(u - k.x)) * 3.2f, dv = v - k.y;
                        if (du * du + dv * dv < k.z * k.z * 4) c *= .65f;
                    }
                    c.a = 1;
                    px[y * w + x] = c;
                }
            return Save("Iris", px, w, h, true, TextureWrapMode.Repeat);
        }

        /// Branching vessels in an azimuthal projection centred on the texture (white = vessel).
        public static Texture2D Vessels(string name, int seed, int trunks, float startRadius, float width, int depth, float wander)
        {
            int n = 1024; var px = new Color[n * n];
            var buf = new float[n * n];
            var rnd = new System.Random(seed);
            void Branch(Vector2 p, Vector2 dir, float w, int level, float length)
            {
                for (float s = 0; s < length; s += .5f)
                {
                    dir = (Quaternion.Euler(0, 0, ((float)rnd.NextDouble() - .5f) * wander) * dir).normalized;
                    p += dir * .5f;
                    if (p.x < 0 || p.y < 0 || p.x >= n || p.y >= n) return;
                    int r = Mathf.CeilToInt(w + 1);
                    for (int dy = -r; dy <= r; dy++)
                        for (int dx = -r; dx <= r; dx++)
                        {
                            int X = (int)p.x + dx, Y = (int)p.y + dy;
                            if (X < 0 || Y < 0 || X >= n || Y >= n) continue;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            buf[Y * n + X] = Mathf.Max(buf[Y * n + X], Mathf.Clamp01(w - d + .5f));
                        }
                    if (level < depth && rnd.NextDouble() < .018)
                    {
                        var side = Quaternion.Euler(0, 0, (rnd.NextDouble() < .5 ? -1 : 1) * (25 + 30 * (float)rnd.NextDouble())) * dir;
                        Branch(p, side, w * .65f, level + 1, length * .55f);
                    }
                    w = Mathf.Max(.6f, w * .998f);
                }
            }
            var c0 = new Vector2(n / 2f, n / 2f);
            for (int i = 0; i < trunks; i++)
            {
                float a = (i + (float)rnd.NextDouble() * .6f) / trunks * Mathf.PI * 2;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Branch(c0 + dir * startRadius * n * .5f, dir, width, 0, n * .5f);
            }
            for (int pass = 0; pass < 2; pass++) buf = Blur(buf, n);   // soft edges so close-ups don't look stair-stepped
            for (int i = 0; i < px.Length; i++) px[i] = new Color(buf[i], buf[i], buf[i], buf[i]);
            return Save(name, px, n, n, false);
        }

        static float[] Blur(float[] src, int n)
        {
            var dst = new float[src.Length];
            for (int y = 1; y < n - 1; y++)
                for (int x = 1; x < n - 1; x++)
                {
                    float s = 0;
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) s += src[(y + dy) * n + x + dx] * (dx == 0 && dy == 0 ? 4 : dx == 0 || dy == 0 ? 2 : 1);
                    dst[y * n + x] = s / 16;
                }
            return dst;
        }

        public static Texture2D Streak()
        {
            int w = 256, h = 16; var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w, across = 1 - Mathf.Abs(y / (float)(h - 1) * 2 - 1);
                    float pulse = .55f + .45f * Mathf.Pow(Mathf.Sin(u * Mathf.PI * 2) * .5f + .5f, 3);
                    float a = Mathf.SmoothStep(0, 1, across) * pulse;
                    px[y * w + x] = new Color(1, 1, 1, a);
                }
            return Save("Streak", px, w, h, true, TextureWrapMode.Repeat, true);
        }

        public static Texture2D Dash()
        {
            int w = 128, h = 16; var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w, across = 1 - Mathf.Abs(y / (float)(h - 1) * 2 - 1);
                    float on = Mathf.SmoothStep(.0f, .08f, u) * Mathf.SmoothStep(.62f, .54f, u);
                    px[y * w + x] = new Color(1, 1, 1, Mathf.SmoothStep(0, 1, across) * on);
                }
            return Save("Dash", px, w, h, true, TextureWrapMode.Repeat, true);
        }

        public static Texture2D Soft()
        {
            int n = 64; var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f - .5f, n / 2f - .5f)) / (n / 2f);
                    float a = Mathf.Clamp01(1 - d); a *= a;
                    px[y * n + x] = new Color(1, 1, 1, a);
                }
            return Save("Soft", px, n, n, true, TextureWrapMode.Clamp, true);
        }

        public static Texture2D BeamNoise()
        {
            int w = 64, h = 256; var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float n = Noise1(x * .25f + y * .02f, 21) * .5f + Noise1(y * .06f, 22) * .5f;
                    px[y * w + x] = new Color(1, 1, 1, .45f + .55f * n);
                }
            return Save("BeamNoise", px, w, h, true, TextureWrapMode.Repeat, true);
        }

        /// Combines a colour map and a separate alpha map (Poly Haven foliage) into one RGBA texture for alpha clipping.
        public static Texture2D CombineAlpha(string colourPath, string alphaPath, string name)
        {
            var c = new Texture2D(2, 2); c.LoadImage(File.ReadAllBytes(colourPath));
            var a = new Texture2D(2, 2); a.LoadImage(File.ReadAllBytes(alphaPath));
            int w = c.width, h = c.height; var px = c.GetPixels();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x].a = a.GetPixelBilinear(x / (float)w, y / (float)h).r;
            UnityEngine.Object.DestroyImmediate(c); UnityEngine.Object.DestroyImmediate(a);
            return Save(name, px, w, h, true, TextureWrapMode.Repeat, true);
        }
    }
}
