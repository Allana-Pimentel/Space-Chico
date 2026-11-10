using UnityEngine;

namespace SpaceChico
{
    [RequireComponent(typeof(Collider2D))]
    public class StompFoot : MonoBehaviour
    {
        public PlayerController player;
        public float defaultBounceForce = 13f;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            if (player == null) player = GetComponentInParent<PlayerController>();
        }

        private void Awake()
        {
            if (player == null) player = GetComponentInParent<PlayerController>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (player == null) return;

            if (player.CurrentVelocity.y > 0.5f) return;

            var weakPoint = other.GetComponent<WeakPoint>();
            if (weakPoint == null)
            {
                weakPoint = other.GetComponentInParent<WeakPoint>();
            }

            if (weakPoint != null)
            {
                if (weakPoint.TryStomp(player))
                {
                    float force = weakPoint.bounceForce > 0 ? weakPoint.bounceForce : defaultBounceForce;
                    player.BounceFromStomp(force);
                }
            }
        }
    }
}
