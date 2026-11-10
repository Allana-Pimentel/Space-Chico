using UnityEngine;

namespace SpaceChico
{
    [RequireComponent(typeof(Collider2D))]
    public class BossArenaTrigger : MonoBehaviour
    {
        [Header("Referências")]
        public BossSwamp boss;
        public BossHealthBarUI bossHealthBar;
        public GameObject arenaBarrier;
        public CameraFollow cameraFollow;

        [Header("Limites da Arena")]
        public float cameraMinX = 86f;
        public float cameraMaxX = 114f;

        private bool triggered = false;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered) return;

            if (other.CompareTag(Layers.TagPlayer) || other.gameObject.layer == LayerMask.NameToLayer(Layers.Player))
            {
                triggered = true;

                if (cameraFollow == null) cameraFollow = FindAnyObjectByType<CameraFollow>();
                if (cameraFollow != null)
                {
                    cameraFollow.minX = cameraMinX;
                    cameraFollow.maxX = cameraMaxX;
                }

                if (arenaBarrier != null)
                {
                    arenaBarrier.SetActive(true);
                    AudioManager.Play(Sfx.Alert);
                    VFX.Burst(arenaBarrier.transform.position, new Color(0.8f, 0.2f, 0.4f), 15, 3f, 0.3f, 0.4f);
                }

                if (boss != null)
                {
                    boss.ActivateBoss();
                }

                if (bossHealthBar != null)
                {
                    bossHealthBar.Show();
                }

                gameObject.SetActive(false);
            }
        }
    }
}
