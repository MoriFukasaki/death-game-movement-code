using MetroidvaniaMVP.Player;
using UnityEngine;

namespace MetroidvaniaMVP.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyPatrol : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Transform leftPoint;
        [SerializeField] private Transform rightPoint;

        [Header("Patrol")]
        [SerializeField] private float speed = 2f;
        [SerializeField] private float fallbackPatrolDistance = 2.5f;

        [Header("Damage")]
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private float contactDamageCooldown = 0.7f;

        private int direction = 1;
        private float fallbackLeftX;
        private float fallbackRightX;
        private float nextDamageTime;

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

            fallbackLeftX = transform.position.x - fallbackPatrolDistance;
            fallbackRightX = transform.position.x + fallbackPatrolDistance;
        }

        private void FixedUpdate()
        {
            float leftX = leftPoint != null ? leftPoint.position.x : fallbackLeftX;
            float rightX = rightPoint != null ? rightPoint.position.x : fallbackRightX;

            if (direction > 0 && transform.position.x >= rightX)
            {
                SetDirection(-1);
            }
            else if (direction < 0 && transform.position.x <= leftX)
            {
                SetDirection(1);
            }

            body.linearVelocity = new Vector2(direction * speed, body.linearVelocity.y);
        }

        public void Configure(Rigidbody2D newBody, Transform newLeftPoint, Transform newRightPoint)
        {
            body = newBody;
            leftPoint = newLeftPoint;
            rightPoint = newRightPoint;
        }

        private void SetDirection(int newDirection)
        {
            direction = newDirection;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryDamagePlayer(collision.collider);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamagePlayer(other);
        }

        private void TryDamagePlayer(Collider2D other)
        {
            if (Time.time < nextDamageTime)
            {
                return;
            }

            if (!other.TryGetComponent(out PlayerHealth playerHealth))
            {
                return;
            }

            Vector2 hitDirection = (playerHealth.transform.position - transform.position).normalized;
            playerHealth.TakeDamage(contactDamage, hitDirection);
            nextDamageTime = Time.time + contactDamageCooldown;
        }
    }
}
