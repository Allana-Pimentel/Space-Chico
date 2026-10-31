using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Colisor "Ponto Fraco" no topo do inimigo.
    /// Quando atingido pelos pés do Chico (StompFoot), repassa o dano de esmagamento (2 de dano)
    /// e instrui o jogador a saltar para cima.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WeakPoint : MonoBehaviour
    {
        public EnemyBase parentEnemy;
        public int stompDamage = 2; //Pulo Esmagador causa 2 de dano
        public float bounceForce = 12.5f;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            if (parentEnemy == null) parentEnemy = GetComponentInParent<EnemyBase>();
            gameObject.layer = LayerMask.NameToLayer(Layers.EnemyTrigger);
        }

        private void Awake()
        {
            if (parentEnemy == null) parentEnemy = GetComponentInParent<EnemyBase>();
        }

        /// <summary>
        /// Chamado pelo pé do jogador (StompFoot) quando ele colide caindo.
        /// </summary>
        public bool TryStomp(PlayerController player)
        {
            if (parentEnemy == null || parentEnemy.IsDead) return false;

            // Aplica 2 de dano ao inimigo
            parentEnemy.TakeDamage(stompDamage, Vector2.down, player.gameObject);

            // Efeito sonoro de esmagamento
            AudioManager.Play(Sfx.Stomp);

            // Partículas de impacto
            VFX.Burst(transform.position, new Color(0.4f, 1f, 0.4f), 18, 4.5f, 0.25f, 0.4f);
            CameraFollow.Shake(0.18f, 0.15f);

            return true;
        }
    }
}
