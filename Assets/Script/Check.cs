using UnityEngine;

enum CheckType
{
    Check1,
    Check2,
    Check3
}
public class Check : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private CheckType checkType;

    [SerializeField]
    MaterialBase CheckMaterial;

    SpriteRenderer checkSpriteRenderer;
    void Start()
    {
        checkSpriteRenderer = GetComponent<SpriteRenderer>();
        checkSpriteRenderer.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(checkType == CheckType.Check1 || checkType == CheckType.Check3)
        {
            if(CheckMaterial.Changed) checkSpriteRenderer.enabled = true;
        }
    }
}
