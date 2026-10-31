using UnityEngine;

namespace SpaceChico
{
    // Script de disparo da arma laser do Chico
    public class PlayerShooter : MonoBehaviour
    {
        [Header("Projétil")]
        public GameObject projectilePrefab;
        public Transform firePoint;
        public float fireRate = 0.25f;

        [Header("Efeitos")]
        public Color muzzleColor = new Color(0.2f, 0.95f, 1f);

        private float nextFireTime;
        private PlayerController player;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            if (firePoint == null)
            {
                var fp = new GameObject("FirePoint");
                fp.transform.SetParent(transform);
                fp.transform.localPosition = new Vector3(0.65f, 0.22f, 0f);
                firePoint = fp.transform;
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameplayRunning) return;
            if (player != null && (player.State == PlayerState.Dead || player.State == PlayerState.Celebrate)) return;

            if (GameInput.Pressed(GameAction.Shoot) && Time.time >= nextFireTime)
            {
                Shoot();
            }
        }

        public void Shoot()
        {
            nextFireTime = Time.time + fireRate;

            float facing = player != null ? player.Facing : Mathf.Sign(transform.localScale.x);
            Vector2 fireDir = new Vector2(facing, 0f);

            // Posição da ponta da arma na direção em que o Chico está olhando
            Vector3 spawnPos = transform.position + new Vector3(facing * 0.85f, 0.65f, 0f);

            if (player != null)
            {
                player.NotifyShot();
            }

            if (projectilePrefab != null)
            {
                var projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                var proj = projObj.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.Launch(fireDir);
                }
            }
            else
            {
                var go = new GameObject("Projectile_Dynamic");
                go.transform.position = spawnPos;
                var proj = go.AddComponent<Projectile>();
                proj.Launch(fireDir);
            }

            AudioManager.Play(Sfx.Shoot);
            VFX.Burst(spawnPos, muzzleColor, 10, 3.5f, 0.18f, 0.2f);
        }
    }
}
