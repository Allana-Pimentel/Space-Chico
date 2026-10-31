using UnityEngine;

namespace SpaceChico
{
    // Projétil laser disparado pela arma do Chico
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("Configurações")]
        public float speed = 15f;
        public int damage = 1;
        public float lifeTime = 2.5f;
        public Color trailColor = new Color(0.2f, 0.95f, 1f, 1f);

        private Rigidbody2D rb;
        private float spawnTime;
        private bool hasHit;
        private static Sprite generatedLaserSprite;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            // Garante que a bala sempre tenha um sprite visível de laser neon
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            if (sr.sprite == null)
            {
                if (generatedLaserSprite == null)
                    generatedLaserSprite = CreateLaserSprite();
                sr.sprite = generatedLaserSprite;
            }
            sr.sortingOrder = 55;
            sr.color = Color.white;
        }

        public void Launch(Vector2 direction)
        {
            spawnTime = Time.time;
            direction = direction.normalized;
            rb.linearVelocity = direction * speed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void Update()
        {
            if (Time.time - spawnTime >= lifeTime && !hasHit)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasHit) return;

            if (other.CompareTag(Layers.TagPlayer) || other.gameObject.layer == LayerMask.NameToLayer(Layers.Player))
                return;

            var damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = other.GetComponentInParent<IDamageable>();

            if (damageable != null || other.CompareTag(Layers.TagEnemy))
            {
                hasHit = true;
                Vector2 hitDir = rb.linearVelocity.normalized;
                if (damageable != null)
                {
                    damageable.TakeDamage(damage, hitDir, gameObject);
                }

                VFX.Burst(transform.position, trailColor, 12, 3.5f, 0.2f, 0.35f);
                AudioManager.Play(Sfx.EnemyHit);
                Destroy(gameObject);
                return;
            }

            if (other.CompareTag(Layers.TagGround) || other.gameObject.layer == LayerMask.NameToLayer(Layers.Ground))
            {
                hasHit = true;
                VFX.Burst(transform.position, trailColor * 0.8f, 6, 2f, 0.15f, 0.25f);
                Destroy(gameObject);
            }
        }

        // Cria o sprite brilhante do disparo laser
        private static Sprite CreateLaserSprite()
        {
            int w = 48;
            int h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                float dy = Mathf.Abs(y - (h - 1) * 0.5f) / (h * 0.5f);
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - (w - 1) * 0.5f) / (w * 0.5f);
                    float dist = Mathf.Sqrt(dx * dx * 0.7f + dy * dy);
                    if (dist > 1f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (dist < 0.38f)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        float alpha = Mathf.Clamp01((1f - dist) * 1.6f);
                        tex.SetPixel(x, y, new Color(0.15f, 0.95f, 1f, alpha));
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 48f);
        }
    }
}
