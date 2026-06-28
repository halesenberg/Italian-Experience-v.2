using UnityEngine;
using Bellavalle.Missions;

namespace Bellavalle.Scene
{
    /// <summary>
    /// Collider trigger posizionato davanti a Carla (o nella sua cucina).
    /// Quando il player ci entra con almeno un item nello zaino,
    /// avvia la fase 3 della missione (reazione di Carla).
    ///
    /// Setup:
    ///  1. Crea un GameObject nell'area di Carla
    ///  2. Aggiungi SphereCollider → Is Trigger = true, raggio 1.5m
    ///  3. Aggiungi questo component
    /// </summary>
    public class ReturnTrigger : MonoBehaviour
    {
        [SerializeField] float minimumItemsRequired = 1f;  // deve avere almeno 1 item

        bool _triggered;

        void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!other.CompareTag("Player")) return;

            var inventory = other.GetComponentInChildren<PlayerInventory>()
                            ?? FindObjectOfType<PlayerInventory>();

            if (inventory == null) return;
            if (inventory.GetCollected().Count < minimumItemsRequired) return;

            _triggered = true;
            MissionManager.Instance?.OnPlayerReturnedToCarla();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            var col = GetComponent<SphereCollider>();
            Gizmos.DrawWireSphere(transform.position, col ? col.radius : 1.5f);
        }
    }
}
