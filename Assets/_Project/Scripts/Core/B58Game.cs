using System;
using UnityEngine;

namespace Liminal.Core
{
    public enum B58Room { Kitchen = 0, Corridor = 1, DollRoom = 2 }

    /// <summary>
    /// 슬라이스 전체 상태: 현재 방, 대사 중인지, 잡힌 상태인지.
    /// 다른 컴포넌트는 여기만 보고 판단한다.
    /// </summary>
    public class B58Game : MonoBehaviour
    {
        public static B58Game Instance { get; private set; }

        public static readonly string[] RoomNames = { "부엌", "서쪽 통로", "인형의 방" };

        [Tooltip("잡힌 뒤 붉은 화면 유지 시간 (초)")]
        public float caughtDuration = 1.8f;
        [Tooltip("잡힌 뒤 재시작 위치 (복도 입구)")]
        public Vector3 respawnPosition = new Vector3(0f, 0f, 8.9f);
        public float respawnYaw = 0f;

        public B58Room Room { get; private set; } = B58Room.Kitchen;
        public bool DialogueOpen { get; set; }
        public bool Caught => caughtTimer > 0f;
        /// <summary>0~1. ECG HUD와 심장 소리가 쓴다.</summary>
        public float Danger { get; set; }

        public event Action<B58Room> RoomChanged;
        public event Action CaughtStarted;
        public event Action Respawned;

        float caughtTimer;
        bool roomInitialized;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void EnterRoom(B58Room room)
        {
            if (roomInitialized && room == Room) return;
            roomInitialized = true;
            Room = room;
            RoomChanged?.Invoke(room);
        }

        public void TriggerCaught()
        {
            if (Caught) return;
            caughtTimer = caughtDuration;
            DialogueOpen = false;
            CaughtStarted?.Invoke();
        }

        void Update()
        {
            if (caughtTimer <= 0f) return;
            caughtTimer -= Time.deltaTime;
            if (caughtTimer <= 0f) Respawned?.Invoke();
        }

        /// <summary>대사·잡힘 상태가 아니어서 플레이어가 움직일 수 있는가.</summary>
        public bool PlayerCanAct => !DialogueOpen && !Caught;
    }
}
