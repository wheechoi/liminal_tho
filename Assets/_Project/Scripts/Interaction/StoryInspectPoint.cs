using System;
using System.Collections.Generic;
using Liminal.Camera;
using Liminal.Core;
using Liminal.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Liminal.Interaction
{
    /// <summary>
    /// 이야기용 조사 지점.
    /// - 처음 조사 / 다시 조사할 때 대사를 따로 줄 수 있다 (여러 줄 가능).
    /// - "숨은 대사": 같은 그룹의 조사 지점을 전부 한 번씩 본 뒤 이걸 다시 조사하면 나온다. 플래그를 켠다.
    /// - 클로즈업: 대사가 떠 있는 동안 지정한 구도로 카메라를 바꾼다.
    /// </summary>
    public class StoryInspectPoint : InspectPoint
    {
        static readonly List<StoryInspectPoint> Story = new();

        [Header("대사")]
        public string id;
        [TextArea(2, 4)] public string[] lines;
        [Tooltip("두 번째 조사부터. 비워두면 처음 대사를 반복")]
        [TextArea(2, 4)] public string[] repeatLines;

        [Header("숨은 대사 (같은 그룹을 다 조사한 뒤 다시 조사하면)")]
        public string group = "bedroom";
        public bool hasSecret;
        [TextArea(2, 4)] public string[] secretLines;
        public string secretFlag;
        public UnityEvent onSecretShown = new();
        public UnityEvent onSecretClosed = new();

        [Header("클로즈업 (비워두면 그대로)")]
        public Transform closeUp;
        public float closeUpFov = 32f;
        public ShotRig rig;
        public DialogueBox dialogue;

        public int TimesInspected { get; private set; }

        /// <summary>어느 조사 지점이든 조사되면.</summary>
        public static event Action<StoryInspectPoint> AnyInspected;

        bool showingSecret;

        protected override void OnEnable() { base.OnEnable(); Story.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Story.Remove(this); }

        public override string[] GetLines()
        {
            showingSecret = hasSecret && TimesInspected > 0 && GroupDone(group);
            if (showingSecret) return secretLines;
            if (TimesInspected > 0 && repeatLines != null && repeatLines.Length > 0) return repeatLines;
            return lines;
        }

        public override void OnInspected()
        {
            TimesInspected++;
            if (showingSecret)
            {
                StoryFlags.Set(secretFlag);
                onSecretShown?.Invoke();
            }
            if (closeUp && rig) rig.SetOverride(closeUp, closeUpFov);
            if (dialogue) dialogue.Closed += OnDialogueClosed;
            AnyInspected?.Invoke(this);
        }

        void OnDialogueClosed()
        {
            dialogue.Closed -= OnDialogueClosed;
            if (closeUp && rig) rig.ClearOverride();
            if (showingSecret) onSecretClosed?.Invoke();
            showingSecret = false;
        }

        public static bool GroupDone(string group)
        {
            Count(group, out int seen, out int total);
            return total > 0 && seen == total;
        }

        public static void Count(string group, out int seen, out int total)
        {
            seen = total = 0;
            foreach (var p in Story)
            {
                if (p.group != group) continue;
                total++;
                if (p.TimesInspected > 0) seen++;
            }
        }
    }
}
