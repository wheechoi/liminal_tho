using UnityEngine;

namespace Liminal.Environment
{
    /// <summary>인형의 방 오른쪽 벽의 눈알들: 홍채가 항상 소녀를 따라 굴러간다.</summary>
    public class EyeWall : MonoBehaviour
    {
        public Transform target;
        [Tooltip("소녀의 눈높이")]
        public float targetHeight = 1.2f;
        public Transform[] irises;
        public float[] sizes;

        void LateUpdate()
        {
            if (!target || irises == null) return;
            Vector3 t = target.position;
            for (int i = 0; i < irises.Length; i++)
            {
                var iris = irises[i];
                if (!iris) continue;
                Vector3 eye = iris.parent.position;
                Vector3 d = new Vector3(t.x - eye.x, targetHeight - eye.y, t.z - eye.z);
                float l = d.magnitude;
                if (l < 1e-4f) l = 1f;
                float sz = sizes[i];
                iris.localPosition = new Vector3(-sz * 0.5f, d.y / l * sz * 0.32f, d.z / l * sz * 0.32f);
            }
        }
    }
}
