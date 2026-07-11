using EL4S.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GOUSEI : MonoBehaviour
{


    [Header("合成素材を入れる2つの枠")]
    [SerializeField]
    private DropArea firstDropArea;

    [SerializeField]
    private DropArea secondDropArea;

    [Header("生成するオブジェクト")]
    [SerializeField]
    private GameObject resultPrefab;

    [Header("合成結果を生成する位置")]
    [SerializeField]
    private Transform resultSpawnPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MaterialBase firstMaterial  = firstDropArea.placedMaterial;

        MaterialBase secondMaterial = secondDropArea.placedMaterial;

        if(!firstMaterial || !secondMaterial)
        {
            return;
        }

        if (firstMaterial.Changed && secondMaterial.Changed)
        {
            // 合成処理をここに追加
            firstMaterial.DestroyMaterial();
            secondMaterial.DestroyMaterial();

            // 合成結果を生成する
            //GameObject resultObject = Instantiate(
            //    resultPrefab,
            //    resultSpawnPoint.position,
            //    Quaternion.identity);

            //MaterialBase resultMaterial =
            //resultObject.GetComponent<MaterialBase>();
            //
            //if (resultMaterial != null)
            //{
            //    resultMaterial.InitializeAsCraftResult();
            //}

            SpriteRenderer resultSpriteRenderer = resultPrefab.GetComponent<SpriteRenderer>();
            MaterialBase resultMaterial = resultPrefab.GetComponent<MaterialBase>();
            if (resultSpriteRenderer != null)
            {
                resultSpriteRenderer.enabled = true;
                resultMaterial.Changed = true;

                // 2秒待ってからシーン遷移するコルーチンを開始
                StartCoroutine(ChangeSceneAfterDelay(2f));
            }
        }
    }

    private System.Collections.IEnumerator ChangeSceneAfterDelay(float delay)
    {
        // delay 秒待つ
        yield return new WaitForSeconds(delay);

        // シーン遷移
        SceneManager.LoadScene("Clear");
    }
}
