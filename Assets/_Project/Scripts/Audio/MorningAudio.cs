using Liminal.Player;
using UnityEngine;

namespace Liminal.Audio
{
    /// <summary>
    /// 씬 1-1 임시 사운드 (실행 시 합성). 실제 에셋이 오면 clip 칸에 넣으면 그걸 쓴다.
    ///  - 인트로: 생체 모니터 "삐… 삐…"가 점점 빨라지고 높아져 알람 소리로 넘어간다.
    ///    (이 삐 소리는 1장 두통·5장 집·엔딩에서 다시 쓰이는 복선)
    ///  - 알람: "삐비비빅" 반복. 창밖 시점일 때는 유리 너머처럼 먹먹하게.
    ///  - 아침: 새소리 + 아주 작은 방 소음. 발소리.
    /// </summary>
    public class MorningAudio : MonoBehaviour
    {
        [Header("비워두면 합성음 사용")]
        public AudioClip introClip;
        public AudioClip alarmClip;
        public AudioClip birdsClip;
        public AudioClip clickClip;
        public AudioClip stepClip;

        [Range(0f, 1f)] public float masterVolume = 0.8f;
        [Tooltip("인트로 클립에서 알람 루프로 넘어가는 시점 (초)")]
        public float introToAlarm = 3.95f;

        const int Rate = 44100;
        AudioSource intro, alarm, birds, oneShot;
        AudioLowPassFilter alarmLpf, birdsLpf;
        bool morning; // 알람이 이미 꺼졌으면 다시 켜지지 않게

        void Awake()
        {
            if (!introClip) introClip = MakeIntro();
            if (!alarmClip) alarmClip = MakeAlarm();
            if (!birdsClip) birdsClip = MakeBirds();
            if (!clickClip) clickClip = MakeClick();
            if (!stepClip) stepClip = MakeStep();

            intro = Source(gameObject, introClip, false);
            oneShot = Source(gameObject, null, false);
            (alarm, alarmLpf) = FilteredChild("Alarm", alarmClip);
            (birds, birdsLpf) = FilteredChild("Birds", birdsClip);
        }

        void OnEnable() => GirlAnimator.Footstep += OnFootstep;
        void OnDisable() => GirlAnimator.Footstep -= OnFootstep;

        AudioSource Source(GameObject g, AudioClip c, bool loop)
        {
            var s = g.AddComponent<AudioSource>();
            s.clip = c;
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = masterVolume;
            return s;
        }

        (AudioSource, AudioLowPassFilter) FilteredChild(string name, AudioClip c)
        {
            var g = new GameObject(name);
            g.transform.SetParent(transform, false);
            var s = Source(g, c, true);
            var f = g.AddComponent<AudioLowPassFilter>();
            f.cutoffFrequency = 22000f;
            return (s, f);
        }

        // ---------- 연출에서 부르는 것 ----------

        public void PlayIntro() => intro.Play();

        public void StartAlarm()
        {
            if (morning) return;
            alarm.volume = masterVolume * 0.75f;
            alarm.Play();
        }

        public void StopAlarm()
        {
            if (!morning) oneShot.PlayOneShot(clickClip, masterVolume);
            morning = true;
            alarm.Stop();
        }

        public void StartBirds(float volume = 0.6f)
        {
            birds.volume = masterVolume * volume;
            if (!birds.isPlaying) birds.Play();
        }

        /// <summary>창밖 시점이면 유리 너머처럼 먹먹하게 (알람), 실내면 새소리가 먹먹하게.</summary>
        public void SetOutside(bool outside)
        {
            alarmLpf.cutoffFrequency = outside ? 1400f : 22000f;
            birdsLpf.cutoffFrequency = outside ? 22000f : 2600f;
            birds.volume = masterVolume * (outside ? 0.7f : 0.45f);
        }

        /// <summary>건너뛰기: 바로 아침 상태로.</summary>
        public void SkipToMorning()
        {
            intro.Stop();
            morning = true;
            alarm.Stop();
            StartBirds();
            SetOutside(false);
        }

        void OnFootstep(bool running) => oneShot.PlayOneShot(stepClip, (running ? 0.5f : 0.32f) * masterVolume);

        // ---------- 합성 ----------

        /// <summary>
        /// 0~2.7초: 960Hz 사인 "삐"를 0.9초 간격으로 (생체 모니터).
        /// 그 뒤 간격이 줄고 음이 올라가며 각진 소리로 바뀌어 알람(2kHz)과 이어진다.
        /// </summary>
        static AudioClip MakeIntro()
        {
            (float t, float f, float sq)[] beeps =
            {
                (0.0f, 960f, 0f), (0.9f, 960f, 0f), (1.8f, 960f, 0f), (2.65f, 1080f, 0.15f),
                (3.2f, 1400f, 0.45f), (3.55f, 1750f, 0.75f), (3.78f, 2000f, 1f),
            };
            var d = new float[(int)(Rate * 4.2f)];
            foreach (var b in beeps) Beep(d, b.t, b.f, b.sq, 0.13f, 0.32f);
            return Clip("SP Intro Monitor", d);
        }

        /// <summary>1.1초 루프: 2kHz 짧은 삐 네 번 + 쉼.</summary>
        static AudioClip MakeAlarm()
        {
            var d = new float[(int)(Rate * 1.1f)];
            for (int i = 0; i < 4; i++) Beep(d, i * 0.13f, 2000f, 1f, 0.075f, 0.28f);
            return Clip("SP Alarm", d);
        }

        /// <summary>사인↔각진 소리를 섞은 짧은 삐 (부드러운 시작·끝).</summary>
        static void Beep(float[] d, float start, float freq, float square, float dur, float amp)
        {
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            for (int i = 0; i < n && s0 + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float ph = 2f * Mathf.PI * freq * t;
                float sine = Mathf.Sin(ph);
                float sq = Mathf.Sin(ph) + Mathf.Sin(3 * ph) / 3f + Mathf.Sin(5 * ph) / 5f; // 홀수 배음만 (부드러운 사각파)
                float env = Mathf.Min(1f, t / 0.004f) * Mathf.Min(1f, (dur - t) / 0.02f);
                d[s0 + i] += Mathf.Lerp(sine, sq * 0.8f, square) * env * amp;
            }
        }

        /// <summary>12초 루프: 띄엄띄엄 새 지저귐 + 아주 작은 방 소음.</summary>
        static AudioClip MakeBirds()
        {
            int n = Rate * 12;
            var d = new float[n];
            var rng = new System.Random(11);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            float brown = 0f;
            for (int i = 0; i < n; i++)
            {
                brown = (brown + 0.02f * R(-1f, 1f)) / 1.02f;
                d[i] = brown * 0.12f;
            }
            for (float t = 0.3f; t < 11.2f; t += R(0.9f, 2.2f))
            {
                int notes = rng.Next(2, 6);
                float f0 = R(2800f, 4200f), dir = R(-1f, 1f) > 0 ? 1f : -1f;
                for (int k = 0; k < notes; k++)
                {
                    float st = t + k * R(0.08f, 0.14f), dur = R(0.05f, 0.1f);
                    int s0 = (int)(st * Rate), len = (int)(dur * Rate);
                    float ph = 0f;
                    for (int i = 0; i < len && s0 + i < n; i++)
                    {
                        float u = (float)i / len;
                        float f = f0 * (1f + dir * 0.35f * u) + 300f * Mathf.Sin(u * 40f);
                        ph += 2f * Mathf.PI * f / Rate;
                        d[s0 + i] += Mathf.Sin(ph) * Mathf.Sin(Mathf.PI * u) * 0.09f;
                    }
                }
            }
            int fade = Rate / 10;
            for (int i = 0; i < fade; i++) d[i] = Mathf.Lerp(d[n - fade + i], d[i], (float)i / fade);
            return Clip("SP Birds", d);
        }

        static AudioClip MakeClick()
        {
            var d = new float[(int)(Rate * 0.04f)];
            var rng = new System.Random(3);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] = ((float)rng.NextDouble() * 2f - 1f) * 0.5f * Mathf.Exp(-t * 300f)
                     + Mathf.Sin(2f * Mathf.PI * 3200f * t) * 0.3f * Mathf.Exp(-t * 200f);
            }
            return Clip("SP Click", d);
        }

        /// <summary>실내 슬리퍼 발소리: 낮게 깎은 노이즈 0.07초.</summary>
        static AudioClip MakeStep()
        {
            var d = new float[(int)(Rate * 0.08f)];
            var rng = new System.Random(5);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                lp += 0.08f * (((float)rng.NextDouble() * 2f - 1f) - lp);
                d[i] = lp * 2.5f * Mathf.Exp(-t * 45f);
            }
            return Clip("SP Step", d);
        }

        static AudioClip Clip(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
