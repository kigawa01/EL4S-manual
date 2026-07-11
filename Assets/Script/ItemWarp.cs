using EL4S.Realtime;
using UnityEngine;

public class ItemWarp : MonoBehaviour
{
    [SerializeField] private RealtimeConnection connection;

    private void Awake()
    {
        if (RealtimeConnection.Instance != null)
        {
            connection = RealtimeConnection.Instance;
            connection.ItemTransferReceived += ReceiveItemTransfer;
        }
        else
        {
            Debug.LogWarning("[ItemWarp] RealtimeConnection.Instance is null");
        }
    }

    private void OnDestroy()
    {
        if (connection != null)
        {
            connection.ItemTransferReceived -= ReceiveItemTransfer;
        }
    }

    // itemTransfer no longer carries a position: the sender's kago and this
    // kago live in separate scenes with unrelated coordinates (e.g. Player1's
    // kago sits at roughly x=-7.7, Player2's at x=7.8), so applying the
    // sender's raw position here used to snap this kago to the wrong spot
    // every time an item arrived.
    private void ReceiveItemTransfer(string fromClientId, ItemTransfer itemTransfer)
    {
        Debug.Log($"[ItemWarp] received {itemTransfer.itemId} from {fromClientId}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        if (other.CompareTag("Item") && connection != null)
        {
            connection.SendItemTransfer(new ItemTransfer
            {
                itemId = other.gameObject.name
            });

            Destroy(other.gameObject);
        }
    }
}
