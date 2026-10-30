using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Mecânica de Disparo da Arma (GDD Seção 4 e Sprint 4: Tecla E).
    /// Dispara à distância sem interromper o movimento atual.
    /// </summary>
    public class PlayerShooter : MonoBehaviour
    {
        [Header("Projétil")]
        public GameObject projectilePrefab;
        public Transform firePoint;
        public float fireRate = 0.28f; // Cooldown entre disparos

        [Header("Efeitos")]
        public Color muzzleColor = new Color(0.2f, 0.9f, 1f);

        private float nextFireTime;
        private PlayerController player;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            if (firePoint == null)
            {
                var fp = new GameObject("FirePoint");
                fp.transform.SetParent(transform);
                fp.transform.localPosition = new Vector3(0.55f, 0.15f, 0f);
                firePoint = fp.transform;
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameplayRunning) return;
            if (player != null && (player.State == PlayerState.Dead || player.State == PlayerState.Celebrate)) return;

            // Disparo com Tecla E (GameAction.Shoot)
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

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + (Vector3)(fireDir * 0.5f);

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
                // Fallback dinâmico caso prefab não esteja linkado
                var go = new GameObject("Projectile_Dynamic");
                go.transform.position = spawnPos;
                var proj = go.AddComponent<Projectile>();
                proj.Launch(fireDir);
            }

            AudioManager.Play(Sfx.Shoot);
            VFX.Burst(spawnPos, muzzleColor, 6, 2.5f, 0.12f, 0.15f);
        }
    }
}
