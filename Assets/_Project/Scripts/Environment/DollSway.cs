using UnityEngine;

namespace Liminal.Environment
{
    /// <summary>매달린 인형이 미세하게 흔들린다 (z축, 라디안 진폭).</summary>
    public class DollSway : MonoBehaviour
    {
        public float phase;
        public float amplitude = 0.04f;
        public float speed = 0.8f;

        float baseYaw;

        void Start() => baseYaw = transform.localEulerAngles.y;

        void Update()
        {
            float z = Mathf.Sin(Time.time * speed + phase) * amplitude * Mathf.Rad2Deg;
            transform.localRotation = Quaternion.Euler(0f, baseYaw, z);
        }
    }
}
