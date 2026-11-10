using UnityEngine;

namespace SpaceChico
{
    // Desativado no modo Estilo Mario (Chico derrota inimigos pulando na cabeça deles)
    public class PlayerShooter : MonoBehaviour
    {
        public GameObject projectilePrefab;
        public Transform firePoint;
        public float fireRate = 0.25f;
        public Color muzzleColor = new Color(0.2f, 0.95f, 1f);
    }
}
