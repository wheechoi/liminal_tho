using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Liminal.Camera
{
    public enum ViewMode { Dollhouse = 0, FixedCorner = 1, ThirdPerson = 2 }

    /// <summary>
    /// 게임 카메라 1대로 여러 시점을 시험해 보는 장치. [V]로 순환.
    ///  - Dollhouse: 위에서 비스듬히 내려다보는 단면도 (앞쪽 벽·천장을 숨김, 거의 직교처럼 보이는 좁은 화각)
    ///  - FixedCorner: 방 구석 고정 카메라 (원근)
    ///  - ThirdPerson: 등 뒤를 따라가는 카메라
    /// 컷신·클로즈업은 SetOverride로 구도를 잠시 덮어쓴다.
    /// </summary>
    public class ShotRig : MonoBehaviour
    {
        public UnityEngine.Camera cam;
        public Transform player;
        public InputActionReference toggleView;
        public ViewMode mode = ViewMode.Dollhouse;

        [Header("돌하우스")]
        public Transform dollhouseShot;
        [Tooltip("멀리서 좁은 화각으로 찍어 거의 직교처럼 보이게 (직교 투영은 URP에서 방 조명이 잘 안 먹어서 사용 안 함)")]
        public float dollhouseFov = 21f;
        [Tooltip("돌하우스 시점에서 숨길 것 (카메라 쪽 벽, 천장, 바깥 풍경)")]
        public GameObject[] cutawayHide;
        [Tooltip("돌하우스 시점 배경색 (레퍼런스처럼 어두운 바탕에 집이 떠 있게)")]
        public Color dollhouseBackground = new Color(0.1f, 0.08f, 0.08f);
        [Tooltip("그 외 시점 배경색 (창밖 하늘)")]
        public Color skyBackground = new Color(0.6f, 0.8f, 0.94f);

        [Header("구석 고정 카메라")]
        public Transform cornerShot;
        public float cornerFov = 64f;

        [Header("3인칭")]
        public float tpDistance = 2.6f;
        public float tpHeight = 1.9f;
        public float tpLookHeight = 1.0f;
        public float tpFov = 58f;
        public float tpSmooth = 8f;

        /// <summary>컷신 중에는 시점 전환 키를 막는다.</summary>
        public bool lockToggle;

        public event Action<ViewMode> ModeChanged;

        Transform overrideShot;
        float overrideFov;
        Vector3 tpPos;
        bool tpSnap = true;

        void OnEnable() { if (toggleView != null) toggleView.action.Enable(); }

        public void SetOverride(Transform shot, float fov)
        {
            overrideShot = shot;
            overrideFov = fov;
            Apply();
        }

        public void ClearOverride()
        {
            overrideShot = null;
            tpSnap = true;
            Apply();
        }

        public void SetMode(ViewMode m)
        {
            mode = m;
            tpSnap = true;
            Apply();
            ModeChanged?.Invoke(mode);
        }

        void Update()
        {
            if (!lockToggle && toggleView != null && toggleView.action.WasPressedThisFrame())
                SetMode((ViewMode)(((int)mode + 1) % 3));
        }

        void LateUpdate() => Apply();

        void Apply()
        {
            if (!cam) return;
            bool cutaway = overrideShot == null && mode == ViewMode.Dollhouse;
            foreach (var g in cutawayHide)
                if (g && g.activeSelf == cutaway) g.SetActive(!cutaway);
            cam.backgroundColor = cutaway ? dollhouseBackground : skyBackground;

            if (overrideShot)
            {
                Place(overrideShot.position, overrideShot.rotation, overrideFov);
                return;
            }
            switch (mode)
            {
                case ViewMode.Dollhouse:
                    if (dollhouseShot) Place(dollhouseShot.position, dollhouseShot.rotation, dollhouseFov);
                    break;
                case ViewMode.FixedCorner:
                    if (cornerShot) Place(cornerShot.position, cornerShot.rotation, cornerFov);
                    break;
                case ViewMode.ThirdPerson:
                    ThirdPerson();
                    break;
            }
        }

        void ThirdPerson()
        {
            if (!player) return;
            Vector3 head = player.position + Vector3.up * tpLookHeight;
            Vector3 want = player.position - player.forward * tpDistance + Vector3.up * tpHeight;
            // 벽 뒤로 빠지지 않게
            Vector3 dir = want - head;
            if (Physics.SphereCast(head, 0.15f, dir.normalized, out var hit, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
                want = head + dir.normalized * Mathf.Max(hit.distance - 0.05f, 0.3f);
            tpPos = tpSnap ? want : Vector3.Lerp(tpPos, want, 1f - Mathf.Exp(-tpSmooth * Time.deltaTime));
            tpSnap = false;
            Place(tpPos, Quaternion.LookRotation(head + player.forward * 0.8f - tpPos), tpFov);
        }

        void Place(Vector3 pos, Quaternion rot, float fov)
        {
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = fov;
        }
    }
}
