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

    private void ReceiveItemTransfer(string fromClientId, ItemTransfer itemTransfer)
    {
        if (this.gameObject == null) return;

        this.gameObject.transform.position = itemTransfer.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        Debug.Log("Hit");

        if (other.CompareTag("Item") && connection != null)
        {
            connection.SendItemTransfer(new ItemTransfer
            {
                position = this.gameObject.transform.position
            });

            Destroy(other.gameObject);
        }
    }
}
