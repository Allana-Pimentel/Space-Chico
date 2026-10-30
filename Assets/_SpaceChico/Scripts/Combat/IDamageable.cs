using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Interface para qualquer entidade que possa receber dano (Jogador, Inimigos, etc.)
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(int damage, Vector2 hitDirection, GameObject damageSource);
    }
}
