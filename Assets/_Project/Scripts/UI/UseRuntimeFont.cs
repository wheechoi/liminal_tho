using TMPro;
using UnityEngine;

namespace Liminal.UI
{
    /// <summary>이 오브젝트의 TMP 텍스트에 실행 시 OS 폰트를 적용.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class UseRuntimeFont : MonoBehaviour
    {
        public enum Kind { Korean, Mono }
        public Kind kind = Kind.Korean;

        void Awake()
        {
            var t = GetComponent<TMP_Text>();
            if (kind == Kind.Mono) _ = RuntimeFonts.Korean; // 모노 폰트의 한글 대체용
            t.font = kind == Kind.Korean ? RuntimeFonts.Korean : RuntimeFonts.Mono;
        }
    }
}
