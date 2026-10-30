using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Colisor "Esmagador" nos pés do Chico (GDD Sprint 3.1).
    /// Ativo quando o jogador está caindo (Fall).
    /// Ao encostar no Ponto Fraco de um inimigo, causa 2 de dano e faz o Chico pular novamente.
    /// </summary>
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

            // Só esmaga se o jogador estiver caindo (velocidade vertical < 0 ou estado Fall)
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
