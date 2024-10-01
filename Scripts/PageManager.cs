using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class PageManager : MonoBehaviour
{
    public GameObject[] pages;
    public float pageTransitionTime = 0.8f;
    public float pageSpacing = 1200f;
    public float maxShakeInterval = 1f;
    public Canvas canvas;
    public GameObject homePagePrefab;

    public VoiceFeedbackManager voiceFeedbackManager;
    public InputManager inputManager;
    public DBManager dbManager;

    public bool isAnimating { get; private set; }

    private GameObject pagesContainer;
    private bool isAutoMovingToHome = false;
    private int totalPages = 1;
    private int currentPageIndex = 0;

    void Awake()
    {
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        isAnimating = false;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float scalingFactor = canvasWidth / 1080f;
        pageSpacing *= scalingFactor;

        totalPages = 1;
        pages = new GameObject[totalPages];
        pagesContainer = new GameObject("PagesContainer");
        RectTransform containerRect = pagesContainer.AddComponent<RectTransform>();
        containerRect.SetParent(canvas.transform);

        SetContainerSizeToCanvas(containerRect);

        containerRect.localScale = Vector3.one;
        containerRect.anchorMin = new Vector2(0.5f, 1f);
        containerRect.anchorMax = new Vector2(0.5f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.anchoredPosition = Vector2.zero;

        if (homePagePrefab != null)
        {
            GameObject homePage = AddPage(homePagePrefab, isInitialPage: true);
            pages[0] = homePage;
            inputManager.AssignCameraButton(homePage);
        }
        LoadSavedPages();
    }

    private void SetContainerSizeToCanvas(RectTransform containerRect)
    {
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(canvasRect.rect.width, canvasRect.rect.height);
    }

    private void UpdatePagesContainerSize()
    {
        RectTransform containerRect = pagesContainer.GetComponent<RectTransform>();
        float width = (totalPages - 1) * pageSpacing + canvas.GetComponent<RectTransform>().rect.width;
        containerRect.sizeDelta = new Vector2(width, containerRect.sizeDelta.y);
    }

    public GameObject AddPage(GameObject pagePrefab, int pageIndex = -1, bool isInitialPage = false, string barcode = null)
    {
        if (pagePrefab == null)
        {
            return null;
        }

        GameObject newPage;

        if (isInitialPage)
        {
            if (pages == null || pages.Length == 0)
            {
                totalPages = 1;
                pages = new GameObject[totalPages];
            }

            pageIndex = 0;
            newPage = Instantiate(pagePrefab, pagesContainer.transform);
            pages[0] = newPage;
        }
        else
        {
            if (pageIndex == -1)
            {
                pageIndex = totalPages;
                totalPages++;
                System.Array.Resize(ref pages, totalPages);
            }
            else if (pageIndex >= pages.Length)
            {
                totalPages = pageIndex + 1;
                System.Array.Resize(ref pages, totalPages);
            }

            newPage = Instantiate(pagePrefab, pagesContainer.transform);
            pages[pageIndex] = newPage;

            if (barcode != null)
            {
                SavePage(pageIndex, pagePrefab.name, barcode);
            }
            else
            {
                SavePage(pageIndex, pagePrefab.name);
            }
        }

        RectTransform rectTransform = newPage.GetComponent<RectTransform>();

        rectTransform.SetParent(pagesContainer.transform);
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.sizeDelta = new Vector2(0, 0);
        rectTransform.anchoredPosition = new Vector2(pageIndex * pageSpacing, 50);

        UpdatePagesContainerSize();

        return newPage;
    }

    public GameObject AddNewPage(GameObject newUIGroupPrefab, string barcode)
    {
        return AddPage(newUIGroupPrefab, barcode: barcode);
    }

    public void MoveToPage(int pageIndex)
    {
        if (!isAutoMovingToHome && pageIndex == 0)
        {
            voiceFeedbackManager.StartCoroutine(voiceFeedbackManager.PlayAudioClip(0.8f, 8));
        }
        else if (pageIndex != 0)
        {
            string barcodeKey = "PageBarcode_" + pageIndex;
            string barcode = PlayerPrefs.GetString(barcodeKey, "");

            if (!string.IsNullOrEmpty(barcode))
            {
                dbManager.PlayNameAudio(barcode);
            }
        }
        ScrollToPage(pageIndex);

        if (isAutoMovingToHome && pageIndex == 0)
        {
            isAutoMovingToHome = false;
        }
    }

    private void ScrollToPage(int pageIndex)
    {
        if (isAnimating) return;

        float targetPositionX = -pageIndex * pageSpacing;
        RectTransform containerRect = pagesContainer.GetComponent<RectTransform>();

        isAnimating = true;

        containerRect.DOAnchorPosX(targetPositionX, pageTransitionTime)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(true)
            .OnComplete(() => isAnimating = false);

        currentPageIndex = pageIndex;
    }

    private void LoadSavedPages()
    {
        int savedTotalPages = PlayerPrefs.GetInt("TotalPages", 1);

        if (pages == null || pages.Length < savedTotalPages)
        {
            System.Array.Resize(ref pages, savedTotalPages);
        }
        totalPages = savedTotalPages;

        StartCoroutine(LoadSavedPrefabs());
    }

    private IEnumerator LoadSavedPrefabs()
    {
        yield return StartCoroutine(dbManager.LoadAssetBundle());

        for (int i = 1; i < totalPages; i++)
        {
            string prefabName = PlayerPrefs.GetString("PagePrefab_" + i, "");
            if (!string.IsNullOrEmpty(prefabName))
            {
                int pageIndex = i;
                string barcodeKey = "PageBarcode_" + i;
                string barcode = PlayerPrefs.GetString(barcodeKey, "");

                dbManager.LoadPrefabFromBundle(prefabName, barcode, pageIndex);
            }
            else
            {
                totalPages--;
                System.Array.Resize(ref pages, totalPages);
                PlayerPrefs.SetInt("TotalPages", totalPages);
                PlayerPrefs.Save();
                i--;
            }
        }
    }

    private void SavePage(int pageIndex, string prefabName = null, string barcode = null)
    {
        PlayerPrefs.SetInt("TotalPages", totalPages);

        if (!string.IsNullOrEmpty(prefabName))
        {
            PlayerPrefs.SetString("PagePrefab_" + pageIndex, prefabName);
        }
        else
        {
            PlayerPrefs.DeleteKey("PagePrefab_" + pageIndex);
        }

        if (barcode != null)
        {
            if (!string.IsNullOrEmpty(barcode))
            {
                PlayerPrefs.SetString("PageBarcode_" + pageIndex, barcode);
            }
            else
            {
                PlayerPrefs.DeleteKey("PageBarcode_" + pageIndex);
            }
        }
        PlayerPrefs.Save();
    }

    private IEnumerator HandlePageDelete()
    {
        RemovePage(currentPageIndex);
        voiceFeedbackManager.StartCoroutine(voiceFeedbackManager.PlayAudioClip(0.3f, 6));

        if (totalPages == 2)
        {
            totalPages--;
            SavePage(totalPages);
            PlayerPrefs.DeleteKey("PageBarcode_" + totalPages);
            System.Array.Resize(ref pages, totalPages);
            isAutoMovingToHome = true;
            MoveToPage(0);
            yield break;
        }

        if (currentPageIndex < totalPages - 1)
        {
            for (int i = currentPageIndex; i < totalPages - 1; i++)
            {
                pages[i] = pages[i + 1];
                RectTransform rect = pages[i].GetComponent<RectTransform>();
                rect.DOAnchorPosX(i * pageSpacing, 0.5f);

                string nextPrefabName = PlayerPrefs.GetString("PagePrefab_" + (i + 1), "");
                string nextBarcode = PlayerPrefs.GetString("PageBarcode_" + (i + 1), "");

                SavePage(i, nextPrefabName, nextBarcode);
            }
        }

        pages[totalPages - 1] = null;
        totalPages--;
        SavePage(totalPages);

        PlayerPrefs.DeleteKey("PagePrefab_" + totalPages);
        PlayerPrefs.DeleteKey("PageBarcode_" + totalPages);

        PlayerPrefs.SetInt("TotalPages", totalPages);
        PlayerPrefs.Save();

        System.Array.Resize(ref pages, totalPages);

        yield return null;
    }

    private void RemovePage(int pageIndex)
    {
        if (pageIndex == 0)
        {
            return;
        }

        if (pages[pageIndex] != null)
        {
            Destroy(pages[pageIndex]);
            pages[pageIndex] = null;
        }
    }

    public void RequestPageDelete()
    {
        StartCoroutine(HandlePageDelete());
    }

    public int GetTotalPages()
    {
        return totalPages;
    }

    public int GetCurrentPageIndex()
    {
        return currentPageIndex;
    }
}