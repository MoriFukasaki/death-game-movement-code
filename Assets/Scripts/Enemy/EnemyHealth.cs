using MetroidvaniaMVP.Core;
using UnityEngine;

namespace MetroidvaniaMVP.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D body;

        [Header("Health")]
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float destroyDelay = 0.05f;

        [Header("Hit Reaction")]
        [SerializeField] private Vector2 knockbackForce = new Vector2(4f, 3f);

        private int currentHealth;
        private bool isDead;

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            currentHealth = maxHealth;
        }

        public void Configure(Rigidbody2D newBody)
        {
            body = newBody;
        }

        public void TakeDamage(int damage, Vector2 hitDirection)
        {
            if (isDead || damage <= 0)
            {
                return;
            }

            currentHealth -= damage;
            Vector2 direction = hitDirection.sqrMagnitude > 0.001f ? hitDirection.normalized : Vector2.right;
            body.linearVelocity = new Vector2(direction.x * knockbackForce.x, knockbackForce.y);

            if (currentHealth <= 0)
            {
                isDead = true;
                Destroy(gameObject, destroyDelay);
            }
        }
    }
}
