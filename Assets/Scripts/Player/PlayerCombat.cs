using MetroidvaniaMVP.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MetroidvaniaMVP.Player
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AbilityController abilities;
        [SerializeField] private PlayerController controller;
        [SerializeField] private Transform attackPoint;
        [SerializeField] private LayerMask enemyLayers;

        [Header("Attack")]
        [SerializeField] private int damage = 1;
        [SerializeField] private float attackRadius = 0.65f;
        [SerializeField] private float attackCooldown = 0.3f;

        private float cooldownCounter;

        private void Awake()
        {
            if (abilities == null)
            {
                abilities = GetComponent<AbilityController>();
            }

            if (controller == null)
            {
                controller = GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            cooldownCounter -= Time.deltaTime;

            if (!abilities.CanAttack || cooldownCounter > 0f || (controller != null && controller.IsActionLocked))
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            bool attackPressed = keyboard != null && keyboard.jKey.wasPressedThisFrame;
            attackPressed |= mouse != null && mouse.leftButton.wasPressedThisFrame;

            if (attackPressed)
            {
                Attack();
            }
        }

        public void Configure(AbilityController newAbilities, Transform newAttackPoint, LayerMask newEnemyLayers, PlayerController newController = null)
        {
            abilities = newAbilities;
            controller = newController;
            attackPoint = newAttackPoint;
            enemyLayers = newEnemyLayers;
        }

        private void Attack()
        {
            cooldownCounter = attackCooldown;
            if (attackPoint == null)
            {
                return;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayers);
            foreach (Collider2D hit in hits)
            {
                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    Vector2 hitDirection = (hit.transform.position - transform.position).normalized;
                    damageable.TakeDamage(damage, hitDirection);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (attackPoint == null)
            {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}
