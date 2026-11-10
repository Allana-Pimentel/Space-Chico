using UnityEngine;

namespace SpaceChico
{
    [RequireComponent(typeof(Collider2D))]
    public class Vine : MonoBehaviour
    {
        public float topY;
        public float bottomY;
        public Collider2D platformCollider;

        private void Awake()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.isTrigger = true;
                box.size = new Vector2(1.35f, 6.4f);
                box.offset = new Vector2(0f, 0.25f);
            }
            CalculateBounds();
        }

        private void Start()
        {
            CalculateBounds();
        }

        public void CalculateBounds()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                topY = col.bounds.max.y;
                bottomY = col.bounds.min.y;

                Collider2D[] hits = Physics2D.OverlapBoxAll(
                    new Vector2(transform.position.x, topY - 0.4f),
                    new Vector2(1.2f, 2.0f),
                    0f,
                    Layers.GroundMask
                );

                foreach (var h in hits)
                {
                    if (h != null && h != col && h.bounds.max.y > transform.position.y)
                    {
                        platformCollider = h;
                        topY = h.bounds.max.y;
                        break;
                    }
                }
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
