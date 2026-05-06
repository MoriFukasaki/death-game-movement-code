using MetroidvaniaMVP.Player;
using UnityEngine;

namespace MetroidvaniaMVP.Level
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FallRespawnZone : MonoBehaviour
    {
        [SerializeField] private float respawnDelay;

        private void Reset()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.RespawnAtCheckpoint(respawnDelay);
            }
        }
    }
}
