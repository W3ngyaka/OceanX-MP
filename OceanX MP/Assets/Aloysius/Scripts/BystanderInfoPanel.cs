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
    public TMP_Text sciNameText;
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
        // IMAGE + name/role come from the big screen's own reveal sheet (same art as the NEW
        // ARRIVAL card). INFO is summarised from the tablet's SpeciesContent.csv so bystanders
        // see the same facts the tablet user is reading, just condensed.
        string key = !string.IsNullOrEmpty(sp.contentId) ? sp.contentId : sp.speciesName;
        var reveal  = RevealContentDB.Get(key);    // big-screen art + short blurb
        var content = SpeciesContentDB.Get(key);   // tablet's richer info

        if (nameText != null)
            nameText.text = (reveal != null && !string.IsNullOrWhiteSpace(reveal.speciesName)) ? reveal.speciesName : sp.speciesName;

        // Role and scientific name are separate text objects now.
        string sci = (content != null && !string.IsNullOrWhiteSpace(content.sciName)) ? content.sciName : sp.sciName;

        if (roleText != null)
        {
            roleText.text = (content != null && !string.IsNullOrWhiteSpace(content.role)) ? content.role
                          : (reveal != null && !string.IsNullOrWhiteSpace(reveal.role)) ? reveal.role : sp.tier;
        }

        if (sciNameText != null)
        {
            sciNameText.text = sci;
            sciNameText.gameObject.SetActive(!string.IsNullOrWhiteSpace(sci));
        }

        if (blurbText != null)
        {
            // Description only — summarised to the first sentence for glanceability.
            string desc = content != null ? content.description : null;
            if (string.IsNullOrWhiteSpace(desc))
                desc = (reveal != null && !string.IsNullOrWhiteSpace(reveal.firstAddedMessage)) ? reveal.firstAddedMessage : sp.addedMessage;
            blurbText.text = FirstSentence(desc);
        }

        if (speciesImage != null)
        {
            // Same photo the NEW ARRIVAL card shows.
            Sprite pic = (reveal != null) ? RevealContentDB.GetImage(reveal.imageFile) : null;
            if (pic == null && index >= 0 && index < cardImages.Count) pic = cardImages[index];
            speciesImage.sprite = pic;
            speciesImage.enabled = pic != null;
            speciesImage.preserveAspect = true;
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
