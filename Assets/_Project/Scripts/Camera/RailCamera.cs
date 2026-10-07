using UnityEngine;

namespace Liminal.Camera
{
    /// <summary>
    /// 복도용 레일 카메라: 플레이어 뒤 2.5m, 높이 1.72에서 z축을 따라가고
    /// 플레이어 앞 3m를 본다 (목업 setCam room===1).
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class RailCamera : MonoBehaviour
    {
        public Transform player;
        public float height = 1.72f;
        public float behind = 2.5f;
        [Tooltip("복도 시작 z보다 뒤로 빠지지 않게")]
        public float minZ = 8.25f;
        public float xFollow = 0.4f;
        public float lookAhead = 3f;
        public float lookHeight = 0.95f;
        public float lookXFollow = 0.5f;

        void LateUpdate()
        {
            if (!player) return;
            Vector3 p = player.position;
            transform.position = new Vector3(p.x * xFollow, height, Mathf.Max(p.z - behind, minZ));
            transform.LookAt(new Vector3(p.x * lookXFollow, lookHeight, p.z + lookAhead));
        }
    }
}
