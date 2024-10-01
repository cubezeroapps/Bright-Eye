using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Linq;
using System.IO;
using Mono.Data.Sqlite;

public class DBManager : MonoBehaviour
{
    public VoiceFeedbackManager voiceFeedbackManager;
    public PageManager pageManager;

    private string dbPath;
    private AssetBundle loadedBundle;

    void Start()
    {
        dbPath = Path.Combine(Application.persistentDataPath, "ProductDB.sqlite");
        StartCoroutine(CopyDatabaseToPersistentPath());
    }

    IEnumerator CopyDatabaseToPersistentPath()
    {
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }

        string uri = Path.Combine(Application.streamingAssetsPath, "DB/ProductDB.sqlite");

    #if UNITY_ANDROID
        uri = Application.streamingAssetsPath + "/DB/ProductDB.sqlite";
    #endif
        UnityWebRequest request = UnityWebRequest.Get(uri);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            File.WriteAllBytes(dbPath, request.downloadHandler.data);
        }
    }

    public void LoadPrefabData(string barcode, System.Action<string> onPrefabNameLoaded)
    {
        if (!File.Exists(dbPath))
        {
            return;
        }

        string connectionString = "URI=file:" + dbPath;

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT prefab_name FROM Product WHERE barcode = @barcode";
                command.Parameters.AddWithValue("@barcode", barcode);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string prefabName = reader.GetString(0);
                        onPrefabNameLoaded(prefabName);
                    }
                    else
                    {
                        voiceFeedbackManager.StartCoroutine(voiceFeedbackManager.PlayAudioClip(0.3f, 5));
                    }
                }
            }
        }
    }

    private IEnumerator LoadAudioData(string barcode, string path)
    {
        if (!File.Exists(dbPath))
        {
            yield break;
        }

        string connectionString = "URI=file:" + dbPath;

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT prefab_name FROM Product WHERE barcode = @barcode";
                command.Parameters.AddWithValue("@barcode", barcode);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string prefabName = reader.GetString(0);
                        voiceFeedbackManager.PlayAudioFromPath(path + prefabName);
                    }
                }
            }
        }
        yield return null;
    }

    public IEnumerator LoadAssetBundle()
    {
        if (loadedBundle == null)
        {
            string bundleName = "operation layout";
            string bundlePath = Path.Combine(Application.streamingAssetsPath, "DB/Data/Prefab", bundleName);

        #if UNITY_ANDROID
            bundlePath = Application.streamingAssetsPath + "/DB/Data/Prefab/" + bundleName;
        #endif

            AssetBundleCreateRequest bundleRequest = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return bundleRequest;

            loadedBundle = bundleRequest.assetBundle;

            if (loadedBundle == null)
            {
                yield break;
            }
        }
    }

    public void LoadPrefabFromBundle(string prefabName, string barcode, int pageIndex = -1)
    {
        if (loadedBundle == null)
        {
            return;
        }

        GameObject prefab = loadedBundle.LoadAsset<GameObject>(prefabName);

        if (prefab != null)
        {
            GameObject instantiatedPrefab;

            if (pageIndex == -1)
            {
                instantiatedPrefab = pageManager.AddNewPage(prefab, barcode);
            }
            else
            {
                string barcodeKey = "PageBarcode_" + pageIndex;
                string savedBarcode = PlayerPrefs.GetString(barcodeKey, "");
                instantiatedPrefab = pageManager.AddPage(prefab, pageIndex, barcode: savedBarcode);
            }

            Button[] buttons = instantiatedPrefab.GetComponentsInChildren<Button>();

            foreach (Button button in buttons)
            {
                string buttonName = button.name;
                button.onClick.AddListener(() => {
                    voiceFeedbackManager.PlayAudioFromPath("Button Audio/" + buttonName);
                });
            }
        }
    }

    public void PlayNameAudio(string barcode)
    {
        StartCoroutine(PlayAudioWithDelay(barcode, "DB/Data/Name/", 0.8f));
    }

    public void PlayManualAudio(string barcode)
    {
        StartCoroutine(PlayAudioWithDelay(barcode, "DB/Data/Manual/", 0.3f));
    }

    private IEnumerator PlayAudioWithDelay(string barcode, string path, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(LoadAudioData(barcode, path));
    }
}