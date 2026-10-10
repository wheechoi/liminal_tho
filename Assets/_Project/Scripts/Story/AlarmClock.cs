using TMPro;
using UnityEngine;

namespace Liminal.Story
{
    /// <summary>
    /// 침대 옆 디지털 알람시계. 평소엔 10:34, 숨은 대사가 나오는 동안만 06:56.
    /// 울리는 동안 숫자가 깜빡인다.
    /// </summary>
    public class AlarmClock : MonoBehaviour
    {
        public TMP_Text display;
        public string shownTime = "10:34";
        [Tooltip("진짜 시간. 엔딩에서 정체가 드러나는 생체 모니터와 연결")]
        public string realTime = "06:56";
        public bool ringing;

        bool real;

        public void ShowRealTime() { real = true; Refresh(); }
        public void ShowNormalTime() { real = false; Refresh(); }

        void Start() => Refresh();

        void Update()
        {
            if (!display) return;
            display.enabled = !ringing || Mathf.Repeat(Time.time, 0.5f) < 0.3f;
        }

        void Refresh()
        {
            if (display) display.text = real ? realTime : shownTime;
        }
    }
}
