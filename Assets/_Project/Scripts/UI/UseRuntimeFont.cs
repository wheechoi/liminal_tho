using TMPro;
using UnityEngine;

namespace Liminal.UI
{
    /// <summary>이 오브젝트의 TMP 텍스트에 실행 시 OS 폰트를 적용.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class UseRuntimeFont : MonoBehaviour
    {
        public enum Kind { Korean, Mono, Serif }
        public Kind kind = Kind.Korean;

        void Awake()
        {
            var t = GetComponent<TMP_Text>();
            if (kind != Kind.Korean) _ = RuntimeFonts.Korean; // 다른 폰트의 한글 대체용
            t.font = kind switch
            {
                Kind.Mono => RuntimeFonts.Mono,
                Kind.Serif => RuntimeFonts.Serif,
                _ => RuntimeFonts.Korean,
            };
        }
    }
}
