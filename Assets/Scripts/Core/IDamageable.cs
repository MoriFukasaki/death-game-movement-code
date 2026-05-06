using UnityEngine;

namespace MetroidvaniaMVP.Core
{
    public interface IDamageable
    {
        void TakeDamage(int damage, Vector2 hitDirection);
    }
}
