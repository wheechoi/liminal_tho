using Liminal.Core;
using Liminal.Player;
using UnityEngine;

namespace Liminal.Enemy
{
    /// <summary>
    /// "그것": 시선이 닿지 않을 때만 움직인다 (기획서 6장, 목업 수치 그대로).
    /// 1) 복도에서 8m 안에 들어오면 깨어남
    /// 2) 플레이어 정면·(플레이어→그것) 내적 ≥ 0.55 이면 정지, 아니면 1.45m/s로 접근
    /// 3) 0.55m 안이면 잡힘 → 복도 입구에서 재시작, 그것은 원위치
    /// 4) 다른 방으로 나가면 원위치
    /// </summary>
    public class WeepingStalker : MonoBehaviour
    {
        public TankController player;

        [Header("규칙")]
        public Vector3 home = new Vector3(-0.4f, 0f, 21.6f);
        public float wakeDistance = 8f;
        [Tooltip("이 값 이상이면 '보고 있다'로 판정해 정지")]
        public float facingThreshold = 0.55f;
        public float speed = 1.45f;
        public float catchDistance = 0.55f;
        [Tooltip("복도 안에서만 움직이도록 (x, z) 범위")]
        public Vector2 xRange = new Vector2(-0.8f, 0.8f);
        public Vector2 zRange = new Vector2(8.8f, 25.4f);

        [Header("위험도 (ECG): clamp((dangerNear - 거리) / dangerSpan, 0, 1)")]
        public float dangerNear = 6f;
        public float dangerSpan = 4.5f;

        [Header("연출용 파츠")]
        public Transform torso, head, leftArm, rightArm;

        public bool IsAwake { get; private set; }
        public bool Moving { get; private set; }

        float twitchTimer, time;

        void Start()
        {
            var game = B58Game.Instance;
            if (game != null)
            {
                game.RoomChanged += OnRoomChanged;
                game.Respawned += ResetHome;
            }
            ResetHome();
        }

        void OnDestroy()
        {
            var game = B58Game.Instance;
            if (game == null) return;
            game.RoomChanged -= OnRoomChanged;
            game.Respawned -= ResetHome;
        }

        void OnRoomChanged(B58Room r)
        {
            if (r != B58Room.Corridor) ResetHome();
        }

        public void ResetHome()
        {
            transform.position = home;
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            IsAwake = false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            time += dt;
            Moving = false;
            var game = B58Game.Instance;
            if (player == null || game == null) return;

            Vector3 d = player.transform.position - transform.position;
            d.y = 0f;
            float dist = d.magnitude;

            if (game.Room == B58Room.Corridor && !game.Caught)
            {
                if (dist < wakeDistance) IsAwake = true;
                if (IsAwake)
                {
                    Vector3 toThing = dist > 0f ? -d / dist : Vector3.zero;
                    float facing = Vector3.Dot(player.Forward, toThing);

                    float targetYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                    float yaw = Mathf.LerpAngle(transform.eulerAngles.y, targetYaw, 0.2f);
                    transform.rotation = Quaternion.Euler(0f, yaw, 0f);

                    if (facing < facingThreshold && !game.DialogueOpen && dist > 0f)
                    {
                        Vector3 p = transform.position + d / dist * speed * dt;
                        p.x = Mathf.Clamp(p.x, xRange.x, xRange.y);
                        p.z = Mathf.Clamp(p.z, zRange.x, zRange.y);
                        transform.position = p;
                        Moving = true;
                    }

                    if (dist < catchDistance) game.TriggerCaught();
                }
            }

            game.Danger = game.Room == B58Room.Corridor && IsAwake
                ? Mathf.Clamp01((dangerNear - dist) / dangerSpan)
                : 0f;

            Animate(dt);
        }

        void Animate(float dt)
        {
            twitchTimer -= dt;
            if (twitchTimer <= 0f && head)
            {
                twitchTimer = Random.Range(0.15f, 1.6f);
                head.localRotation = Quaternion.Euler(Random.Range(-0.3f, 0.4f) * Mathf.Rad2Deg, 0f, Random.Range(-0.5f, 0.5f) * Mathf.Rad2Deg);
            }
            float crawl = Moving ? Mathf.Sin(time * 9f) : 0f;
            if (leftArm) leftArm.localRotation = Quaternion.Euler((-1.0f + crawl * 0.4f) * Mathf.Rad2Deg, 0f, -0.12f * Mathf.Rad2Deg);
            if (rightArm) rightArm.localRotation = Quaternion.Euler((-1.0f - crawl * 0.4f) * Mathf.Rad2Deg, 0f, 0.12f * Mathf.Rad2Deg);
            if (torso) torso.localRotation = Quaternion.Euler((1.05f + Mathf.Sin(time * 1.3f) * 0.04f) * Mathf.Rad2Deg, 0f, 0f);
        }
    }
}
