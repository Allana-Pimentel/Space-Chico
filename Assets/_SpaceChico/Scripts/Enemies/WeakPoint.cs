using UnityEngine;

namespace SpaceChico
{
    [RequireComponent(typeof(Collider2D))]
    public class WeakPoint : MonoBehaviour
    {
        public EnemyBase parentEnemy;
        public int stompDamage = 2;
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

        public bool TryStomp(PlayerController player)
        {
            if (parentEnemy == null || parentEnemy.IsDead) return false;

            parentEnemy.TakeDamage(stompDamage, Vector2.down, player.gameObject);

            AudioManager.Play(Sfx.Stomp);

            VFX.Burst(transform.position, new Color(0.4f, 1f, 0.4f), 18, 4.5f, 0.25f, 0.4f);
            CameraFollow.Shake(0.18f, 0.15f);

            return true;
        }
    }
}
