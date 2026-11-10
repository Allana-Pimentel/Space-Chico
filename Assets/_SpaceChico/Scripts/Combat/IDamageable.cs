using UnityEngine;

namespace SpaceChico
{
    public interface IDamageable
    {
        void TakeDamage(int damage, Vector2 hitDirection, GameObject damageSource);
    }
}
