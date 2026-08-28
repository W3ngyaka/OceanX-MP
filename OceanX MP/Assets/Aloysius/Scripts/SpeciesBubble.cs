using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SpeciesBubble : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Species")]
    public SpeciesData data;

    [Header("Visuals")]
    public Sprite cardImage;
    public GameObject lockOverlay;
    public GameObject glowRing;
        public TMPro.TMP_Text nameLabel;
        public string lockedNameText = "???";
        public UnityEngine.UI.Image fishImage;
        public Color lockedTint = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Overpopulation")]
        public GameObject overpopulatedOverlay;
        public float overpopCheckInterval = 0.4f;

    [Header("Food Web")]
    public List<SpeciesBubble> prey = new List<SpeciesBubble>();
    public List<SpeciesBubble> predators = new List<SpeciesBubble>();

    private Vector3 baseScale = Vector3.one;
    private Coroutine punchRoutine;
    private bool _wasLocked = true;   // for detecting the unlock moment
    private bool _lockInit = false;
    private bool locked = false;

    private SpeciesBubbleHoldRing _holdRing;

    private int _speciesIndex = -2;
    private float _overpopCheckTimer;
    private bool _overpopShown;

    void OnEnable()
    {
        if (Application.isPlaying) Refresh();
    }

    void Start()
    {
        baseScale = transform.localScale;
        Refresh();
        _holdRing = GetComponentInChildren<SpeciesBubbleHoldRing>(true);

        if (overpopulatedOverlay != null) overpopulatedOverlay.SetActive(false);
        _overpopShown = false;
    }

    public void Refresh()
    {
        if (data == null) return;

        bool isUnlocked;
        if (EcosystemUnlockManagerGPU.Instance != null)
            isUnlocked = EcosystemUnlockManagerGPU.Instance.IsUnlocked(data);
        else if (GameState.Instance != null && GameState.Instance.unlocked.ContainsKey(data.speciesName))
            isUnlocked = GameState.Instance.unlocked[data.speciesName];
        else
            isUnlocked = data.startUnlocked;

        locked = !isUnlocked;

        // Celebrate the unlock moment with a pop (only on a real locked->unlocked flip at runtime).
        if (Application.isPlaying)
        {
            if (_lockInit && _wasLocked && !locked) PlayUnlockPop();
            _wasLocked = locked;
            _lockInit = true;
        }

        if (lockOverlay != null)
            lockOverlay.SetActive(locked);

        if (overpopulatedOverlay == null)
        {
            var op = transform.Find("Overpopulated");
            if (op != null) overpopulatedOverlay = op.gameObject;
        }
        if (locked) UpdateOverpopulation();

        if (nameLabel == null)
            nameLabel = GetComponentInChildren<TMPro.TMP_Text>(true);

        if (nameLabel != null)
            nameLabel.text = (locked && Application.isPlaying) ? lockedNameText : data.speciesName;

        if (fishImage == null)
        {
            foreach (Transform c in transform)
            {
                if (c.name == "Overpopulated") continue;
                if (lockOverlay != null && c == lockOverlay.transform) continue;
                var im = c.GetComponent<UnityEngine.UI.Image>();
                if (im != null) { fishImage = im; break; }
            }
        }
        if (fishImage != null)
            fishImage.color = (locked && Application.isPlaying) ? lockedTint : Color.white;

        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.interactable = true;
    }

    void Update()
    {
        _overpopCheckTimer += Time.unscaledDeltaTime;
        if (_overpopCheckTimer >= overpopCheckInterval)
        {
            _overpopCheckTimer = 0f;
            UpdateOverpopulation();
        }

        if (locked) { if (_holdRing != null) _holdRing.CancelHold(); return; }
        if (_holdRing != null) _holdRing.Tick();
    }

    void ResolveSpeciesIndex()
    {
        if (_speciesIndex != -2) return;
        _speciesIndex = (data != null && data.gpuSpecies != null && TabletEcosystemUIGPU.Instance != null)
            ? TabletEcosystemUIGPU.Instance.GetSpeciesIndex(data.gpuSpecies)
            : -1;
    }

    void UpdateOverpopulation()
    {
        if (overpopulatedOverlay == null) return;

        bool over = false;
        if (!locked && Application.isPlaying)
        {
            ResolveSpeciesIndex();
            var net = EcosystemNetworkManagerGPU.Instance;
            if (_speciesIndex >= 0 && net != null)
                over = net.IsOverpopulated(_speciesIndex);
        }

        if (over != _overpopShown)
        {
            _overpopShown = over;
            overpopulatedOverlay.SetActive(over);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (locked) return;
        if (_holdRing != null) _holdRing.BeginHold();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (locked)
        {
            if (UISoundManager.Instance != null)
                UISoundManager.Instance.Play(UISound.Locked);
            ShowLockedHint();
            return;
        }

        bool wasHold = _holdRing != null && _holdRing.EndHold();
        if (wasHold) { ContextNudge.DismissId("hold"); GuidedTutorial.NotifyHold(); }
        if (!wasHold)
            OnTap();
    }

    void OnTap()
    {
        ContextNudge.DismissId("tap");
        GuidedTutorial.NotifyTap();
        if (locked)
        {
            ShowLockedHint();
            return;
        }

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayTap();

        if (punchRoutine != null) StopCoroutine(punchRoutine);
        transform.localScale = baseScale;
        punchRoutine = StartCoroutine(TapPunch());

        int speciesIndex = -1;
        if (data != null && data.gpuSpecies != null && TabletEcosystemUIGPU.Instance != null)
            speciesIndex = TabletEcosystemUIGPU.Instance.GetSpeciesIndex(data.gpuSpecies);

        if (SpeciesInfoPanel.Instance != null)
            SpeciesInfoPanel.Instance.Show(data, cardImage, speciesIndex);
        else if (ModalController.Instance != null)
            ModalController.Instance.Open(data);
    }

    void PlayPunch()
    {
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        transform.localScale = baseScale;
        punchRoutine = StartCoroutine(TapPunch());
    }

    // Bigger, bouncier pop when a species unlocks — distinct from the subtle tap punch.
    public void PlayUnlockPop()
    {
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        transform.localScale = baseScale;
        punchRoutine = StartCoroutine(UnlockPop());
    }

    System.Collections.IEnumerator UnlockPop()
    {
        Vector3 original = baseScale;
        Vector3 big = original * 1.5f;      // grow well past normal
        Vector3 settle = original * 1.08f;  // slight overshoot on the way back
        // grow
        float t = 0f;
        while (t < 1f) { t += Time.unscaledDeltaTime / 0.18f; transform.localScale = Vector3.LerpUnclamped(original, big, EaseOut(Mathf.Clamp01(t))); yield return null; }
        // spring back past normal
        t = 0f;
        while (t < 1f) { t += Time.unscaledDeltaTime / 0.16f; transform.localScale = Vector3.Lerp(big, settle, EaseOut(Mathf.Clamp01(t))); yield return null; }
        // settle to normal
        t = 0f;
        while (t < 1f) { t += Time.unscaledDeltaTime / 0.12f; transform.localScale = Vector3.Lerp(settle, original, Mathf.Clamp01(t)); yield return null; }
        transform.localScale = original;
        punchRoutine = null;
    }

    static float EaseOut(float x) => 1f - Mathf.Pow(1f - x, 3f);

    System.Collections.IEnumerator TapPunch()
    {
        Vector3 original = baseScale;
        Vector3 big = original * 1.2f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.1f;
            transform.localScale = Vector3.Lerp(original, big, Mathf.Clamp01(t));
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.15f;
            transform.localScale = Vector3.Lerp(big, original, Mathf.Clamp01(t));
            yield return null;
        }

        transform.localScale = original;
        punchRoutine = null;
    }

    void ShowLockedHint()
    {
        if (data == null) return;

        PlayPunch();
        if (SpeciesInfoPanel.Instance != null)
            SpeciesInfoPanel.Instance.ShowLocked(data);

        if (EcosystemUnlockManagerGPU.Instance != null)
        {
            EcosystemUnlockManagerGPU.Instance.RegisterLockedTap(data);
        }
        else if (GameState.Instance != null)
        {
            int taps = GameState.Instance.tapCounts.ContainsKey(data.speciesName)
                ? GameState.Instance.tapCounts[data.speciesName]
                : 0;
            GameState.Instance.tapCounts[data.speciesName] = taps + 1;
        }
    }
}
