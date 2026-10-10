using Febucci.TextAnimatorForUnity;
using Liminal.Core;
using TMPro;
using UnityEngine;

namespace Liminal.UI
{
    /// <summary>
    /// 하단 대사창. Text Animator 타이핑(초당 38자), 조사 키를 누르면 즉시 완성,
    /// 완성 후 한 번 더 누르면 닫힌다. 완성되면 ▼ 표시.
    /// 대사 안에 Text Animator 태그를 쓸 수 있다: &lt;shake&gt; &lt;wiggle&gt; &lt;wave&gt; &lt;dangle&gt; 등 (계속 움직임),
    /// &lt;waitfor=0.6&gt; (잠깐 멈춤), &lt;speed=0.5&gt; (타이핑 속도 배율).
    /// </summary>
    public class DialogueBox : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text text;
        public TypewriterComponent typewriter;
        public GameObject moreIndicator;
        public float charsPerSecond = 38f;
        [Tooltip("모든 글자가 나타날 때 기본으로 쓰는 등장 효과 (Text Animator 태그 ID)")]
        public string[] defaultAppearance = { "fade" };

        public bool IsOpen => panel && panel.activeSelf;

        bool typing;

        void Awake()
        {
            if (typewriter)
            {
                var timings = ScriptableObject.CreateInstance<TypingDelaysByCharacter>();
                timings.waitForNormalChars = 1f / charsPerSecond;
                timings.waitMiddle = 1f / charsPerSecond;
                timings.waitLong = 1f / charsPerSecond;
                typewriter.TimingSettings = timings;
                if (typewriter.TryGetComponent(out TextAnimatorComponentBase anim))
                    anim.localSettings.defaultAppearanceTags = defaultAppearance;
                typewriter.onTextShowed.AddListener(OnTextShowed);
            }
            if (panel) panel.SetActive(false);
        }

        void Start()
        {
            if (B58Game.Instance) B58Game.Instance.CaughtStarted += Close;
        }

        void OnDestroy()
        {
            if (B58Game.Instance) B58Game.Instance.CaughtStarted -= Close;
        }

        public void Show(string line)
        {
            panel.SetActive(true);
            if (moreIndicator) moreIndicator.SetActive(false);
            typing = true;
            if (B58Game.Instance) B58Game.Instance.DialogueOpen = true;
            if (typewriter) typewriter.ShowText(line);
            else text.text = line;
            if (!typewriter) OnTextShowed();
        }

        /// <summary>타이핑 중이면 즉시 완성, 완성됐으면 닫기.</summary>
        public void Advance()
        {
            if (typing && typewriter && typewriter.IsShowingText)
            {
                typewriter.SkipTypewriter();
                OnTextShowed();
                return;
            }
            Close();
        }

        public void Close()
        {
            typing = false;
            if (panel) panel.SetActive(false);
            if (B58Game.Instance) B58Game.Instance.DialogueOpen = false;
        }

        void OnTextShowed()
        {
            typing = false;
            if (moreIndicator) moreIndicator.SetActive(true);
        }
    }
}
