using EL4S.Realtime;
using UnityEngine;

public enum CheckType
{
    Check1,
    Check2,
    Check3
}
public class Check : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    public CheckType checkType;

    [SerializeField]
    MaterialBase CheckMaterial;

    SpriteRenderer checkSpriteRenderer;

    [SerializeField] private RealtimeConnection connection;

    private void Awake()
    {
        if (RealtimeConnection.Instance != null)
        {
            connection = RealtimeConnection.Instance;
            connection.AlchemyResultReceived += Checka;
        }
        else
        {
            Debug.LogWarning("[ItemWarp] RealtimeConnection.Instance is null");
        }
    }

    void Start()
    {
        checkSpriteRenderer = GetComponent<SpriteRenderer>();
        checkSpriteRenderer.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(checkType == CheckType.Check2 || checkType == CheckType.Check3)
        {
           // if(CheckMaterial.Changed){ 
                connection.SendAlchemyResult(new AlchemyResult
                {
                    checkType = checkType
                });

                checkSpriteRenderer.enabled = true; 
        //}
        }
    }

    void Checka(string fromClient, AlchemyResult alchemyResult)
    {
        //if (alchemyResult.checkType == checkType)
        //{
        //    checkSpriteRenderer.enabled = true;
        //}
    }
}
