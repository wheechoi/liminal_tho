using Liminal.Core;
using Liminal.Interaction;
using Liminal.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Liminal.Player
{
    /// <summary>
    /// 탱크 컨트롤: 위/아래 = 전진/후진, 좌/우 = 제자리 회전, Q = 180° 퀵턴.
    /// 수치는 목업(B58_mockup.html) 그대로.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class TankController : MonoBehaviour
    {
        [Header("Input (InputSystem_Actions / Tank 맵)")]
        public InputActionReference move;
        public InputActionReference run;
        public InputActionReference interact;
        public InputActionReference quickTurn;

        [Header("Speed (m/s, rad/s)")]
        public float walkSpeed = 1.7f;
        public float runSpeed = 3.0f;
        public float backSpeed = 0.9f;
        public float turnSpeed = 2.5f;
        public float quickTurnSpeed = 10f;

        [Header("Refs")]
        public DialogueBox dialogue;

        /// <summary>현재 이동 속도의 절댓값 (애니메이션·발소리용).</summary>
        public float CurrentSpeed { get; private set; }
        /// <summary>정면 방향 (y=0).</summary>
        public Vector3 Forward => transform.forward;
        public InspectPoint NearestSpot { get; private set; }

        CharacterController cc;
        float quickTurnLeft;

        void Awake() => cc = GetComponent<CharacterController>();

        void OnEnable()
        {
            foreach (var a in new[] { move, run, interact, quickTurn })
                if (a != null) a.action.Enable();
            if (B58Game.Instance != null) B58Game.Instance.Respawned += OnRespawned;
        }

        void OnDisable()
        {
            if (B58Game.Instance != null) B58Game.Instance.Respawned -= OnRespawned;
        }

        void Start()
        {
            // B58Game.Awake가 먼저 돌았는지 보장할 수 없어 Start에서 한 번 더 구독
            if (B58Game.Instance != null)
            {
                B58Game.Instance.Respawned -= OnRespawned;
                B58Game.Instance.Respawned += OnRespawned;
            }
        }

        void Update()
        {
            var game = B58Game.Instance;
            bool canAct = game == null || game.PlayerCanAct;
            float dt = Time.deltaTime;

            if (interact != null && interact.action.WasPressedThisFrame() && (game == null || !game.Caught))
                OnInteract();

            CurrentSpeed = 0f;
            if (canAct)
            {
                if (quickTurn != null && quickTurn.action.WasPressedThisFrame() && quickTurnLeft <= 0f)
                    quickTurnLeft = Mathf.PI;

                Vector2 m = move != null ? move.action.ReadValue<Vector2>() : Vector2.zero;
                float yaw = transform.eulerAngles.y * Mathf.Deg2Rad;

                if (quickTurnLeft > 0f)
                {
                    float s = Mathf.Min(quickTurnLeft, dt * quickTurnSpeed);
                    yaw -= s; // 목업과 같은 방향(왼쪽)으로 돈다
                    quickTurnLeft -= s;
                }
                else if (Mathf.Abs(m.x) > 0.3f)
                {
                    yaw += Mathf.Sign(m.x) * turnSpeed * dt;
                }
                transform.rotation = Quaternion.Euler(0f, yaw * Mathf.Rad2Deg, 0f);

                float sp = 0f;
                if (m.y > 0.3f) sp = run != null && run.action.IsPressed() ? runSpeed : walkSpeed;
                else if (m.y < -0.3f) sp = -backSpeed;

                if (sp != 0f)
                {
                    Vector3 before = transform.position;
                    cc.Move(transform.forward * sp * dt + Vector3.down * 0.5f * dt);
                    Vector3 delta = transform.position - before;
                    delta.y = 0f;
                    CurrentSpeed = delta.magnitude / Mathf.Max(dt, 1e-5f) > 0.05f ? Mathf.Abs(sp) : 0f;
                }
            }

            NearestSpot = canAct ? InspectPoint.FindNearest(transform.position) : null;
        }

        void OnInteract()
        {
            if (dialogue == null) return;
            if (dialogue.IsOpen) { dialogue.Advance(); return; }
            var spot = InspectPoint.FindNearest(transform.position);
            if (spot != null) dialogue.Show(spot.line);
        }

        void OnRespawned()
        {
            var game = B58Game.Instance;
            Teleport(game.respawnPosition, game.respawnYaw);
            quickTurnLeft = 0f;
        }

        public void Teleport(Vector3 pos, float yawRad)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yawRad * Mathf.Rad2Deg, 0f));
            cc.enabled = true;
        }
    }
}
