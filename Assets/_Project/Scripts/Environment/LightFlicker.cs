using UnityEngine;

namespace Liminal.Environment
{
    /// <summary>통로 주황 조명: 프레임마다 4% 확률로 어두워진다.</summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        [Range(0f, 1f)] public float chance = 0.04f;
        [Tooltip("깜빡일 때 원래 밝기 대비 비율 (목업 0.15 / 0.9)")]
        public float dimRatio = 0.15f / 0.9f;

        Light lamp;
        float baseIntensity;

        void Awake()
        {
            lamp = GetComponent<Light>();
            baseIntensity = lamp.intensity;
        }

        void Update() => lamp.intensity = Random.value < chance ? baseIntensity * dimRatio : baseIntensity;
    }
}
