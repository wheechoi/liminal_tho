using UnityEngine;

namespace Liminal.Rendering
{
    /// <summary>
    /// PS1 셰이더 전역 값 (앰비언트, 정점 스냅 격자). 에디터에서도 바로 보이도록 ExecuteAlways.
    /// 320×240 저해상도 출력 자체는 카메라 targetTexture(RenderTexture 에셋, Point 필터) + 화면 RawImage로 한다.
    /// </summary>
    [ExecuteAlways]
    public class PS1Settings : MonoBehaviour
    {
        [Tooltip("목업 AmbientLight(0x2a2622, 0.55)")]
        public Color ambient = new Color(0x2a / 255f * 0.55f, 0x26 / 255f * 0.55f, 0x22 / 255f * 0.55f);

        static readonly int AmbientId = Shader.PropertyToID("_LiminalAmbient");

        void OnEnable() => Apply();
        void OnValidate() => Apply();
        void Update() { if (!Application.isPlaying) Apply(); }

        void Apply() => Shader.SetGlobalColor(AmbientId, ambient);
    }
}
