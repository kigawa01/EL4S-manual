using System;
using EL4S.Realtime;
using UnityEngine;

[Serializable]
public class MaterialPrefabEntry
{
    public MaterialType materialType;
    public MaterialBase prefab;
}

public class ItemWarp : MonoBehaviour
{
    [SerializeField] private RealtimeConnection connection;

    [Header("受信したアイテムをスポーンする際に使うプレハブ一覧")]
    [SerializeField]
    private MaterialPrefabEntry[] materialPrefabs;

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

    // itemId carries a MaterialType (e.g. "KUSA"), not a scene-specific
    // GameObject name, since the sender and this kago live in different
    // scenes: a raw name or position from the sender's scene wouldn't mean
    // anything here. The spawned copy is placed at this kago's own position
    // for the same reason - the sender's position is a separate scene's
    // local coordinates and would put the item somewhere meaningless here.
    private void ReceiveItemTransfer(string fromClientId, ItemTransfer itemTransfer)
    {
        if (!Enum.TryParse(itemTransfer.itemId, out MaterialType materialType))
        {
            Debug.LogWarning($"[ItemWarp] received unknown itemId: {itemTransfer.itemId}");
            return;
        }

        var prefab = FindPrefab(materialType);
        if (prefab == null)
        {
            Debug.LogWarning($"[ItemWarp] no prefab configured for {materialType}");
            return;
        }

        var spawned = Instantiate(prefab, transform.position, Quaternion.identity);

        // The spawned material lands right inside this kago's own trigger
        // bounds (same position), so without this it immediately re-enters
        // OnTriggerEnter2D below and gets sent straight back out before the
        // player has a chance to pick it up.
        var spawnedCollider = spawned.GetComponent<Collider2D>();
        var myCollider = GetComponent<Collider2D>();
        if (spawnedCollider != null && myCollider != null)
        {
            Physics2D.IgnoreCollision(spawnedCollider, myCollider, true);
        }
    }

    private MaterialBase FindPrefab(MaterialType materialType)
    {
        if (materialPrefabs == null) return null;

        foreach (var entry in materialPrefabs)
        {
            if (entry.materialType == materialType) return entry.prefab;
        }

        return null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        if (!other.CompareTag("Item") || connection == null) return;

        var material = other.GetComponent<MaterialBase>();
        if (material == null)
        {
            Debug.LogWarning($"[ItemWarp] {other.gameObject.name} is tagged Item but has no MaterialBase");
            return;
        }

        connection.SendItemTransfer(new ItemTransfer
        {
            itemId = material.MaterialType.ToString()
        });

        Destroy(other.gameObject);
    }
}
