using Liminal.Core;
using UnityEngine;

namespace Liminal.UI
{
    /// <summary>
    /// 방 전환 시 0.12초 노이즈 컷, 잡혔을 때 붉은 "들켰다" 화면, 재시작 시 0.15초 컷.
    /// </summary>
    public class ScreenFx : MonoBehaviour
    {
        public CanvasGroup cut;
        public CanvasGroup hurt;
        public float roomCut = 0.12f;
        public float respawnCut = 0.15f;
        [Tooltip("붉은 화면 페이드 속도 (목업 CSS transition .2s)")]
        public float hurtFade = 0.2f;

        float cutTimer;

        void Start()
        {
            var game = B58Game.Instance;
            if (game == null) return;
            game.RoomChanged += OnRoomChanged;
            game.Respawned += OnRespawned;
            if (cut) cut.alpha = 0f;
            if (hurt) hurt.alpha = 0f;
        }

        void OnDestroy()
        {
            var game = B58Game.Instance;
            if (game == null) return;
            game.RoomChanged -= OnRoomChanged;
            game.Respawned -= OnRespawned;
        }

        bool firstRoom = true;
        void OnRoomChanged(B58Room _)
        {
            if (firstRoom) { firstRoom = false; return; } // 시작 순간은 컷 없음
            cutTimer = roomCut;
        }

        void OnRespawned() => cutTimer = respawnCut;

        void Update()
        {
            cutTimer -= Time.deltaTime;
            if (cut) cut.alpha = cutTimer > 0f ? 1f : 0f;
            if (hurt)
            {
                bool on = B58Game.Instance && B58Game.Instance.Caught;
                hurt.alpha = Mathf.MoveTowards(hurt.alpha, on ? 1f : 0f, Time.deltaTime / hurtFade);
            }
        }
    }
}
