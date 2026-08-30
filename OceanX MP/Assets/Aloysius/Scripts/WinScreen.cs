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

    [Tooltip("Alucia, so the corner bubble can be silenced while this card is up. Leave empty and it is " +
             "found in the scene on Awake.")]
    public AluciaController alucia;

    public float fadeDuration = 0.6f;

    private bool _shown;
    private Coroutine _fade;

    void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (alucia == null) alucia = FindFirstObjectByType<AluciaController>();
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

        // MUST come before the thank-you plays. This card shows Alucia full-size across the whole
        // display, so the little corner bubble talking underneath reads as two of her. SetMuted clears
        // that bubble and stops whatever line it was in the middle of — and because it stops the shared
        // AluciaVoice source, calling it AFTER TryPlay below would cut off the thank-you itself. Mute
        // first, then speak: muting only gates Alucia's own Say(), not this direct TryPlay.
        if (alucia != null) alucia.SetMuted(true);

        // GetLine, not Get: Get() returns text only, so the row's Audio column would be lost and
        // the thank-you would appear in silence. Looked up outside the messageText null-check so
        // she still speaks even if the label isn't wired.
        AluciaLines.Line winLine = AluciaLines.GetLine("win.thankyou", null);
        if (messageText != null)
            messageText.text = winLine.Found && !string.IsNullOrEmpty(winLine.Text)
                ? winLine.Text : thankYou;
        if (AluciaVoice.Instance != null) AluciaVoice.Instance.TryPlay(winLine.Audio);
        if (aluciaImage != null && aluciaWinSprite != null) aluciaImage.sprite = aluciaWinSprite;

        SetVisible(true);
    }

    void SetVisible(bool visible, bool instant = false)
    {
        _shown = visible;
        gameObject.SetActive(true);
        // Card going away: hand Alucia back her voice. Show() muted her, and without this she would
        // stay silent for the rest of the session. Safe on the exhibit reset path too, which re-mutes
        // her itself via ResetForNewSession.
        if (!visible && alucia != null) alucia.SetMuted(false);
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
