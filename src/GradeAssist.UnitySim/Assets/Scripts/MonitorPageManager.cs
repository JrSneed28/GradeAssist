using UnityEngine;

public sealed class MonitorPageManager : MonoBehaviour
{
    [Header("Pages")]
    public GameObject pageWork = null!;
    public GameObject pageTarget = null!;
    public GameObject pageSystem = null!;
    public GameObject pageBoot = null!;

    [Header("Navigation Buttons")]
    public GameObject tabWork = null!;
    public GameObject tabTarget = null!;
    public GameObject tabSystem = null!;

    [Header("Back Button")]
    public GameObject btnBack = null!;

    private GameObject? currentPage;
    private float bootTimer;
    private bool bootComplete;

    private void Start()
    {
        if (pageBoot != null)
        {
            ShowPage(pageBoot);
            bootTimer = 0f;
            bootComplete = false;
        }
        else
        {
            ShowPage(pageWork);
            bootComplete = true;
        }
    }

    private void Update()
    {
        // Boot splash timer
        if (!bootComplete && currentPage == pageBoot)
        {
            bootTimer += Time.deltaTime;
            if (bootTimer >= 2.0f)
            {
                bootComplete = true;
                ShowPage(pageWork);
            }
        }

        // Page cycling
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                CyclePage(forward: false);
            else
                CyclePage(forward: true);
        }

        // Direct page hotkeys
        if (Input.GetKeyDown(KeyCode.F1)) ShowPage(pageWork);
        if (Input.GetKeyDown(KeyCode.F2)) ShowPage(pageTarget);
        if (Input.GetKeyDown(KeyCode.F3)) ShowPage(pageSystem);

        // Back navigation
        if (Input.GetKeyDown(KeyCode.Escape) && currentPage != pageWork)
        {
            ShowPage(pageWork);
        }
    }

    public void ShowPage(GameObject? page)
    {
        if (currentPage != null) currentPage.SetActive(false);
        currentPage = page;
        if (currentPage != null) currentPage.SetActive(true);

        UpdateTabVisuals();
        UpdateBackButton();
    }

    private void CyclePage(bool forward)
    {
        if (currentPage == pageWork)
            ShowPage(forward ? pageTarget : pageSystem);
        else if (currentPage == pageTarget)
            ShowPage(forward ? pageSystem : pageWork);
        else if (currentPage == pageSystem)
            ShowPage(forward ? pageWork : pageTarget);
        else
            ShowPage(pageWork);
    }

    private void UpdateTabVisuals()
    {
        SetTabActive(tabWork, currentPage == pageWork);
        SetTabActive(tabTarget, currentPage == pageTarget);
        SetTabActive(tabSystem, currentPage == pageSystem);
    }

    private static void SetTabActive(GameObject? tab, bool active)
    {
        if (tab == null) return;
        var image = tab.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.color = active
                ? new Color32(0x2D, 0x5F, 0x8A, 0xFF)   // #2D5F8A active
                : new Color32(0x33, 0x33, 0x33, 0xFF);  // #333333 inactive
        }
        var text = tab.GetComponentInChildren<UnityEngine.UI.Text>();
        if (text != null)
        {
            text.color = active ? Color.white : new Color32(0xAA, 0xAA, 0xAA, 0xFF);
        }
    }

    private void UpdateBackButton()
    {
        if (btnBack == null) return;
        bool showBack = currentPage != pageWork && currentPage != pageBoot;
        btnBack.SetActive(showBack);
    }

    public void OnClickTabWork() => ShowPage(pageWork);
    public void OnClickTabTarget() => ShowPage(pageTarget);
    public void OnClickTabSystem() => ShowPage(pageSystem);
    public void OnClickBack() => ShowPage(pageWork);
}
