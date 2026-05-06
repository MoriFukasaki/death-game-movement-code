using MetroidvaniaMVP.Player;
using UnityEngine;

namespace MetroidvaniaMVP.Level
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [Header("Respawn")]
        [SerializeField] private Vector3 respawnOffset = new Vector3(0f, 0.9f, 0f);

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer markerRenderer;
        [SerializeField] private Color inactiveColor = new Color(0.2f, 0.8f, 0.5f, 1f);
        [SerializeField] private Color activeColor = new Color(0.2f, 1f, 1f, 1f);

        private bool isActive;

        private void Reset()
        {
            markerRenderer = GetComponent<SpriteRenderer>();
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
        }

        private void Awake()
        {
            if (markerRenderer == null)
            {
                markerRenderer = GetComponent<SpriteRenderer>();
            }

            ApplyVisualState();
        }

        public void Configure(SpriteRenderer newMarkerRenderer)
        {
            markerRenderer = newMarkerRenderer;
            ApplyVisualState();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out PlayerHealth playerHealth))
            {
                return;
            }

            playerHealth.SetCheckpoint(transform.position + respawnOffset);
            isActive = true;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (markerRenderer != null)
            {
                markerRenderer.color = isActive ? activeColor : inactiveColor;
            }
        }
    }
}
