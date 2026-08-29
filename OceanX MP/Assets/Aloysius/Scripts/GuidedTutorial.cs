using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Optional STEP-BY-STEP guided tutorial. Self-contained: a dim overlay with a highlight ring over
// the current target, an instruction card, and per-step gating that advances only when the visitor
// performs the real action. Runs INSTEAD of TutorialPanel+nudges when TutorialMode is set to Guided.
//
// Kept deliberately separate from TutorialPanel so it's a clean fallback: flip the mode back and the
// old panel+nudges system runs untouched.
public class GuidedTutorial : MonoBehaviour
{
    public static GuidedTutorial Instance { get; private set; }

    public enum Step { Welcome, TapFish, ReadInfo, ViewDetails, HoldFish, PressAdd, WatchHealth, OrganismsTab, ExploreOrganisms, BackToFoodWeb, FreeExplore, Done }

    [Header("Refs")]
    public CanvasGroup group;         // whole overlay fade + input block
    public RectTransform highlight;   // ring/box that moves over the current target
    public TMP_Text instruction;      // the coaching text
    public TMP_Text tapToContinue;    // "tap to continue" hint for read-only steps
    public Button skipButton;         // let staff/visitors bail out

    [Header("Targets to spotlight")]
    public RectTransform tapFishTarget;    // a fish bubble
    public RectTransform addButtonTarget;  // the + button
    public RectTransform healthTarget;     // the health gauge
    public RectTransform organismsTabTarget;
    public RectTransform infoPanelTarget;      // the side Info panel (name + desc + add)
    public RectTransform viewDetailsTarget;    // the VIEW DETAILS button
    public RectTransform modalTarget;          // the full-screen species modal
    public RectTransform organismsPanelTarget; // the Current Organisms panel (container)
    public RectTransform organismsListTarget;  // the list content (rows only)

    [Header("Behaviour")]
    public float autoShowDelay = 0.6f;
    public float fadeDuration = 0.3f;
    public bool showOnStart = true;

    private Step _step;
    private bool _running;
    private bool _subscribed;
    private Coroutine _fade;
    private float _stepEnteredAt;
    private bool _modalOpened;

    // ---- static notifiers: action sites call these; no-ops unless the guided tutorial is waiting ----
    public static void NotifyTap()      { if (Instance != null && Instance._running) Instance.OnAction(Step.TapFish); }
    public static void NotifyHold()     { if (Instance != null && Instance._running) Instance.OnAction(Step.HoldFish); }
    public static void NotifyAdd()      { if (Instance != null && Instance._running) Instance.OnAction(Step.PressAdd); }
    public static void NotifyViewDetails() { if (Instance != null && Instance._running) Instance._modalOpened = true; }
    public static void NotifyTab(int i)
    {
        Debug.Log($"[GT DEBUG] NotifyTab({i}) running={(Instance!=null&&Instance._running)} step={(Instance!=null?Instance._step.ToString():"-")}");
        if (Instance == null || !Instance._running) return;
        if (i == 1) Instance.OnAction(Step.OrganismsTab);      // Organisms tab
        else if (i == 0) Instance.OnAction(Step.BackToFoodWeb); // Food Web tab
    }
    public static bool IsRunning => Instance != null && Instance._running;

    void Awake()
    {
        Instance = this;
        if (group == null) group = GetComponent<CanvasGroup>();
        SetVisible(false, true);
        if (skipButton != null) skipButton.onClick.AddListener(Finish);
    }

    void Update()
    {
        if (!_subscribed && showOnStart)
        {
            var net = EcosystemNetworkManagerGPU.Instance;
            if (net != null)
            {
                _subscribed = true;
                if (net.HasStarted) BeginSoon();
                else net.OnStarted += BeginSoon;
            }
        }

        // ViewDetails modal gate: while on this step, keep the spotlight synced to the modal, and
        // advance only after the modal has been opened AND closed again.
        if (_running && _step == Step.ViewDetails)
        {
            bool modalOpen = ModalController.Instance != null && ModalController.Instance.gameObject.activeSelf;
            if (modalOpen && _spotlit != (modalTarget != null ? modalTarget.gameObject : null))
                ApplyStep();   // switch spotlight to the modal
            if (_modalOpened && !modalOpen) { _modalOpened = false; Advance(); }
        }

                // Organisms fallback: if they reached the Organisms tab step and the panel is now showing,
        // advance even if the tab click didn't route through NotifyTab.
        if (_running && _step == Step.OrganismsTab && organismsPanelTarget != null
            && organismsPanelTarget.gameObject.activeInHierarchy
            && Time.unscaledTime - _stepEnteredAt > 0.5f)
            Advance();

                // Advance the tap-to-continue steps on any screen tap — but ignore taps for a short beat
        // after entering the step, so the tap that COMPLETED the previous action step doesn't also
        // skip through this one in the same frame.
        if (_running && (_step == Step.Welcome || _step == Step.ReadInfo || _step == Step.WatchHealth || _step == Step.ExploreOrganisms || _step == Step.FreeExplore))
            if (Time.unscaledTime - _stepEnteredAt > 0.35f)
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                    Advance();
    }

    void OnDestroy()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null) net.OnStarted -= BeginSoon;
    }

    void BeginSoon()
    {
        if (_running) return;
        StartCoroutine(BeginAfterDelay());
    }

    [Tooltip("Wait for the HOW TO PLAY panel to be dismissed before the guided steps start.")]
    public bool waitForPanel = true;

    IEnumerator BeginAfterDelay()
    {
        yield return new WaitForSecondsRealtime(autoShowDelay);
        // Let the HOW TO PLAY panel be read + dismissed first, so the two don't overlap.
        if (waitForPanel)
        {
            while (TutorialPanel.Instance != null && TutorialPanel.Instance.IsOpen)
                yield return null;
            // If the panel never showed (already dismissed / disabled), fall straight through.
            yield return new WaitForSecondsRealtime(0.35f);
        }
        Begin();
    }

    public void Begin()
    {
        _running = true;
        // Re-arm the Alucia gate: she stays quiet until this guided run finishes.
        var net0 = EcosystemNetworkManagerGPU.Instance;
        if (net0 != null) net0.SetTutorialDoneRpc(false);
        _step = Step.Welcome;
        SetVisible(true);
        ApplyStep();
    }

    void OnAction(Step expected)
    {
        Debug.Log($"[GT DEBUG] OnAction expected={expected} current={_step} -> {(_step==expected?"ADVANCE":"ignored")}");
        if (_step == expected) Advance();
    }

    void Advance()
    {
        Debug.Log($"[GT DEBUG] Advance from {_step}");
        if (UISoundManager.Instance != null) UISoundManager.Instance.Play(UISound.TutorialNext);
        _step = (Step)((int)_step + 1);
        if (_step == Step.Done) { Finish(); return; }
        ApplyStep();
    }

    void ApplyStep()
    {
        bool waitAction = false;
        RectTransform target = null;
        string text = "";

        switch (_step)
        {
            case Step.Welcome:
                text = "Welcome, reef keeper!\nThis reef has collapsed \u2014 let's bring it back to life."; break;
            case Step.TapFish:
                text = "Tap this fish to learn about it."; target = tapFishTarget; waitAction = true; break;
            case Step.ReadInfo:
                text = "This panel tells you about the species. Read it, then tap to continue."; target = infoPanelTarget; break;
            case Step.ViewDetails:
                // Spotlight the modal once it's open; otherwise spotlight the VIEW DETAILS button.
                bool modalOpen = ModalController.Instance != null && ModalController.Instance.gameObject.activeSelf;
                if (modalOpen) { text = "Read the full profile, then tap X to close."; target = modalTarget; }
                else { text = "Tap VIEW DETAILS to see the full species profile."; target = viewDetailsTarget; }
                waitAction = true; break;
            case Step.HoldFish:
                text = "Now HOLD a fish to see what it eats and what eats it."; target = tapFishTarget; waitAction = true; break;
            case Step.PressAdd:
                text = "Press + to add it to the reef."; target = infoPanelTarget; waitAction = true; break;
            case Step.WatchHealth:
                text = "Watch the Ecosystem Health rise. Add the right mix to restore balance."; target = healthTarget; break;
            case Step.OrganismsTab:
                text = "Open the ORGANISMS tab to see your reef."; target = organismsTabTarget; waitAction = true; break;
            case Step.ExploreOrganisms:
                text = "This is your reef. Here you can check each species and remove any you don't want. Take a look, then tap to continue."; target = null; break;   // dim hidden this step
            case Step.BackToFoodWeb:
                text = "Now head back to the FOOD WEB tab to keep building your reef."; target = organismsTabTarget; waitAction = true; break;
            case Step.FreeExplore:
                text = "That's it \u2014 you're ready! Add species, watch the balance, and bring the reef back to life."; break;
        }

        _stepEnteredAt = Time.unscaledTime;
        if (_step == Step.ViewDetails && (ModalController.Instance == null || !ModalController.Instance.gameObject.activeSelf)) _modalOpened = false;
        if (instruction != null) instruction.text = text;
        if (tapToContinue != null) tapToContinue.gameObject.SetActive(!waitAction);
        MoveHighlight(target);

        // On the 'explore your reef' step, hide the dim entirely so the organisms panel is fully
        // visible (it's a masked scroll panel that can't be elevated above the dim like the others).
        var dimObj = transform.Find("Dim");
        if (dimObj != null) dimObj.gameObject.SetActive(_step != Step.ExploreOrganisms);

        // Dim always blocks; the spotlit element sits ABOVE the dim with its own raycaster, so it
        // still receives taps while everything behind stays blocked.
        if (group != null) group.blocksRaycasts = true;
    }

    // Spotlight by ELEVATION: give the focused element a Canvas with a high sorting order so it
    // renders ABOVE the dim. We NEVER destroy the Canvas mid-tutorial (that made bubbles flicker)
    // — we just toggle overrideSorting off, and strip everything we added in CleanupTouched().
    private GameObject _spotlit;
    private Canvas _spotCanvas;
    private readonly System.Collections.Generic.List<GameObject> _touched = new System.Collections.Generic.List<GameObject>();

    void MoveHighlight(RectTransform target)
    {
        ClearSpotlight();
        if (highlight != null) highlight.gameObject.SetActive(false);
        if (target == null) return;

        _spotlit = target.gameObject;
        _spotCanvas = _spotlit.GetComponent<Canvas>();
        if (_spotCanvas == null)
        {
            _spotCanvas = _spotlit.AddComponent<Canvas>();
            if (_spotlit.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                _spotlit.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if (!_touched.Contains(_spotlit)) _touched.Add(_spotlit);
        }
        _spotCanvas.overrideSorting = true;
        _spotCanvas.sortingOrder = 999;
    }

    void CleanupTouched()
    {
        // Non-destructive: leaving the Canvas/raycaster in place but returning them to normal
        // avoids tearing down the UI raycast tree (destroying Canvases mid-frame killed taps).
        foreach (var go in _touched)
        {
            if (go == null) continue;
            var cv = go.GetComponent<Canvas>();
            if (cv != null) cv.overrideSorting = false;   // back to inheriting parent sort
        }
        _touched.Clear();
    }

    void ClearSpotlight()
    {
        if (_spotCanvas != null) _spotCanvas.overrideSorting = false;
        _spotlit = null;
        _spotCanvas = null;
    }

    public void Finish()
    {
        Debug.Log("[GT DEBUG] Finish() called");
        _running = false;
        // If the visitor skipped while the species modal was open, close it too.
        if (ModalController.Instance != null && ModalController.Instance.gameObject.activeSelf)
            ModalController.Instance.Close();
        _modalOpened = false;
        ClearSpotlight();
        CleanupTouched();
        var d = transform.Find("Dim"); if (d != null) d.gameObject.SetActive(true);
        SetVisible(false, instant: true);   // hard hide — no lingering dim on Skip
        // Tell the host Alucia can talk now (same signal the panel used).
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null) net.SetTutorialDoneRpc(true);
    }

    void SetVisible(bool v, bool instant = false)
    {
        if (group == null) return;
        group.interactable = v;
        group.blocksRaycasts = v;   // set immediately — don't wait for the fade, or the invisible
                                    // overlay keeps blocking every tap after Finish().
        if (instant) { group.alpha = v ? 1f : 0f; return; }
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeTo(v ? 1f : 0f));
    }

    IEnumerator FadeTo(float target)
    {
        float from = group.alpha, t = 0f;
        while (t < 1f) { t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, fadeDuration); group.alpha = Mathf.Lerp(from, target, t); yield return null; }
        group.alpha = target;
    }
}
