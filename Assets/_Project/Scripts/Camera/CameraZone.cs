using System.Collections.Generic;
using Liminal.Core;
using UnityEngine;

namespace Liminal.Camera
{
    /// <summary>
    /// 방 하나 = 트리거 박스 하나 + 고정 카메라 하나.
    /// 플레이어가 들어오면 이 방 카메라로 하드 컷한다.
    /// 시선은 방 중앙과 플레이어 사이를 lookBlend 비율로 보간 (목업 setCam).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CameraZone : MonoBehaviour
    {
        static readonly List<CameraZone> All = new();

        public B58Room room;
        public UnityEngine.Camera shotCamera;
        public Transform player;

        [Header("고정 카메라 시선 (RailCamera가 있으면 무시)")]
        public Vector3 lookCenter;
        [Range(0f, 1f)] public float lookBlend = 0.35f;

        BoxCollider box;
        RailCamera rail;

        void Awake()
        {
            box = GetComponent<BoxCollider>();
            box.isTrigger = true;
            if (shotCamera) rail = shotCamera.GetComponent<RailCamera>();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Start()
        {
            var game = B58Game.Instance;
            if (game != null) game.RoomChanged += OnRoomChanged;
            if (player && box.bounds.Contains(player.position + Vector3.up * 0.5f)) Enter();
        }

        void OnDestroy()
        {
            if (B58Game.Instance != null) B58Game.Instance.RoomChanged -= OnRoomChanged;
        }

        void OnTriggerEnter(Collider other)
        {
            if (player && other.transform == player) Enter();
        }

        void OnTriggerStay(Collider other)
        {
            if (player && other.transform == player && B58Game.Instance && B58Game.Instance.Room != room) Enter();
        }

        void Enter()
        {
            if (B58Game.Instance) B58Game.Instance.EnterRoom(room);
            else OnRoomChanged(room);
        }

        void OnRoomChanged(B58Room r)
        {
            if (shotCamera) shotCamera.enabled = r == room;
            if (r == room) LateUpdate(); // 컷 첫 프레임부터 올바른 구도
        }

        void LateUpdate()
        {
            if (!shotCamera || !shotCamera.enabled || rail || !player) return;
            Vector3 p = player.position;
            var target = new Vector3(
                Mathf.Lerp(lookCenter.x, p.x, lookBlend),
                lookCenter.y,
                Mathf.Lerp(lookCenter.z, p.z, lookBlend));
            shotCamera.transform.LookAt(target);
        }

        /// <summary>현재 켜져 있는 방 카메라.</summary>
        public static UnityEngine.Camera ActiveCamera
        {
            get
            {
                foreach (var z in All)
                    if (z.shotCamera && z.shotCamera.enabled) return z.shotCamera;
                return null;
            }
        }
    }
}
