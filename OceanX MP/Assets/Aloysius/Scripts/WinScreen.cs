using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WinScreen : MonoBehaviour
{
    public CanvasGroup group;
    public TMP_Text titleText;
    public TMP_Text messageText;
    public Image aluciaImage;

    public string title = "ECOSYSTEM RESTORED";

    // Fallback only. The live text comes from the alucia_lines sheet under the event key
    // "win.thankyou" (see Show below); this string is used only if that row is missing.
    public string thankYou = "You did it — thank you for bringing the reef back to life!";
    public Sprite aluciaWinSprite;

    [Header("Voice line (optional)")]
    [Tooltip("Alucia reading the win message aloud. Plays once alongside the card, on top of the UISound.Win " +
             "sting. Leave empty and nothing extra plays — the card still shows and the sting still fires.\n\n" +
             "Keep this in sync with the win.thankyou text in the sheet: if the wording there changes, the " +
             "clip has to be re-recorded or the two will disagree.")]
    public AudioClip thankYouVoice;
    [Range(0f, 1f)] public float thankYouVoiceVolume = 1f;

    public float fadeDuration = 0.6f;

    private bool _shown;
    private Coroutine _fade;

    void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        SetVisible(false, instant: true);
    }

    void Update()
    {
        var wc = WinCondition.Instance;
        if (wc == null) return;
        bool won = wc.Won;
        if (won && !_shown) Show();
        else if (!won && _shown) SetVisible(false);
    }

    void Show()
    {
        _shown = true;
        // No-op on the big screen: UISoundManager lives only in the Tablet scene, so Instance is
        // null here. The tablet fires UISound.Win from TabletWinScreen; the big screen's audio for
        // this moment is Alucia's thank-you below. Kept so it works if a manager is ever added.
        if (UISoundManager.Instance != null) UISoundManager.Instance.Play(UISound.Win);
        if (titleText != null) titleText.text = title;

        // GetLine, not Get: Get() returns text only, so the row's Audio column would be lost and
        // the thank-you would appear in silence. Looked up outside the messageText null-check so
        // she still speaks even if the label isn't wired.
        AluciaLines.Line winLine = AluciaLines.GetLine("win.thankyou", null);
        if (messageText != null)
            messageText.text = winLine.Found && !string.IsNullOrEmpty(winLine.Text)
                ? winLine.Text : thankYou;
        if (AluciaVoice.Instance != null) AluciaVoice.Instance.TryPlay(winLine.Audio);
        if (aluciaImage != null && aluciaWinSprite != null) aluciaImage.sprite = aluciaWinSprite;

        // Voice line, if one has been assigned. Routed through AdaptiveMusicSystem.PlayIntro because that
        // is the project's existing one-shot voice path (it ducks against the music bed and respects the
        // master volume), rather than a second AudioSource on this canvas.
        // The null check is deliberate and must stay: PlayIntro falls back to PlaySwell() when handed a
        // null clip, which would fire the swell sting on top of the UISound.Win sting above.
        if (thankYouVoice != null && AdaptiveMusicSystem.Instance != null)
            AdaptiveMusicSystem.Instance.PlayIntro(thankYouVoice, thankYouVoiceVolume);
        SetVisible(true);
    }

    void SetVisible(bool visible, bool instant = false)
    {
        _shown = visible;
        gameObject.SetActive(true);
        if (_fade != null) StopCoroutine(_fade);
        if (instant || group == null)
        {
            if (group != null) { group.alpha = visible ? 1f : 0f; group.blocksRaycasts = visible; group.interactable = visible; }
            if (!visible) _shown = false;
            return;
        }
        _fade = StartCoroutine(Fade(visible));
    }

    IEnumerator Fade(bool visible)
    {
        float from = group.alpha, to = visible ? 1f : 0f, t = 0f;
        group.blocksRaycasts = visible;
        group.interactable = visible;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, fadeDuration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        group.alpha = to;
        if (!visible) _shown = false;
    }

    void OnResetPressed()
    {

        if (WinCondition.Instance != null) WinCondition.Instance.Reset();
    }
}
