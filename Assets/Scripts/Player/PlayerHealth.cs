using System.Collections;
using MetroidvaniaMVP.Core;
using UnityEngine;

namespace MetroidvaniaMVP.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private PlayerController controller;

        [Header("Health")]
        [SerializeField] private int maxHealth = 5;
        [SerializeField] private float invincibilityTime = 0.9f;
        [SerializeField] private float respawnDelay = 0.35f;

        [Header("Knockback")]
        [SerializeField] private Vector2 knockbackForce = new Vector2(8f, 6f);

        private int currentHealth;
        private bool isInvincible;
        private bool isRespawning;
        private Vector3 respawnPosition;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            controller = GetComponent<PlayerController>();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (controller == null)
            {
                controller = GetComponent<PlayerController>();
            }

            currentHealth = maxHealth;
            respawnPosition = transform.position;
        }

        public void Configure(Rigidbody2D newBody, PlayerController newController)
        {
            body = newBody;
            controller = newController;
        }

        public void SetCheckpoint(Vector3 newRespawnPosition)
        {
            respawnPosition = newRespawnPosition;
        }

        public void TakeDamage(int damage, Vector2 hitDirection)
        {
            if (isInvincible || isRespawning || damage <= 0)
            {
                return;
            }

            currentHealth -= damage;
            ApplyKnockback(hitDirection);

            if (currentHealth <= 0)
            {
                RespawnAtCheckpoint(respawnDelay);
                return;
            }

            StartCoroutine(InvincibilityRoutine());
        }

        public void RespawnAtCheckpoint(float delay = 0f)
        {
            if (isRespawning)
            {
                return;
            }

            StartCoroutine(RespawnRoutine(delay));
        }

        private void ApplyKnockback(Vector2 hitDirection)
        {
            Vector2 direction = hitDirection.sqrMagnitude > 0.001f ? hitDirection.normalized : Vector2.left;
            body.linearVelocity = new Vector2(direction.x * knockbackForce.x, knockbackForce.y);
        }

        private IEnumerator InvincibilityRoutine()
        {
            isInvincible = true;
            yield return new WaitForSeconds(invincibilityTime);
            isInvincible = false;
        }

        private IEnumerator RespawnRoutine(float delay)
        {
            isRespawning = true;
            isInvincible = true;
            yield return new WaitForSeconds(delay);

            transform.position = respawnPosition;
            currentHealth = maxHealth;
            if (controller != null)
            {
                controller.ResetMotion();
            }
            else
            {
                body.linearVelocity = Vector2.zero;
            }

            yield return new WaitForSeconds(invincibilityTime);
            isInvincible = false;
            isRespawning = false;
        }
    }
}
