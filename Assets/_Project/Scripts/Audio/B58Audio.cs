using Lofelt.NiceVibrations;
using Liminal.Core;
using Liminal.Player;
using Liminal.UI;
using UnityEngine;

namespace Liminal.Audio
{
    /// <summary>
    /// 임시 사운드: 목업의 WebAudio 합성을 그대로 옮겨 실행 시 AudioClip을 만든다.
    /// 앰비언트 드론(55/58.3Hz + 갈색 노이즈), 발소리, 심장 '쿵', 잡힘 '쿵'.
    /// 교체 대상 — 실제 사운드 에셋이 오면 clip 필드에 넣으면 합성 대신 그걸 쓴다.
    /// 모바일에서는 Nice Vibrations로 진동도 같이 준다.
    /// </summary>
    public class B58Audio : MonoBehaviour
    {
        [Header("비워두면 합성음 사용")]
        public AudioClip ambientClip;
        public AudioClip thumpClip;
        public AudioClip footstepRoomClip;
        public AudioClip footstepCorridorClip;

        [Range(0f, 1f)] public float masterVolume = 0.7f;
        public bool haptics = true;

        const int Rate = 44100;
        AudioSource loop, oneShot;

        void Awake()
        {
            if (!ambientClip) ambientClip = MakeDrone();
            if (!thumpClip) thumpClip = MakeThump();
            if (!footstepRoomClip) footstepRoomClip = MakeStep(700f, 1);
            if (!footstepCorridorClip) footstepCorridorClip = MakeStep(380f, 2);

            loop = gameObject.AddComponent<AudioSource>();
            loop.clip = ambientClip;
            loop.loop = true;
            loop.volume = masterVolume;
            loop.spatialBlend = 0f;
            loop.Play();

            oneShot = gameObject.AddComponent<AudioSource>();
            oneShot.spatialBlend = 0f;
        }

        void OnEnable()
        {
            GirlAnimator.Footstep += OnFootstep;
            EcgHud.Heartbeat += OnHeartbeat;
        }

        void OnDisable()
        {
            GirlAnimator.Footstep -= OnFootstep;
            EcgHud.Heartbeat -= OnHeartbeat;
            if (B58Game.Instance) B58Game.Instance.CaughtStarted -= OnCaught;
        }

        void Start()
        {
            if (B58Game.Instance) B58Game.Instance.CaughtStarted += OnCaught;
        }

        void OnFootstep(bool running)
        {
            bool corridor = B58Game.Instance && B58Game.Instance.Room == B58Room.Corridor;
            oneShot.PlayOneShot(corridor ? footstepCorridorClip : footstepRoomClip, (running ? 0.5f : 0.3f) * masterVolume);
        }

        void OnHeartbeat(float volume)
        {
            oneShot.PlayOneShot(thumpClip, volume * masterVolume);
            if (haptics) HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
        }

        void OnCaught()
        {
            oneShot.PlayOneShot(thumpClip, 0.9f * masterVolume);
            if (haptics) HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
        }

        // ---------- 합성 ----------

        /// <summary>10초 루프: 55Hz·58.3Hz 톱니파(170Hz 로우패스) + 갈색 노이즈.</summary>
        static AudioClip MakeDrone()
        {
            int n = Rate * 10; // 55×10, 58.3×10 모두 정수 주기 → 이음매 없음
            var d = new float[n];
            float lp = 0f, brown = 0f;
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * 170f / Rate);
            var rng = new System.Random(58);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float saw = Saw(t * 55f) * 0.04f + Saw(t * 58.3f) * 0.04f;
                lp += a * (saw - lp);
                brown = (brown + 0.02f * ((float)rng.NextDouble() * 2f - 1f)) / 1.02f;
                d[i] = lp + brown * 3.5f * 0.22f;
            }
            // 노이즈 이음매 페이드
            int fade = Rate / 20;
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                d[i] = Mathf.Lerp(d[n - fade + i], d[i], k);
            }
            return Clip("B58 Drone", d);
        }

        /// <summary>72Hz → 36Hz 0.16초 하강, 0.26초 감쇠.</summary>
        static AudioClip MakeThump()
        {
            int n = (int)(Rate * 0.3f);
            var d = new float[n];
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float f = 72f * Mathf.Pow(0.5f, Mathf.Min(t, 0.16f) / 0.16f);
                ph += 2f * Mathf.PI * f / Rate;
                float env = t < 0.01f ? t / 0.01f : Mathf.Pow(0.001f, (t - 0.01f) / 0.25f);
                d[i] = Mathf.Sin(ph) * env;
            }
            return Clip("B58 Thump", d);
        }

        /// <summary>대역통과 노이즈 0.09초.</summary>
        static AudioClip MakeStep(float freq, int seed)
        {
            int n = (int)(Rate * 0.1f);
            var d = new float[n];
            var rng = new System.Random(seed);
            // RBJ 밴드패스 (Q≈1)
            float w0 = 2f * Mathf.PI * freq / Rate, alpha = Mathf.Sin(w0) / 2f, cos = Mathf.Cos(w0);
            float b0 = alpha, b2 = -alpha, a0 = 1f + alpha, a1 = -2f * cos, a2 = 1f - alpha;
            float x1 = 0, x2 = 0, y1 = 0, y2 = 0, brown = 0;
            for (int i = 0; i < n; i++)
            {
                brown = (brown + 0.02f * ((float)rng.NextDouble() * 2f - 1f)) / 1.02f;
                float x = brown * 3.5f;
                float y = (b0 * x + b2 * x2 - a1 * y1 - a2 * y2) / a0;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                float t = (float)i / Rate;
                d[i] = y * 4f * Mathf.Pow(0.001f, t / 0.09f);
            }
            return Clip("B58 Step", d);
        }

        static float Saw(float cycles) => 2f * (cycles - Mathf.Floor(cycles + 0.5f));

        static AudioClip Clip(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
