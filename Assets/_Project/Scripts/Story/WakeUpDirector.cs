using System;
using System.Collections;
using Liminal.Audio;
using Liminal.Camera;
using Liminal.Player;
using Liminal.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Liminal.Story
{
    /// <summary>
    /// 씬 1-1 "소희가 잠에서 깨어남" 오프닝 연출.
    ///  ① 암전, 생체 모니터 삐 소리 → 알람으로 바뀜
    ///  ② 앵커 컷: 창밖에서 커튼 틈으로 들여다보는 고정 구도 (엔딩 1초 깜빡임에 그대로 재사용)
    ///  ③ 소희가 알람을 끄고 일어나 기지개 → 실내 시점으로 컷, 첫 독백 후 조작 시작
    /// 연출 중 [E]를 누르면 건너뛴다 (개발용).
    /// </summary>
    public class WakeUpDirector : MonoBehaviour
    {
        [Header("Refs")]
        public ShotRig rig;
        public Transform anchorShot;
        public float anchorFov = 38f;
        public TankController player;
        public GirlAnimator animator;
        public MorningAudio sound;
        public AlarmClock clock;
        public SurplusHud hud;
        public DialogueBox dialogue;
        public InputActionReference skip;

        [Header("소희 관절")]
        public Transform pose;   // 엉덩이 높이 회전축
        public Transform hipL, hipR, shoulderL, shoulderR;
        public Transform duvet;
        public Vector3 duvetFoldedScale = new Vector3(1.1f, 0.14f, 1.6f);
        public Vector3 duvetFoldedOffset = new Vector3(0.45f, 0.02f, 0f);

        [Header("위치 (월드)")]
        public Vector3 bedPos = new Vector3(-2.95f, 0.14f, 3.65f);
        public float bedYaw = 90f;
        public Vector3 edgePos = new Vector3(-3.0f, 0.14f, 3.05f);
        public Vector3 standPos = new Vector3(-3.1f, 0f, 2.4f); // 창밖 커튼 틈에서 보이는 자리
        public float standYaw = 180f;

        [Header("첫 독백")]
        [TextArea(2, 4)]
        public string[] monologue =
        {
            "<size=70%><color=#b98f8a>소희</color></size>\n“…이상한 꿈 꿨는데.<waitfor=0.5> 뭐였더라.”",
            "<size=70%><color=#b98f8a>소희</color></size>\n“뭐, 됐어.”",
        };

        public bool Playing { get; private set; }

        const float RestArmZ = 4.58f; // 0.08 rad
        bool skipping;
        float started;

        void Start()
        {
            if (skip != null) skip.action.Enable();
            StartCoroutine(Run());
        }

        void Update()
        {
            if (Playing && !skipping && Time.time - started > 0.5f && skip != null && skip.action.WasPressedThisFrame())
            {
                skipping = true;
                sound.SkipToMorning();
                clock.ringing = false;
            }
        }

        IEnumerator Run()
        {
            Playing = true;
            started = Time.time;

            // 준비: 침대에 누운 자세, 창밖 구도, 검은 화면
            player.enabled = false;
            player.GetComponent<CharacterController>().enabled = false;
            animator.enabled = false;
            hud.gameplay = false;
            if (hud.fade) hud.fade.alpha = 1f;
            rig.lockToggle = true;
            rig.SetOverride(anchorShot, anchorFov);
            sound.SetOutside(true);
            float tilt = -90f, hip = 0f, armL = -RestArmZ, armR = RestArmZ;
            Vector3 pos = bedPos;
            float yaw = bedYaw;
            Vector3 duvetScale = duvet ? duvet.localScale : Vector3.one, duvetPos = duvet ? duvet.localPosition : Vector3.zero;
            void ApplyPose()
            {
                player.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
                pose.localRotation = Quaternion.Euler(tilt, 0f, 0f);
                hipL.localRotation = hipR.localRotation = Quaternion.Euler(hip, 0f, 0f);
                shoulderL.localRotation = Quaternion.Euler(0f, 0f, armL);
                shoulderR.localRotation = Quaternion.Euler(0f, 0f, armR);
            }
            ApplyPose();

            // ① 암전, 소리부터
            sound.PlayIntro();
            yield return Wait(sound.introToAlarm);
            sound.StartAlarm();
            if (!skipping) clock.ringing = true;

            // ② 앵커 컷: 창밖에서 들여다보는 시점. 카메라는 움직이지 않는다.
            yield return Wait(0.3f);
            sound.StartBirds(0.7f);
            yield return Tween(1.6f, t => { if (hud.fade) hud.fade.alpha = 1f - t; });
            yield return Wait(2.2f);

            // ③ 알람을 끈다
            yield return Tween(0.8f, t => { armL = Mathf.Lerp(-RestArmZ, -80f, Smooth(t)); ApplyPose(); });
            sound.StopAlarm();
            clock.ringing = false;
            yield return Wait(0.25f);
            yield return Tween(0.6f, t => { armL = Mathf.Lerp(-80f, -RestArmZ, Smooth(t)); ApplyPose(); });
            yield return Wait(0.6f);

            // 천천히 일어나 앉는다
            yield return Tween(2.0f, t => { tilt = Mathf.Lerp(-90f, 0f, Smooth(t)); hip = Mathf.Lerp(0f, -90f, Smooth(t)); ApplyPose(); });
            yield return Wait(0.4f);

            // 침대 가장자리로 돌아앉으며 이불을 걷는다
            yield return Tween(0.9f, t =>
            {
                float s = Smooth(t);
                yaw = Mathf.Lerp(bedYaw, standYaw, s);
                pos = Vector3.Lerp(bedPos, edgePos, s);
                if (duvet)
                {
                    duvet.localScale = Vector3.Lerp(duvetScale, duvetFoldedScale, s);
                    duvet.localPosition = Vector3.Lerp(duvetPos, duvetPos + duvetFoldedOffset, s);
                }
                ApplyPose();
            });
            yield return Wait(0.2f);

            // 일어선다
            yield return Tween(1.0f, t => { pos = Vector3.Lerp(edgePos, standPos, Smooth(t)); hip = Mathf.Lerp(-90f, 0f, Smooth(t)); ApplyPose(); });
            yield return Wait(0.2f);

            // 기지개
            yield return Tween(1.2f, t =>
            {
                float s = Smooth(t);
                armL = Mathf.Lerp(-RestArmZ, -168f, s);
                armR = Mathf.Lerp(RestArmZ, 168f, s);
                tilt = Mathf.Lerp(0f, -8f, s);
                ApplyPose();
            });
            yield return Wait(0.5f);
            yield return Tween(0.8f, t =>
            {
                float s = Smooth(t);
                armL = Mathf.Lerp(-168f, -RestArmZ, s);
                armR = Mathf.Lerp(168f, RestArmZ, s);
                tilt = Mathf.Lerp(-8f, 0f, s);
                ApplyPose();
            });
            yield return Wait(0.3f);

            // 실내 시점으로 컷 → 첫 독백 → 조작 시작
            if (hud.fade) hud.fade.alpha = 0f;
            rig.ClearOverride();
            sound.SetOutside(false);
            sound.StartBirds(0.45f);
            player.GetComponent<CharacterController>().enabled = true;
            player.Teleport(standPos, standYaw * Mathf.Deg2Rad);
            Playing = false;
            yield return null; // 건너뛰기 키가 대사를 바로 넘기지 않도록 한 프레임 쉰다
            animator.enabled = true;
            player.enabled = true;
            rig.lockToggle = false;
            hud.gameplay = true;
            dialogue.Show(monologue);
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);

        IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds && !skipping; t += Time.deltaTime) yield return null;
        }

        IEnumerator Tween(float seconds, Action<float> step)
        {
            for (float t = 0f; t < seconds && !skipping; t += Time.deltaTime)
            {
                step(t / seconds);
                yield return null;
            }
            step(1f);
        }
    }
}
