using UnityEngine;

namespace SpaceChico
{
    // Mantido apenas para compatibilidade de prefab (modo Estilo Mario não usa tiro laser)
    public class Projectile : MonoBehaviour
    {
        public float speed = 15f;
        public int damage = 1;
        public float lifeTime = 2.5f;
        public Color trailColor = new Color(0.2f, 0.95f, 1f, 1f);

        public void Launch(Vector2 direction)
        {
            Destroy(gameObject);
        }
    }
}
