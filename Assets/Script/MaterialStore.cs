using System.Collections.Generic;
using UnityEngine;

public class MaterialStore : MonoBehaviour
{
    [Header("‘fŞ‚ğ•À‚×n‚ß‚éˆÊ’u")]
    [SerializeField]
    private Transform startPosition;

    [Header("‘fŞ“¯m‚Ìc•ûŒü‚ÌŠÔŠu")]
    [SerializeField]
    private float verticalSpacing = 1.5f;

    [Header("‰º•ûŒü‚É•À‚×‚é‚©")]
    [SerializeField]
    private bool arrangeDownward = true;

    private readonly List<MaterialBase> storedMaterials = new();

    /// <summary>
    /// ‘fŞ‚ğ’u‚«ê‚Ö“o˜^‚·‚é
    /// </summary>
    public void AddMaterial(MaterialBase material)
    {
        if (material == null)
        {
            return;
        }

        if (!storedMaterials.Contains(material))
        {
            storedMaterials.Add(material);
        }

        material.SetCurrentStorage(this);
        ArrangeMaterials();
    }

    /// <summary>
    /// ƒhƒ‰ƒbƒOŠJn‚ÉA‘fŞ‚ğ’u‚«ê‚©‚çˆê“I‚ÉŠO‚·
    /// </summary>
    public void RemoveMaterial(MaterialBase material)
    {
        if (material == null)
        {
            return;
        }

        storedMaterials.Remove(material);
        ArrangeMaterials();
    }

    /// <summary>
    /// “o˜^‚³‚ê‚Ä‚¢‚é‘fŞ‚ğc•ûŒü‚É®—ñ‚·‚é
    /// </summary>
    public void ArrangeMaterials()
    {
        if (startPosition == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Start Position‚ªİ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñB");
            return;
        }

        float direction = arrangeDownward ? -1.0f : 1.0f;

        for (int i = 0; i < storedMaterials.Count; i++)
        {
            MaterialBase material = storedMaterials[i];

            if (material == null)
            {
                continue;
            }

            Vector3 position = startPosition.position;
            position.y += verticalSpacing * i * direction;

            material.MoveToStoragePosition(position);
        }
    }
}
