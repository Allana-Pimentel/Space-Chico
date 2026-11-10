using UnityEngine;

namespace SpaceChico
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector2 offset = new Vector2(0f, 1.6f);
        public float smoothTimeX = 0.15f, smoothTimeY = 0.28f, lookAhead = 2.2f, lookAheadSmooth = 0.5f;
        public float minX = -12f, maxX = 130f, minY = -2f, maxY = 14f;

        static CameraFollow instance;
        Camera cam;
        float velX, velY, shakeTime, shakeAmp;
        Vector3 basePos;

        void Awake()
        {
            instance = this;
            cam = GetComponent<Camera>();
            if (Mathf.Abs(transform.position.z) < 0.1f) transform.position = new Vector3(transform.position.x, transform.position.y, -10f);
            basePos = transform.position;
        }

        public static void Shake(float amplitude, float duration)
        {
            if (instance != null) { instance.shakeAmp = amplitude; instance.shakeTime = duration; }
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            basePos = Clamp(new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z));
            transform.position = basePos;
        }

        void Start() => SnapToTarget();

        void LateUpdate()
        {
            if (target == null) return;
            basePos.x = Mathf.SmoothDamp(basePos.x, target.position.x + offset.x, ref velX, smoothTimeX);
            basePos.y = Mathf.SmoothDamp(basePos.y, target.position.y + offset.y, ref velY, smoothTimeY);
            basePos = Clamp(basePos);

            Vector3 shake = (shakeTime > 0f && (shakeTime -= Time.deltaTime) > 0f) ? (Vector3)(Random.insideUnitCircle * shakeAmp) : Vector3.zero;
            transform.position = basePos + shake;
        }

        Vector3 Clamp(Vector3 p)
        {
            float h = cam != null ? cam.orthographicSize : 6f;
            float w = h * (cam != null ? cam.aspect : 16f / 9f);
            p.x = Mathf.Clamp(p.x, minX + w, Mathf.Max(minX + w, maxX - w));
            p.y = Mathf.Clamp(p.y, minY + h, Mathf.Max(minY + h, maxY - h));
            return p;
        }
    }
}
