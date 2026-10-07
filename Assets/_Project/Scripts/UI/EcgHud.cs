using System;
using Liminal.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Liminal.UI
{
    /// <summary>
    /// 우상단 심전도. 위험도(B58Game.Danger)에 따라 심박 60 + 위험도×90 BPM,
    /// &lt; 0.2 FINE / 0.2~0.6 CAUTION / &gt; 0.6 DANGER. 목업 drawECG 그대로.
    /// </summary>
    public class EcgHud : MonoBehaviour
    {
        public RawImage graph;
        public TMP_Text label;

        public Color fine = new Color32(0x8f, 0xa3, 0x7a, 0xff);
        public Color caution = new Color32(0xc9, 0x8b, 0x2c, 0xff);
        public Color danger = new Color32(0xb3, 0x30, 0x2a, 0xff);

        [Tooltip("이 위험도 이상이면 박동마다 '쿵'")]
        public float thumpThreshold = 0.15f;

        /// <summary>심장 박동 소리 이벤트 (볼륨).</summary>
        public static event Action<float> Heartbeat;

        const int W = 60, H = 18;
        readonly float[] buffer = new float[W];
        Texture2D tex;
        Color32[] pixels;
        float phase;
        int lastBeat;

        void Awake()
        {
            tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            pixels = new Color32[W * H];
            for (int i = 0; i < W; i++) buffer[i] = 9f;
            if (graph) graph.texture = tex;
        }

        void OnDestroy() { if (tex) Destroy(tex); }

        void Update()
        {
            float d = B58Game.Instance ? B58Game.Instance.Danger : 0f;
            float dt = Time.deltaTime;

            float bpm = 60f + d * 90f;
            phase += dt * bpm / 60f;
            float f = phase % 1f;
            int beat = Mathf.FloorToInt(phase);
            if (beat != lastBeat)
            {
                lastBeat = beat;
                if (d > thumpThreshold) Heartbeat?.Invoke(0.15f + d * 0.5f);
            }

            float y = 9f;
            if (f < 0.04f) y = 9f - f * 150f;
            else if (f < 0.08f) y = 3f + (f - 0.04f) * 300f;
            else if (f < 0.11f) y = 15f - (f - 0.08f) * 200f;
            Array.Copy(buffer, 1, buffer, 0, W - 1);
            buffer[W - 1] = y;

            Color col = d > 0.6f ? danger : d > 0.2f ? caution : fine;
            Array.Clear(pixels, 0, pixels.Length);
            for (int i = 0; i < W; i++)
            {
                int py = H - 1 - Mathf.Clamp(Mathf.RoundToInt(buffer[i]), 0, H - 1); // 캔버스 y(아래로) → 텍스처 y(위로)
                var c = col;
                c.a = Mathf.Clamp01(0.25f + i / 80f);
                pixels[py * W + i] = c;
            }
            tex.SetPixels32(pixels);
            tex.Apply(false);

            if (label)
            {
                string lab = d > 0.6f ? "DANGER" : d > 0.2f ? "CAUTION" : "FINE";
                if (label.text != lab) label.text = lab;
                label.color = col;
            }
        }
    }
}
