using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BystanderInfoPanel : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup group;
    public Image speciesImage;
    public TMP_Text nameText;
    public TMP_Text roleText;
    public TMP_Text sciNameText;
    public TMP_Text blurbText;

    [Header("Data")]
        public List<SpeciesData> allSpecies = new List<SpeciesData>();
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
        HandleViewedChanged(net.ViewedSpecies);
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

        string key = !string.IsNullOrEmpty(sp.contentId) ? sp.contentId : sp.speciesName;
        var reveal  = RevealContentDB.Get(key);
        var content = SpeciesContentDB.Get(key);

        if (nameText != null)
            nameText.text = (reveal != null && !string.IsNullOrWhiteSpace(reveal.speciesName)) ? reveal.speciesName : sp.speciesName;

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

            string desc = content != null ? content.description : null;
            if (string.IsNullOrWhiteSpace(desc))
                desc = (reveal != null && !string.IsNullOrWhiteSpace(reveal.firstAddedMessage)) ? reveal.firstAddedMessage : sp.addedMessage;
            blurbText.text = FirstSentence(desc);
        }

        if (speciesImage != null)
        {

            Sprite pic = (reveal != null) ? RevealContentDB.GetImage(reveal.imageFile) : null;
            if (pic == null && index >= 0 && index < cardImages.Count) pic = cardImages[index];
            speciesImage.sprite = pic;
            speciesImage.enabled = pic != null;
            speciesImage.preserveAspect = true;
        }
    }

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
