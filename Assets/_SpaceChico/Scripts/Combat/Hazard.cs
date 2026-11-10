using UnityEngine;

namespace SpaceChico
{
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        public int damage = 1;
        public float tickRate = 1.0f;
        public bool instantRespawn = false;

        private float nextDamageTime;

        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && (sr.sprite == null || sr.sprite.name == "")) sr.enabled = false;
        }

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            gameObject.layer = LayerMask.NameToLayer(Layers.Hazard);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time < nextDamageTime) return;

            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null && (other.CompareTag(Layers.TagPlayer) || other.gameObject.layer == LayerMask.NameToLayer(Layers.Player)))
            {
                nextDamageTime = Time.time + tickRate;
                Vector2 dir = (other.transform.position - transform.position).normalized;
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
                damageable.TakeDamage(damage, dir, gameObject);
            }
        }
    }
}
