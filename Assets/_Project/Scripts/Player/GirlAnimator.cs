using System;
using UnityEngine;

namespace Liminal.Player
{
    /// <summary>
    /// 목업과 같은 절차적 걷기: 다리·팔을 사인파로 흔들고 몸을 살짝 튕긴다.
    /// 나중에 실제 애니메이션(Animator)으로 교체할 자리.
    /// </summary>
    public class GirlAnimator : MonoBehaviour
    {
        public TankController controller;
        [Tooltip("위아래로 튕길 몸통 루트 (CharacterController 루트가 아닌 자식)")]
        public Transform body;
        public Transform leftHip, rightHip, leftShoulder, rightShoulder;

        [Header("목업 수치")]
        public float phasePerMeter = 5.2f;
        public float walkStepInterval = 0.45f;
        public float runStepInterval = 0.30f;

        /// <summary>발소리 이벤트 (달리기 여부).</summary>
        public static event Action<bool> Footstep;

        float phase, stepTimer, time;
        float leftShoulderZ, rightShoulderZ;

        Vector3 bodyBase;

        void Start()
        {
            if (body) bodyBase = body.localPosition;
            if (leftShoulder) leftShoulderZ = leftShoulder.localEulerAngles.z;
            if (rightShoulder) rightShoulderZ = rightShoulder.localEulerAngles.z;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            time += dt;
            float moving = controller ? controller.CurrentSpeed : 0f;

            if (moving > 0f)
            {
                phase += dt * moving * phasePerMeter;
                stepTimer -= dt;
                if (stepTimer <= 0f)
                {
                    bool running = moving > 2f;
                    stepTimer = running ? runStepInterval : walkStepInterval;
                    Footstep?.Invoke(running);
                }
            }
            else
            {
                phase = Mathf.Lerp(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, 0.15f);
            }

            float sw = Mathf.Sin(phase) * (moving > 0f ? Mathf.Min(moving / 2.4f, 1f) : 0f);
            SetX(leftHip, sw * 0.7f, 0f);
            SetX(rightHip, -sw * 0.7f, 0f);
            SetX(leftShoulder, -sw * 0.5f, leftShoulderZ);
            SetX(rightShoulder, sw * 0.5f, rightShoulderZ);

            if (body)
            {
                float y = moving > 0f ? Mathf.Abs(Mathf.Cos(phase)) * 0.025f : Mathf.Sin(time * 2f) * 0.004f;
                body.localPosition = bodyBase + new Vector3(0f, y, 0f);
            }
        }

        static void SetX(Transform t, float rad, float zDeg)
        {
            if (t) t.localRotation = Quaternion.Euler(rad * Mathf.Rad2Deg, 0f, zDeg);
        }
    }
}
