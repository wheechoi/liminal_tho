using System.Collections.Generic;
using UnityEngine;

namespace Liminal.Interaction
{
    /// <summary>
    /// 조사 지점. 플레이어가 반경 안에 있으면 [E] 조사가 뜨고 대사가 나온다.
    /// 여러 개가 겹치면 가장 가까운 것 (목업 nearSpot).
    /// </summary>
    public class InspectPoint : MonoBehaviour
    {
        static readonly List<InspectPoint> All = new();

        [TextArea(2, 4)] public string line;
        public float radius = 1f;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static InspectPoint FindNearest(Vector3 pos)
        {
            InspectPoint best = null;
            float bestDist = float.MaxValue;
            foreach (var s in All)
            {
                Vector3 d = s.transform.position - pos;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist < s.radius && dist < bestDist)
                {
                    bestDist = dist;
                    best = s;
                }
            }
            return best;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.79f, 0.55f, 0.17f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
