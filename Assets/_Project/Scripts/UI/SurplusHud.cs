using System.Collections;
using Liminal.Camera;
using Liminal.Core;
using Liminal.Interaction;
using Liminal.Player;
using TMPro;
using UnityEngine;

namespace Liminal.UI
{
    /// <summary>
    /// Surplus Person 공용 HUD: [E] 조사 표시, 현재 시점 이름, 개발용 상태줄(조사 수·진엔딩 조건), 검은 화면 페이드.
    /// </summary>
    public class SurplusHud : MonoBehaviour
    {
        public TankController player;
        public DialogueBox dialogue;
        public ShotRig rig;
        public CanvasGroup hudGroup;
        public GameObject prompt;
        public TMP_Text viewLabel;
        public TMP_Text devStatus;
        public CanvasGroup fade;
        public string inspectGroup = "bedroom";

        /// <summary>false면 컷신 중 (HUD 숨김, 조사 표시 안 함).</summary>
        public bool gameplay;

        static readonly string[] ViewNames = { "돌하우스", "구석 고정 카메라", "3인칭" };

        void Start()
        {
            if (rig) rig.ModeChanged += _ => RefreshView();
            RefreshView();
        }

        void Update()
        {
            if (hudGroup) hudGroup.alpha = Mathf.MoveTowards(hudGroup.alpha, gameplay ? 1f : 0f, Time.deltaTime * 2f);
            bool talking = dialogue && dialogue.IsOpen;
            if (prompt) prompt.SetActive(gameplay && !talking && player && player.enabled && player.NearestSpot != null);
            if (devStatus)
            {
                StoryInspectPoint.Count(inspectGroup, out int seen, out int total);
                bool flag = StoryFlags.Has(StoryFlags.TrueEndClock);
                devStatus.text = $"조사 {seen}/{total}   ·   진엔딩 조건(6:56) {(flag ? "<color=#7da36b>확인</color>" : "미확인")}";
            }
        }

        void RefreshView()
        {
            if (viewLabel && rig) viewLabel.text = $"[V] 시점: {ViewNames[(int)rig.mode]}";
        }

        public IEnumerator Fade(float to, float duration)
        {
            if (!fade) yield break;
            float from = fade.alpha;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                fade.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            fade.alpha = to;
        }
    }
}
