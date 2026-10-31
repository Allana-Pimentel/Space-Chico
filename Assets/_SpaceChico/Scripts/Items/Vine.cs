using UnityEngine;

namespace SpaceChico
{
    // Cipó escalável da fase
    [RequireComponent(typeof(Collider2D))]
    public class Vine : MonoBehaviour
    {
        public float topY;
        public float bottomY;

        private void Awake()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.isTrigger = true;
                box.size = new Vector2(1.4f, 7.1f);
                box.offset = new Vector2(0f, 0.55f);
            }
            CalculateBounds();
        }

        private void CalculateBounds()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                topY = col.bounds.max.y;
                bottomY = col.bounds.min.y;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc == null) pc = other.GetComponentInParent<PlayerController>();
            if (pc != null) pc.RegisterVine(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc == null) pc = other.GetComponentInParent<PlayerController>();
            if (pc != null) pc.UnregisterVine(this);
        }
    }
}
