using System;
using System.Collections.Generic;

namespace Liminal.Core
{
    /// <summary>
    /// 회차를 넘어 남겨야 하는 이야기 플래그 (진엔딩 조건 등).
    /// 지금은 실행 중에만 기억한다. 세이브가 들어오면 Easy Save 3로 저장/불러오기를 붙일 자리.
    /// </summary>
    public static class StoryFlags
    {
        /// <summary>씬 1-1: 침실을 다 둘러본 뒤 알람시계가 6시 56분인 걸 봤다. 첫 번째 진엔딩 선택지 조건.</summary>
        public const string TrueEndClock = "TrueEnd_Clock0656";

        static readonly HashSet<string> Flags = new();

        public static event Action<string> Changed;

        public static bool Has(string flag) => !string.IsNullOrEmpty(flag) && Flags.Contains(flag);

        public static void Set(string flag)
        {
            if (string.IsNullOrEmpty(flag) || !Flags.Add(flag)) return;
            Changed?.Invoke(flag);
        }

        public static void ClearAll() => Flags.Clear();
    }
}
