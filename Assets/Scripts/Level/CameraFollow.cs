using MetroidvaniaMVP.Player;
using UnityEngine;

namespace MetroidvaniaMVP.Level
{
    public sealed class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.4f, -10f);
        [SerializeField] private float smoothTime = 0.15f;

        [Header("Bounds")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private Vector2 minBounds = new Vector2(-11f, -4f);
        [SerializeField] private Vector2 maxBounds = new Vector2(28f, 8f);

        private Vector3 velocity;

        private void Awake()
        {
            if (target == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        }

        public void Configure(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
