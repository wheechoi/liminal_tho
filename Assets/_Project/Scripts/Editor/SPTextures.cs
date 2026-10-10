using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Liminal.Editor
{
    /// <summary>
    /// Surplus Person 임시 텍스처 (절차 생성). 레퍼런스 1(단면 저택 일러스트)의
    /// 줄무늬 벽지 · 흑백 체크 바닥 · 타원 러그 · 짙은 목재 느낌을 낮은 해상도로 흉내낸다.
    /// PNG로 저장하고 Point 필터 / 밉맵 끔 / Repeat 로 임포트. 실제 아트가 오면 같은 이름으로 덮어쓰면 된다.
    /// </summary>
    public static class SPTextures
    {
        public const string Folder = "Assets/_Project/Textures/SP";

        static Color H(string hex) => B58ProcTextures.Hex(hex);

        static float Noise(int x, int y, int seed)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }

        static Color Jitter(Color c, int x, int y, int seed, float amount)
        {
            float n = (Noise(x, y, seed) - 0.5f) * amount;
            return new Color(c.r + n, c.g + n, c.b + n, c.a);
        }

        static Texture2D Make(string name, int w, int h, Func<int, int, Color> px, bool alpha = false)
        {
            Directory.CreateDirectory(Folder);
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var data = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++) data[y * w + x] = px(x, y);
            t.SetPixels(data);
            t.Apply();
            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Default;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = alpha;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>세로 줄무늬 벽지 (크림/베이지, 가는 갈색 선, 작은 마름모 무늬). 32x64 = 0.8m x 1.6m.</summary>
        public static Texture2D Wallpaper() => Make("T_SP_Wallpaper", 32, 64, (x, y) =>
        {
            int sx = x % 16;
            Color c = sx < 8 ? H("#e8d9c2") : H("#d8c3a6");
            if (sx == 0 || sx == 8) c = H("#b0907a");
            int dy = y % 16;
            if (sx >= 3 && sx <= 5 && Mathf.Abs(sx - 4) + Mathf.Abs(dy - 8) <= 1) c = H("#b98f8a");
            return Jitter(c, x, y, 1, 0.03f);
        });

        /// <summary>흑백 체크 바닥. 16x16 = 1m (칸 0.5m).</summary>
        public static Texture2D Checker() => Make("T_SP_Checker", 16, 16, (x, y) =>
        {
            bool dark = (x / 8 + y / 8) % 2 == 0;
            Color c = dark ? H("#2e2624") : H("#eae0cf");
            if (x % 8 == 0 || y % 8 == 0) c = Color.Lerp(c, H("#8a7a6c"), 0.35f);
            return Jitter(c, x, y, 2, dark ? 0.03f : 0.05f);
        });

        /// <summary>짙은 목재 (세로 결).</summary>
        public static Texture2D Wood() => Make("T_SP_Wood", 16, 64, (x, y) =>
        {
            Color c = H("#6a4636");
            if (x % 8 == 0) c = H("#4a2f25");
            float grain = Mathf.Sin(y * 0.4f + x * 1.7f + Noise(x, 0, 3) * 6f);
            c = Color.Lerp(c, H("#5a3a2c"), grain > 0.6f ? 0.6f : 0f);
            return Jitter(c, x, y, 3, 0.04f);
        });

        /// <summary>타원 러그 (먼지 낀 장밋빛, 테두리 무늬, 바깥은 투명).</summary>
        public static Texture2D OvalRug() => Make("T_SP_Rug", 64, 48, (x, y) =>
        {
            float dx = (x + 0.5f - 32f) / 31f, dy = (y + 0.5f - 24f) / 23f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > 1f) return new Color(0, 0, 0, 0);
            Color c = H("#a35a60");
            if (r > 0.86f) c = H("#6c2e35");
            else if (r > 0.78f) c = (x + y) % 4 < 2 ? H("#c98d88") : H("#a35a60");
            else if (r > 0.72f) c = H("#6c2e35");
            else if (Mathf.Abs(r - 0.4f) < 0.04f) c = H("#c98d88");
            return Jitter(c, x, y, 4, 0.05f);
        }, true);

        /// <summary>커튼 (세로 주름).</summary>
        public static Texture2D Curtain() => Make("T_SP_Curtain", 16, 32, (x, y) =>
        {
            float fold = Mathf.Sin(x / 16f * Mathf.PI * 4f);
            Color c = Color.Lerp(H("#9a5d61"), H("#c48a88"), fold * 0.5f + 0.5f);
            return Jitter(c, x, y, 5, 0.03f);
        });

        /// <summary>이불 (분홍 바탕에 작은 꽃점).</summary>
        public static Texture2D Duvet() => Make("T_SP_Duvet", 32, 32, (x, y) =>
        {
            Color c = H("#d7a3a3");
            if ((x % 8 == 3 && y % 8 == 3) || (x % 8 == 7 && y % 8 == 7)) c = H("#f2e2d6");
            if (x % 8 == 4 && y % 8 == 3) c = H("#9fb08a");
            return Jitter(c, x, y, 6, 0.03f);
        });

        /// <summary>바깥 회벽.</summary>
        public static Texture2D Plaster() => Make("T_SP_Plaster", 32, 32, (x, y) => Jitter(H("#efeae0"), x, y, 7, 0.06f));

        /// <summary>잔디.</summary>
        public static Texture2D Grass() => Make("T_SP_Grass", 32, 32, (x, y) =>
        {
            Color c = Noise(x, y, 8) > 0.8f ? H("#9cc070") : H("#7ea85a");
            return Jitter(c, x, y, 9, 0.06f);
        });

        /// <summary>액자 그림: 풍경 (파란 하늘, 초록 언덕).</summary>
        public static Texture2D PaintingLandscape() => Make("T_SP_Painting_Landscape", 32, 24, (x, y) =>
        {
            float hill = 9f + Mathf.Sin(x * 0.25f) * 3f;
            Color c = y < hill ? H("#7a9a5c") : y < hill + 1 ? H("#5e7a48") : H("#a9c8de");
            if (y > 16 && Mathf.Abs(x - 24) + Mathf.Abs(y - 19) < 3) c = H("#f2e6c8");
            return Jitter(c, x, y, 10, 0.06f);
        });

        /// <summary>액자 그림: 초상화 (어두운 배경, 창백한 얼굴 — 얼굴은 일부러 흐리게).</summary>
        public static Texture2D PaintingPortrait() => Make("T_SP_Painting_Portrait", 24, 32, (x, y) =>
        {
            Color c = H("#3a2b28");
            float fx = (x - 12f) / 5f, fy = (y - 19f) / 6.5f;
            if (fx * fx + fy * fy < 1f) c = H("#d9c6b4");
            if (y < 12 && Mathf.Abs(x - 12) < 7 - y * 0.2f) c = H("#5a3f48");
            return Jitter(c, x, y, 11, 0.05f);
        });
    }
}
