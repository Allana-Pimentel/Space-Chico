using UnityEngine;

namespace SpaceChico
{
    /// <summary>Câmera que segue o Chico com suavização, "look ahead", limites da fase e tremor (screen shake).</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector2 offset = new Vector2(0f, 1.6f);
        public float smoothTimeX = 0.15f;
        public float smoothTimeY = 0.28f;
        public float lookAhead = 2.2f;
        public float lookAheadSmooth = 0.5f;

        [Header("Limites da fase (mundo)")]
        public float minX = -12f, maxX = 130f, minY = -2f, maxY = 14f;

        static CameraFollow instance;
        Camera cam;
        float velX, velY, currentLook, lookVel;
        float shakeTime, shakeDuration, shakeAmp;
        Vector3 basePos;

        void Awake()
        {
            instance = this;
            cam = GetComponent<Camera>();
            if (Mathf.Abs(transform.position.z) < 0.1f)
            {
                transform.position = new Vector3(transform.position.x, transform.position.y, -10f);
            }
            basePos = transform.position;
        }

        public static void Shake(float amplitude, float duration)
        {
            if (instance == null) return;
            if (amplitude >= instance.shakeAmp || instance.shakeTime <= 0f)
            {
                instance.shakeAmp = amplitude;
                instance.shakeDuration = duration;
                instance.shakeTime = duration;
            }
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

            float facing = Mathf.Sign(target.lossyScale.x);
            var pc = target.GetComponent<PlayerController>();
            if (pc != null) facing = pc.Facing;

            currentLook = Mathf.SmoothDamp(currentLook, facing * lookAhead, ref lookVel, lookAheadSmooth);

            float tx = target.position.x + offset.x + currentLook;
            float ty = target.position.y + offset.y;

            basePos.x = Mathf.SmoothDamp(basePos.x, tx, ref velX, smoothTimeX);
            basePos.y = Mathf.SmoothDamp(basePos.y, ty, ref velY, smoothTimeY);
            basePos = Clamp(basePos);

            Vector3 shake = Vector3.zero;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.deltaTime;
                float k = Mathf.Clamp01(shakeTime / shakeDuration);
                shake = (Vector3)(Random.insideUnitCircle * shakeAmp * k);
            }
            transform.position = basePos + shake;
        }

        Vector3 Clamp(Vector3 p)
        {
            if (cam == null) cam = GetComponent<Camera>();
            float halfH = cam != null ? cam.orthographicSize : 6f;
            float halfW = halfH * (cam != null ? cam.aspect : 16f / 9f);
            p.x = Mathf.Clamp(p.x, minX + halfW, Mathf.Max(minX + halfW, maxX - halfW));
            p.y = Mathf.Clamp(p.y, minY + halfH, Mathf.Max(minY + halfH, maxY - halfH));
            return p;
        }
    }
}
