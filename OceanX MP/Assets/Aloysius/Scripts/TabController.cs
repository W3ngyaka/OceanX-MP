using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public Button button;
        public GameObject panel;
                public string title;
    }

    [Header("Tabs (button + panel, same order)")]
    public List<Tab> tabs = new List<Tab>();

    [Header("Header title")]
        public TMPro.TMP_Text titleText;

    [Header("Food Web prompt")]
        public GameObject foodWebPrompt;
        public int promptTabIndex = 0;

    [Header("Button tint")]
    public Color activeColor = new Color(0.20f, 0.55f, 0.95f, 1f);
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.12f);

    [Header("Transition")]
        public float slideDuration = 0.3f;
        public float slideDistance = 1000f;

    [Header("Start")]
    public int defaultTab = 0;

    private int _current = -1;
    private bool _animating;
    private readonly Dictionary<GameObject, Vector2> _homePos = new Dictionary<GameObject, Vector2>();

    private bool _initialized;

    void Start()
    {

        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null || !net.HasStarted)
        {
            if (net != null) net.OnStarted += InitializeTabs;
            else StartCoroutine(WaitForNet());
            return;
        }
        InitializeTabs();
    }

    System.Collections.IEnumerator WaitForNet()
    {
        while (EcosystemNetworkManagerGPU.Instance == null) yield return null;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net.HasStarted) InitializeTabs();
        else net.OnStarted += InitializeTabs;
    }

    void InitializeTabs()
    {
        if (_initialized) return;
        _initialized = true;
        foreach (var t in tabs)
        {
            if (t.panel == null) continue;
            var rt = t.panel.GetComponent<RectTransform>();
            if (rt != null && !_homePos.ContainsKey(t.panel))
                _homePos[t.panel] = rt.anchoredPosition;
        }

        for (int i = 0; i < tabs.Count; i++)
        {
            int idx = i;
            if (tabs[i].button != null)
                tabs[i].button.onClick.AddListener(() => Select(idx));
        }

        for (int i = 0; i < tabs.Count; i++)
        {
            bool on = (i == defaultTab);
            if (tabs[i].panel != null) tabs[i].panel.SetActive(on);
            TintButton(i, on);
        }
        _current = defaultTab;
        SetTitle(defaultTab);
        UpdatePrompt(defaultTab);

        if (tabs.Count > 0 && tabs[defaultTab].panel != null)
        {
            var g = tabs[defaultTab].panel.GetComponent<CurrentOrganismsGrid>();
            if (g != null) g.Refresh();
        }
    }

    public void ResetForNewSession()
    {
        if (!_initialized) return;
        StopAllCoroutines();
        _animating = false;
        for (int i = 0; i < tabs.Count; i++)
        {
            bool on = (i == defaultTab);
            if (tabs[i].panel != null)
            {
                if (_homePos.TryGetValue(tabs[i].panel, out var home))
                {
                    var rt = tabs[i].panel.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = home;
                }
                tabs[i].panel.SetActive(on);
            }
            TintButton(i, on);
        }
        _current = defaultTab;
        SetTitle(defaultTab);
        UpdatePrompt(defaultTab);
    }

    public void Select(int index)
    {
        if (_animating || index == _current || index < 0 || index >= tabs.Count) return;
        if (UISoundManager.Instance != null) UISoundManager.Instance.Play(UISound.TabSwitch);
        if (SpeciesInfoPanel.Instance != null) SpeciesInfoPanel.Instance.Clear();
        StartCoroutine(Transition(_current, index));
    }

    IEnumerator Transition(int from, int to)
    {
        _animating = true;
        float dir = (to > from) ? 1f : -1f;

        var outPanel = (from >= 0) ? tabs[from].panel : null;
        var inPanel = tabs[to].panel;

        for (int i = 0; i < tabs.Count; i++) TintButton(i, i == to);
        SetTitle(to);
        UpdatePrompt(to);

        Vector2 inHome = _homePos.ContainsKey(inPanel) ? _homePos[inPanel] : Vector2.zero;
        var inRT = inPanel.GetComponent<RectTransform>();
        inPanel.SetActive(true);
        inRT.anchoredPosition = inHome + new Vector2(dir * slideDistance, 0f);

        var grid = inPanel.GetComponent<CurrentOrganismsGrid>();
        if (grid != null) grid.Refresh();

        Vector2 outHome = Vector2.zero;
        RectTransform outRT = null;
        if (outPanel != null)
        {
            outRT = outPanel.GetComponent<RectTransform>();
            outHome = _homePos.ContainsKey(outPanel) ? _homePos[outPanel] : outRT.anchoredPosition;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / slideDuration;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            if (outRT != null)
                outRT.anchoredPosition = outHome + new Vector2(-dir * slideDistance * k, 0f);
            inRT.anchoredPosition = (inHome + new Vector2(dir * slideDistance, 0f))
                                    + new Vector2(-dir * slideDistance * k, 0f);
            yield return null;
        }

        inRT.anchoredPosition = inHome;
        if (outPanel != null)
        {
            outPanel.SetActive(false);
            outRT.anchoredPosition = outHome;
        }

        _current = to;
        _animating = false;
    }

    void UpdatePrompt(int index)
    {
        if (foodWebPrompt == null)
        {
            var t = transform.Find("prompt");
            if (t != null) foodWebPrompt = t.gameObject;
        }
        if (foodWebPrompt != null) foodWebPrompt.SetActive(index == promptTabIndex);
    }

    void SetTitle(int index)
    {
        if (titleText == null || index < 0 || index >= tabs.Count) return;
        if (!string.IsNullOrEmpty(tabs[index].title))
            titleText.text = tabs[index].title;
    }

    void TintButton(int i, bool active)
    {
        if (tabs[i].button == null) return;
        var img = tabs[i].button.GetComponent<Image>();
        if (img != null) img.color = active ? activeColor : inactiveColor;
    }
}
