using System.IO;
using UnityEditor;
using UnityEngine;

namespace Liminal.Editor
{
    /// <summary>
    /// 목업의 canvas 절차 텍스처를 C#으로 옮긴 것 (임시 아트).
    /// PNG로 저장하고 Point 필터 / 밉맵 끔 / Repeat 로 임포트한다.
    /// </summary>
    public static class B58ProcTextures
    {
        public const string Folder = "Assets/_Project/Textures/B58";

        class Canvas
        {
            public readonly int W, H;
            readonly Color32[] px;
            readonly System.Random rng;

            public Canvas(int w, int h, int seed)
            {
                W = w; H = h; px = new Color32[w * h];
                rng = new System.Random(seed);
            }

            public float Rnd(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            /// <summary>canvas 좌표(y 아래로)로 사각형 채우기, 알파 블렌딩.</summary>
            public void Rect(float x, float y, float w, float h, Color c)
            {
                int x0 = Mathf.Max(0, Mathf.FloorToInt(x)), y0 = Mathf.Max(0, Mathf.FloorToInt(y));
                int x1 = Mathf.Min(W, Mathf.FloorToInt(x + w)), y1 = Mathf.Min(H, Mathf.FloorToInt(y + h));
                for (int yy = y0; yy < y1; yy++)
                for (int xx = x0; xx < x1; xx++) Plot(xx, yy, c);
            }

            public void Plot(int x, int y, Color c)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return;
                int i = (H - 1 - y) * W + x;
                Color d = px[i];
                px[i] = Color.Lerp(d, new Color(c.r, c.g, c.b, 1f), c.a);
            }

            public void Fill(Color c) => Rect(0, 0, W, H, c);

            public void Stroke(int x, int y, int w, int h, Color c, int lw = 1)
            {
                Rect(x, y, w, lw, c); Rect(x, y + h - lw, w, lw, c);
                Rect(x, y, lw, h, c); Rect(x + w - lw, y, lw, h, c);
            }

            public void Speck(string[] cols, int n, int sz = 1)
            {
                for (int i = 0; i < n; i++)
                    Rect((int)Rnd(0, W), (int)Rnd(0, H), sz, sz, Hex(cols[i % cols.Length]));
            }

            public void Ellipse(float cx, float cy, float rx, float ry, Color c, bool strokeOnly = false, float lw = 2f)
            {
                for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (strokeOnly ? Mathf.Abs(r - 1f) * Mathf.Min(rx, ry) < lw / 2f : r <= 1f) Plot(x, y, c);
                }
            }

            /// <summary>3x5 픽셀 글꼴 (숫자·일부 영문만).</summary>
            public void Text(string s, int x, int y, int scale, Color c)
            {
                foreach (char ch in s)
                {
                    if (Glyphs.TryGetValue(ch, out var rows))
                        for (int r = 0; r < 5; r++)
                        for (int k = 0; k < 3; k++)
                            if (rows[r][k] == '#') Rect(x + k * scale, y + r * scale, scale, scale, c);
                    x += 4 * scale;
                }
            }

            public Texture2D Save(string name)
            {
                var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
                t.SetPixels32(px);
                t.Apply();
                string path = $"{Folder}/{name}.png";
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Default;
                imp.filterMode = FilterMode.Point;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        static readonly System.Collections.Generic.Dictionary<char, string[]> Glyphs = new()
        {
            ['-'] = new[] { "...", "...", "###", "...", "..." },
            ['5'] = new[] { "###", "#..", "###", "..#", "###" },
            ['8'] = new[] { "###", "#.#", "###", "#.#", "###" },
            ['B'] = new[] { "##.", "#.#", "##.", "#.#", "##." },
        };

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        static Color Rgba(int r, int g, int b, float a) => new Color(r / 255f, g / 255f, b / 255f, a);

        public static Texture2D Plank()
        {
            var g = new Canvas(64, 64, 1);
            string[] c = { "#3a2818", "#4a301c", "#33261a", "#553522", "#2e2016", "#402c1c" };
            for (int i = 0; i < 8; i++) g.Rect(i * 8, 0, 8, 64, Hex(c[i % 6]));
            g.Speck(new[] { "#5c3a22", "#2a4d4f", "#3e6b6b", "#1b1410", "#6b4a2a", "#233a3d" }, 1100);
            for (int i = 0; i < 16; i++) g.Rect((int)g.Rnd(0, 64), (int)g.Rnd(0, 64), 1, (int)g.Rnd(4, 16), Rgba(70, 120, 118, .55f));
            for (int i = 0; i < 8; i++) g.Rect(i * 8, 0, 1, 64, Hex("#0d0806"));
            return g.Save("T_Plank");
        }

        public static Texture2D Ceil()
        {
            var g = new Canvas(64, 64, 2);
            g.Fill(Hex("#2c2015"));
            for (int i = 0; i < 8; i++) g.Rect(0, i * 8, 64, 7, Hex(i % 2 == 1 ? "#36281a" : "#291d13"));
            g.Speck(new[] { "#4a3522", "#1a120c", "#5a4a30" }, 600);
            return g.Save("T_Ceil");
        }

        public static Texture2D Moss()
        {
            var g = new Canvas(64, 64, 3);
            g.Fill(Hex("#0f2016"));
            g.Speck(new[] { "#1d3a26", "#2b4d2f", "#0a120d", "#36583a", "#14261b", "#1c2c3a" }, 1800, 2);
            return g.Save("T_Moss");
        }

        public static Texture2D KitchenWall()
        {
            var g = new Canvas(64, 64, 4);
            g.Fill(Hex("#8f9a7e"));
            g.Speck(new[] { "#9fa98c", "#7f8a70", "#a6ad8f", "#6f7a62" }, 900);
            for (int y = 0; y < 64; y++) g.Rect(0, y, 64, 1, Rgba(40, 40, 20, .35f * y / 63f));
            return g.Save("T_KitchenWall");
        }

        public static Texture2D KitchenFloor()
        {
            var g = new Canvas(32, 32, 5);
            g.Fill(Hex("#4a3826"));
            g.Rect(0, 0, 16, 16, Hex("#3d2e20")); g.Rect(16, 16, 16, 16, Hex("#3d2e20"));
            g.Speck(new[] { "#5a4430", "#2e2218", "#544028" }, 250);
            return g.Save("T_KitchenFloor");
        }

        public static Texture2D Cream()
        {
            var g = new Canvas(32, 32, 6);
            g.Fill(Hex("#c9bc8c"));
            g.Speck(new[] { "#b7aa7a", "#d6cba0", "#a69a6c" }, 180);
            g.Stroke(3, 3, 26, 26, Hex("#8f845c"));
            return g.Save("T_Cream");
        }

        public static Texture2D White()
        {
            var g = new Canvas(32, 32, 7);
            g.Fill(Hex("#d4d6c6"));
            g.Speck(new[] { "#c3c6b4", "#e1e2d4", "#b1b5a0" }, 200);
            return g.Save("T_White");
        }

        public static Texture2D Dark()
        {
            var g = new Canvas(32, 32, 8);
            g.Fill(Hex("#2b1f17"));
            g.Speck(new[] { "#3a2a1e", "#1b130e", "#45321f" }, 200);
            g.Stroke(2, 2, 28, 28, Hex("#120c08"));
            return g.Save("T_Dark");
        }

        public static Texture2D Red()
        {
            var g = new Canvas(64, 64, 9);
            g.Fill(Hex("#86585a"));
            g.Speck(new[] { "#946466", "#74494c", "#9e7072", "#5f3a3e", "#8b6a6a" }, 1500);
            for (int i = 0; i < 10; i++) g.Rect((int)g.Rnd(0, 64), (int)g.Rnd(10, 64), 1, (int)g.Rnd(6, 20), Rgba(40, 10, 14, .35f));
            return g.Save("T_RedWall");
        }

        public static Texture2D RedPlank()
        {
            var g = new Canvas(64, 64, 10);
            string[] c = { "#3b2421", "#33201d", "#432a25", "#2d1b19" };
            for (int i = 0; i < 8; i++)
            {
                g.Rect(0, i * 8, 64, 8, Hex(c[i % 4]));
                g.Rect(0, i * 8, 64, 1, Hex("#170c0b"));
                g.Rect((i * 23) % 64, i * 8, 1, 8, Hex("#170c0b"));
            }
            g.Speck(new[] { "#4d302b", "#24150f", "#5a3a33" }, 700);
            return g.Save("T_RedPlank");
        }

        public static Texture2D Door()
        {
            var g = new Canvas(32, 64, 11);
            g.Fill(Hex("#3a2620"));
            g.Stroke(4, 5, 24, 24, Hex("#1c110e"), 2);
            g.Stroke(4, 34, 24, 26, Hex("#1c110e"), 2);
            g.Speck(new[] { "#4a322a", "#24160f" }, 200);
            g.Rect(25, 33, 2, 3, Hex("#8a7a50"));
            return g.Save("T_Door");
        }

        public static Texture2D EyeRug()
        {
            var g = new Canvas(128, 96, 12);
            g.Fill(Hex("#4a1517"));
            g.Stroke(4, 4, 120, 88, Hex("#6e2626"), 3);
            g.Stroke(10, 10, 108, 76, Hex("#280809"), 2);
            for (int x = 14; x < 114; x += 8) { g.Rect(x, 14, 3, 3, Hex("#7a2a28")); g.Rect(x, 79, 3, 3, Hex("#7a2a28")); }
            g.Ellipse(64, 48, 34, 16, Hex("#9a7a6e"));
            g.Ellipse(64, 48, 13, 13, Hex("#5a1c1c"));
            g.Ellipse(64, 48, 7, 7, Hex("#2a0707"));
            g.Ellipse(64, 48, 3.5f, 3.5f, Hex("#000000"));
            g.Ellipse(64, 48, 34, 16, Hex("#2a0a0b"), true, 2f);
            g.Speck(new[] { "#3a0e10", "#5e2020", "#2a0809" }, 1400);
            return g.Save("T_EyeRug");
        }

        public static Texture2D KitchenRug()
        {
            var g = new Canvas(16, 16, 13);
            g.Fill(Hex("#4d3a2a"));
            g.Speck(new[] { "#5e4630", "#3a2a1c" }, 60);
            return g.Save("T_KitchenRug");
        }

        /// <summary>「-58」 표지판. 교체 대상: 실제 표지판 텍스처.</summary>
        public static Texture2D Sign58()
        {
            var g = new Canvas(48, 64, 14);
            g.Fill(Hex("#1d2742"));
            g.Stroke(1, 1, 46, 46 + 16, Hex("#7d8396"), 2);
            g.Text("B", 7, 7, 1, Hex("#c97a3a"));
            g.Rect(13, 9, 5, 1, Hex("#c97a3a"));
            g.Text("-58", 4, 26, 3, Hex("#e7e2d2"));
            g.Speck(new[] { "#2c3654", "#11182a", "#4a5470" }, 260);
            return g.Save("T_Sign58");
        }

        /// <summary>「비상 통로」 안내판. 한글은 블록으로 대신함 — 교체 대상.</summary>
        public static Texture2D HangingSign()
        {
            var g = new Canvas(128, 32, 15);
            g.Fill(Hex("#1f2d55"));
            g.Stroke(1, 1, 126, 30, Hex("#5e6a92"));
            var orange = Hex("#d8682e");
            for (int k = 0; k < 4; k++) // "비상 통로" 자리
            {
                int x = 6 + k * 15 + (k >= 2 ? 6 : 0);
                g.Rect(x, 5, 11, 2, orange); g.Rect(x, 5, 2, 11, orange); g.Rect(x + 6, 9, 5, 2, orange); g.Rect(x, 14, 11, 2, orange);
            }
            g.Rect(8, 22, 50, 2, orange); // "B58 ─ 下"
            g.Rect(74, 5, 20, 22, Hex("#d9d4c4"));
            g.Rect(80, 9, 8, 5, Hex("#1f2d55")); g.Rect(78, 16, 12, 8, Hex("#1f2d55"));
            g.Rect(100, 6, 9, 8, Hex("#d9d4c4")); g.Rect(100, 18, 9, 8, Hex("#d9d4c4"));
            g.Speck(new[] { "#2a3a66", "#141c38" }, 250);
            return g.Save("T_HangingSign");
        }

        /// <summary>컷 전환 줄무늬 (#222 2px / #000 2px).</summary>
        public static Texture2D CutStripes()
        {
            var g = new Canvas(1, 4, 16);
            g.Rect(0, 0, 1, 2, Hex("#222222")); g.Rect(0, 2, 1, 2, Hex("#000000"));
            return g.Save("T_CutStripes");
        }

        /// <summary>"들켰다" 붉은 방사형 그라디언트.</summary>
        public static Texture2D HurtGradient()
        {
            var inner = Rgba(179, 48, 42, .35f);
            var outer = Rgba(80, 0, 0, .95f);
            var t = new Texture2D(64, 48, TextureFormat.RGBA32, false);
            for (int y = 0; y < 48; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = (x + .5f - 32f) / 32f, dy = (y + .5f - 24f) / 24f;
                float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f);
                t.SetPixel(x, y, Color.Lerp(inner, outer, r));
            }
            t.Apply();
            string path = $"{Folder}/T_HurtGradient.png";
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
