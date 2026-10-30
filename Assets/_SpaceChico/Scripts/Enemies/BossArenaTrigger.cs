using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Gatilho de entrada na Arena do Chefe do Pântano.
    /// Trava a câmera na arena, ergue a barreira de vinhas e desperta o chefe com a barra de vida.
    /// </summary>
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

                // Trava limites da câmera na arena
                if (cameraFollow == null) cameraFollow = FindAnyObjectByType<CameraFollow>();
                if (cameraFollow != null)
                {
                    cameraFollow.minX = cameraMinX;
                    cameraFollow.maxX = cameraMaxX;
                }

                // Ergue a barreira atrás do jogador
                if (arenaBarrier != null)
                {
                    arenaBarrier.SetActive(true);
                    AudioManager.Play(Sfx.Alert);
                    VFX.Burst(arenaBarrier.transform.position, new Color(0.8f, 0.2f, 0.4f), 15, 3f, 0.3f, 0.4f);
                }

                // Desperta o Chefe
                if (boss != null)
                {
                    boss.ActivateBoss();
                }

                // Exibe a barra de vida do Chefe
                if (bossHealthBar != null)
                {
                    bossHealthBar.Show();
                }

                gameObject.SetActive(false);
            }
        }
    }
}
