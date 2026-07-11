using UnityEngine;

public enum ActionType
{
    NIRU,
    KIRU,
    SRITUBUSU,
    YAKU,
    GOUSEI_1,
    GOUSEI_2,
}

[RequireComponent(typeof(Collider2D))]
public class DropArea : MonoBehaviour
{
    [Header("素材を配置する位置")]
    [SerializeField]
    private Transform snapPosition;

    [Header("すでに配置されている素材")]
    [SerializeField]
    public MaterialBase placedMaterial;

    [SerializeField]
    private ActionType actionType;

    public bool CanPlaceMaterial()
    {
        return placedMaterial == null;
    }

    public Vector3 GetSnapPosition()
    {
        if (snapPosition != null)
        {
            return snapPosition.position;
        }

        return transform.position;
    }

    public bool TryPlaceMaterial(MaterialBase material)
    {
        if (material == null || !CanPlaceMaterial())
        {
            return false;
        }

        placedMaterial = material;
        material.PlaceInDropArea(this, GetSnapPosition());
        if(CheckMaterialType(material))material.ChangeMaterial();

        return true;
    }

    public void RemoveMaterial(MaterialBase material)
    {
        if (placedMaterial == material)
        {
            placedMaterial = null;
        }
    }

    private bool CheckMaterialType(MaterialBase material)
    {
        if(actionType == ActionType.YAKU && material.MaterialType == MaterialType.KINOKO)
        {
            return true;
        }
        if (actionType == ActionType.SRITUBUSU && material.MaterialType == MaterialType.KUSA)
        {
            return true;
        }
        return false;
    }
}
