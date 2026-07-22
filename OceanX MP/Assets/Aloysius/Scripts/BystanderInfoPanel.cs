using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// LARGE SCREEN: mirrors what the tablet user is currently reading about, so bystanders can see it.
// Listens to EcosystemNetworkManagerGPU.OnViewedSpeciesChanged (-1 = nothing) and shows a
// summarised card (image + name + role + one line). Fades in/out; hides when the tablet closes.
public class BystanderInfoPanel : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup group;          // fade this panel
    public Image speciesImage;
    public TMP_Text nameText;
    public TMP_Text roleText;
    public TMP_Text blurbText;

    [Header("Data")]
    [Tooltip("All species, indexed to match the sim's species order (same list SpeciesAddedReveal uses).")]
    public List<SpeciesData> allSpecies = new List<SpeciesData>();
    [Tooltip("Card image per species, index-matched to allSpecies (optional).")]
    public List<Sprite> cardImages = new List<Sprite>();

    [Header("Anim")]
    public float fadeDuration = 0.3f;

    private Coroutine _fade;
    private bool _subscribed;
    private int _current = -1;

    void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
    }

    void Update()
    {
        if (_subscribed) return;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;
        _subscribed = true;
        net.OnViewedSpeciesChanged += HandleViewedChanged;
        HandleViewedChanged(net.ViewedSpecies); // catch current state on join
    }

    void OnDestroy()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null) net.OnViewedSpeciesChanged -= HandleViewedChanged;
    }

    void HandleViewedChanged(int index)
    {
        _current = index;
        if (index < 0 || index >= allSpecies.Count || allSpecies[index] == null) { Hide(); return; }
        Fill(allSpecies[index], index);
        Show();
    }

    void Fill(SpeciesData sp, int index)
    {
        // CSV entry is the same source the reveal card uses (RevealContent.csv).
        var e = RevealContentDB.Get(string.IsNullOrEmpty(sp.contentId) ? sp.speciesName : sp.contentId);

        if (nameText != null)
            nameText.text = (e != null && !string.IsNullOrWhiteSpace(e.speciesName)) ? e.speciesName : sp.speciesName;

        if (roleText != null)
            roleText.text = (e != null && !string.IsNullOrWhiteSpace(e.role)) ? e.role : sp.tier;

        if (blurbText != null)
        {
            string blurb = (e != null && !string.IsNullOrWhiteSpace(e.firstAddedMessage)) ? e.firstAddedMessage : sp.addedMessage;
            blurbText.text = FirstSentence(blurb);
        }

        if (speciesImage != null)
        {
            Sprite s2 = (index >= 0 && index < cardImages.Count) ? cardImages[index] : null;
            if (s2 == null && e != null && !string.IsNullOrWhiteSpace(e.imageFile))
                s2 = RevealContentDB.GetImage(e.imageFile);
            if (s2 != null) { speciesImage.sprite = s2; speciesImage.enabled = true; }
            else speciesImage.enabled = false;
        }
    }

    // Keep it short for a glanceable bystander card.
    static string FirstSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        int dot = text.IndexOf('.');
        return dot > 0 ? text.Substring(0, dot + 1) : text;
    }

    void Show(){ StartFade(1f); }
    void Hide(){ StartFade(0f); }

    void StartFade(float target)
    {
        if (group == null) return;
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeTo(target));
    }

    IEnumerator FadeTo(float target)
    {
        float from = group.alpha, t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, fadeDuration);
            group.alpha = Mathf.Lerp(from, target, t);
            yield return null;
        }
        group.alpha = target;
    }
}
