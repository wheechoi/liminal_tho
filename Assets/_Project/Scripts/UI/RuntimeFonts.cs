using TMPro;
using UnityEngine;

namespace Liminal.UI
{
    /// <summary>
    /// 임시 폰트: 아직 한글 폰트 에셋이 없어서 실행 시 OS 폰트로 TMP 폰트를 만든다.
    /// 교체 대상 — 기획서 10장 폰트(Gowun Batang / Nanum Gothic Coding / VT323)를 에셋으로 넣으면 이 클래스는 지운다.
    /// </summary>
    public static class RuntimeFonts
    {
        static TMP_FontAsset korean, mono, serif;

        public static TMP_FontAsset Korean => korean ??= Create(("Malgun Gothic", "Regular"), ("Gulim", "Regular"));
        public static TMP_FontAsset Mono => mono ??= Create(("Consolas", "Regular"), ("Courier New", "Regular"));
        /// <summary>Surplus Person 대사용 명조 (바탕).</summary>
        public static TMP_FontAsset Serif => serif ??= Create(("Batang", "Regular"), ("BatangChe", "Regular"), ("Malgun Gothic", "Regular"));

        static TMP_FontAsset Create(params (string family, string style)[] candidates)
        {
            foreach (var (family, style) in candidates)
            {
                var f = TMP_FontAsset.CreateFontAsset(family, style, 64);
                if (f == null) continue;
                f.name = $"Runtime {family}";
                if (korean != null && f != korean) f.fallbackFontAssetTable = new() { korean };
                return f;
            }
            Debug.LogWarning("[RuntimeFonts] OS 폰트를 찾지 못했습니다. 기본 TMP 폰트를 씁니다.");
            return TMP_Settings.defaultFontAsset;
        }
    }
}
