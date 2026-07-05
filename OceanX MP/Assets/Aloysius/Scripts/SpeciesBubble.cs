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
    [Tooltip("Name label under the bubble. Auto-found if left empty. Shows '???' when locked.")]
    public TMPro.TMP_Text nameLabel;
    [Tooltip("Text shown in place of the species name while locked.")]
    public string lockedNameText = "???";
    [Tooltip("The fish image. Auto-found (child whose name contains 'IMAGE') if empty. Greyed when locked.")]
    public UnityEngine.UI.Image fishImage;
    [Tooltip("Tint applied to the fish image while locked.")]
    public Color lockedTint = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Food Web")]
    public List<SpeciesBubble> prey = new List<SpeciesBubble>();
    public List<SpeciesBubble> predators = new List<SpeciesBubble>();

    [Header("Hold-to-reveal ring")]
    [Tooltip("Ring sprite drawn as a radial fill while the user holds the bubble. " +
             "If holdRing is left empty, a ring child is auto-created from this sprite at runtime.")]
    public Sprite holdRingSprite;
    [Tooltip("The radial-fill ring Image. Auto-created from holdRingSprite if left empty.")]
    public Image holdRing;
    [Tooltip("Tint of the progress ring as it fills.")]
    public Color holdRingColor = new Color(0.15f, 0.9f, 1f, 1f);
    [Tooltip("Ring size relative to the bubble (1 = same size).")]
    public float holdRingScale = 1.06f;

    private float holdDuration = 0.5f;
    private float holdTimer = 0f;
    private bool isHolding = false;
    private bool longPressTriggered = false;
    private Vector3 baseScale = Vector3.one;
    private Coroutine punchRoutine;
    private bool locked = false;

    void Start()
    {
        baseScale = transform.localScale;
        Refresh();
        EnsureHoldRing();
    }

    // Build a radial-fill ring child once, so no per-bubble manual wiring is needed.
    void EnsureHoldRing()
    {
        if (holdRing != null || holdRingSprite == null) return;

        var go = new GameObject("HoldProgressRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);

        var brt = transform as RectTransform;
        Vector2 size = brt != null ? brt.rect.size : new Vector2(140f, 140f);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size * holdRingScale;
        rt.SetAsLastSibling(); // draw on top of the bubble art

        holdRing = go.GetComponent<Image>();
        holdRing.sprite = holdRingSprite;
        holdRing.type = Image.Type.Filled;
        holdRing.fillMethod = Image.FillMethod.Radial360;
        holdRing.fillOrigin = (int)Image.Origin360.Top;
        holdRing.fillClockwise = true;
        holdRing.fillAmount = 0f;
        holdRing.color = holdRingColor;
        holdRing.raycastTarget = false;
        holdRing.enabled = false;
    }

    void SetHoldRing(float progress)
    {
        if (holdRing == null) return;
        holdRing.enabled = true;
        holdRing.fillAmount = Mathf.Clamp01(progress);
    }

    void HideHoldRing()
    {
        if (holdRing == null) return;
        holdRing.enabled = false;
        holdRing.fillAmount = 0f;
    }

    public void Refresh()
    {
        if (data == null) return;

        // Prefer the live unlock manager; fall back to GameState, else the asset default.
        bool isUnlocked;
        if (EcosystemUnlockManagerGPU.Instance != null)
            isUnlocked = EcosystemUnlockManagerGPU.Instance.IsUnlocked(data);
        else if (GameState.Instance != null && GameState.Instance.unlocked.ContainsKey(data.speciesName))
            isUnlocked = GameState.Instance.unlocked[data.speciesName];
        else
            isUnlocked = data.startUnlocked;

        locked = !isUnlocked;

        if (lockOverlay != null)
            lockOverlay.SetActive(locked);

        // Auto-find the name label (first TMP child) if not assigned.
        if (nameLabel == null)
            nameLabel = GetComponentInChildren<TMPro.TMP_Text>(true);

        // Show '???' while locked (Play only); real name once unlocked.
        if (nameLabel != null)
            nameLabel.text = (locked && Application.isPlaying) ? lockedNameText : data.speciesName;

        // Auto-find the fish image: the Image child that is NOT the 'Overpopulated' status
        // overlay and NOT inside the lock overlay. (Bubbles name the fish image inconsistently,
        // so we identify it by exclusion rather than by name.)
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
        if (locked) { HideHoldRing(); return; }
        if (!isHolding) return;

        holdTimer += Time.unscaledDeltaTime;

        if (!longPressTriggered)
        {
            // Grow the ring toward completion so holding reads as a deliberate gesture.
            float progress = Mathf.Clamp01(holdTimer / holdDuration);
            SetHoldRing(progress);

            if (progress >= 1f)
            {
                longPressTriggered = true;
                HideHoldRing(); // the food-web lines are the feedback now

                if (FoodWebLines.Instance != null)
                    FoodWebLines.Instance.ShowConnections(this);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (locked) return;

        isHolding = true;
        holdTimer = 0f;
        longPressTriggered = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (locked)
        {
            ShowLockedHint();
            return;
        }

        isHolding = false;
        HideHoldRing(); // clear a partial ring if the user let go early

        if (longPressTriggered)
        {
            longPressTriggered = false;

            if (FoodWebLines.Instance != null)
                FoodWebLines.Instance.HideConnections();
        }
        else
        {
            OnTap();
        }
    }

    void OnTap()
    {
        if (locked)
        {
            ShowLockedHint();
            return;
        }

        if (punchRoutine != null) StopCoroutine(punchRoutine);
        transform.localScale = baseScale;
        punchRoutine = StartCoroutine(TapPunch());   // tap-punch animation (Aloysius)

        // Resolve this species' netcode index from its sim link so the modal's Add/Remove
        // buttons drive the real simulation (and the population number shows). -1 = cosmetic only.
        int speciesIndex = -1;
        if (data != null && data.gpuSpecies != null && TabletEcosystemUIGPU.Instance != null)
            speciesIndex = TabletEcosystemUIGPU.Instance.GetSpeciesIndex(data.gpuSpecies);

        // Fill the right-side info panel (summary). Its 'View Details' button opens the full modal.
        if (SpeciesInfoPanel.Instance != null)
            SpeciesInfoPanel.Instance.Show(data, cardImage, speciesIndex);
        else if (ModalController.Instance != null && cardImage != null)
            ModalController.Instance.Open(cardImage, speciesIndex); // fallback: open modal directly
    }

    void PlayPunch()
    {
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        transform.localScale = baseScale;
        punchRoutine = StartCoroutine(TapPunch());
    }

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

        int taps;
        if (EcosystemUnlockManagerGPU.Instance != null)
        {
            taps = EcosystemUnlockManagerGPU.Instance.RegisterLockedTap(data);
        }
        else if (GameState.Instance != null)
        {
            taps = GameState.Instance.tapCounts.ContainsKey(data.speciesName)
                ? GameState.Instance.tapCounts[data.speciesName]
                : 0;
            GameState.Instance.tapCounts[data.speciesName] = taps + 1;
        }
        else
        {
            taps = 0;
        }

        string[] hints = { data.hint1, data.hint2, data.hint3 };
        int level = Mathf.Min(taps, hints.Length - 1);

        string[] labels = { "Hint", "Clearer hint", "Almost there" };
        string label = labels[Mathf.Min(taps, labels.Length - 1)];

        if (LockedHintPanel.Instance != null)
            LockedHintPanel.Instance.Show(label, hints[level]);
    }
}
