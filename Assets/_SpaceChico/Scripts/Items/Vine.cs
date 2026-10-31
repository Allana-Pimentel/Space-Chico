using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Vinha bioluminescente escalável.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Vine : MonoBehaviour
    {
        public float topY;
        public float bottomY;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            CalculateBounds();
        }

        private void Awake()
        {
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
            else
            {
                topY = transform.position.y + 2f;
                bottomY = transform.position.y - 2f;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.RegisterVine(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.UnregisterVine(this);
            }
        }
    }
}
