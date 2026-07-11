using UnityEngine;

public enum MaterialType
{
    KINOKO,
    KUSA,
    KINOMI,
    TOKAGE
}

[RequireComponent(typeof(Collider2D))]
public class MaterialBase : MonoBehaviour
{
    public bool Changed = false;

    SpriteRenderer changedMaterial;

    [Header("変化後のSprite")]
    [SerializeField]
    private Sprite changedSprite;

    [Header("最初に所属する素材置き場")]
    [SerializeField]
    private MaterialStore defaultStorage;

    private Camera mainCamera;

    private MaterialStore currentStorage;
    private DropArea currentDropArea;
    private DropArea hoveredDropArea;

    private Vector3 dragOffset;
    private bool isDragging;

    //private enum MaterialState
    //{
    //    InStorage,
    //    InDropArea,
    //    Dragging
    //}

    

    [SerializeField]
    private MaterialType materialType;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "MainCameraが見つかりません。CameraのTagをMainCameraにしてください。");
        }

        changedMaterial = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (defaultStorage != null)
        {
            defaultStorage.AddMaterial(this);
        }
    }

    private void Update()
    {
        if (!isDragging)
        {
            return;
        }

        // 枠外でマウスを離した場合も検出する
        if (Input.GetMouseButtonUp(0))
        {
            FinishDragging();
        }
    }

    private void OnMouseDown()
    {
        if (mainCamera == null)
        {
            return;
        }

        StartDragging();
    }

    private void OnMouseDrag()
    {
        if (!isDragging || mainCamera == null)
        {
            return;
        }

        Vector3 newPosition = GetMouseWorldPosition() + dragOffset;
        newPosition.z = transform.position.z;

        transform.position = newPosition;
    }

    private void StartDragging()
    {
        isDragging = true;

        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        dragOffset = transform.position - mouseWorldPosition;

        // 素材置き場にいた場合は、ドラッグ中だけリストから外す
        if (currentStorage != null)
        {
            currentStorage.RemoveMaterial(this);
            currentStorage = null;
        }

        // 枠に配置されていた場合は、その枠を空ける
        if (currentDropArea != null)
        {
            currentDropArea.RemoveMaterial(this);
            currentDropArea = null;
        }
    }

    private void FinishDragging()
    {
        isDragging = false;

        // 枠内で離した場合
        if (hoveredDropArea != null)
        {
            bool placed = hoveredDropArea.TryPlaceMaterial(this);

            if (placed)
            {
                return;
            }
        }

        // 枠外、または枠が使用中の場合は素材置き場へ戻す
        ReturnToStorage();
    }

    private void ReturnToStorage()
    {
        if (defaultStorage == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Default Storageが設定されていません。");
            return;
        }

        currentDropArea = null;
        hoveredDropArea = null;

        defaultStorage.AddMaterial(this);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;

        mousePosition.z = Mathf.Abs(
            mainCamera.transform.position.z - transform.position.z);

        return mainCamera.ScreenToWorldPoint(mousePosition);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        DropArea dropArea = other.GetComponent<DropArea>();

        if (dropArea != null)
        {
            hoveredDropArea = dropArea;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        DropArea dropArea = other.GetComponent<DropArea>();

        if (dropArea != null && hoveredDropArea == dropArea)
        {
            hoveredDropArea = null;
        }
    }

    public void SetCurrentStorage(MaterialStore storage)
    {
        currentStorage = storage;
        currentDropArea = null;
    }

    public void MoveToStoragePosition(Vector3 position)
    {
        position.z = transform.position.z;
        transform.position = position;
    }

    public void PlaceInDropArea(
        DropArea dropArea,
        Vector3 position)
    {
        currentStorage = null;
        currentDropArea = dropArea;
        hoveredDropArea = null;

        position.z = transform.position.z;
        transform.position = position;
    }

    public void ChangeMaterial()
    {
        if (changedMaterial == null)
        {
            Debug.LogWarning($"{gameObject.name}: SpriteRendererが見つかりません。");
            return;
        }

        if (changedSprite == null)
        {
            Debug.LogWarning($"{gameObject.name}: 変化後のSpriteが設定されていません。");
            return;
        }

        Changed = true;
        changedMaterial.sprite = changedSprite;
    }

    public MaterialType MaterialType
    {
        get { return materialType; }
    }

    public bool IsChanged
    {
        get { return Changed; }
    }

    public void DestroyMaterial()
    {
        Destroy(gameObject);
    }
}
