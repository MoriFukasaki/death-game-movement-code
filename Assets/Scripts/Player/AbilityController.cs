using UnityEngine;

namespace MetroidvaniaMVP.Player
{
    public sealed class AbilityController : MonoBehaviour
    {
        [Header("Unlocked Abilities")]
        [SerializeField] private bool canDash = true;
        [SerializeField] private bool canWallJump = true;
        [SerializeField] private bool canAttack = true;

        public bool CanDash => canDash;
        public bool CanWallJump => canWallJump;
        public bool CanAttack => canAttack;

        public void SetDashUnlocked(bool unlocked)
        {
            canDash = unlocked;
        }

        public void SetWallJumpUnlocked(bool unlocked)
        {
            canWallJump = unlocked;
        }

        public void SetAttackUnlocked(bool unlocked)
        {
            canAttack = unlocked;
        }
    }
}
