using UnityEngine;
using System.Collections.Generic;

public sealed class MonitorInputRouter : MonoBehaviour
{
    [Header("Page Mapping")]
    public MonitorPageManager pageManager = null!;
    public List<GameObject> buttonObjects = new List<GameObject>();
    public List<GameObject> pageObjects = new List<GameObject>();

    private Dictionary<GameObject, GameObject> buttonToPage = new Dictionary<GameObject, GameObject>();

    private void Start()
    {
        for (int i = 0; i < buttonObjects.Count && i < pageObjects.Count; i++)
        {
            buttonToPage[buttonObjects[i]] = pageObjects[i];
        }
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;
            if (buttonToPage.TryGetValue(hitObj, out GameObject page))
            {
                pageManager?.ShowPage(page);
            }
        }
    }
}
