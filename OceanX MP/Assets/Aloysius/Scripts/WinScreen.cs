using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Full-screen win overlay. One per scene (host + tablet). Watches WinCondition.HasWon and
// fades in when the ecosystem is won: shows Alucia thanking the player, a title
public class WinScreen : MonoBehaviour
{
    public CanvasGroup group;            // fade this whole overlay
    public TMP_Text titleText;           // e.g. "ECOSYSTEM RESTORED"
    public TMP_Text messageText;         // Alucia's thank-you line
    public Image aluciaImage;            // Alucia portrait (use her win sprite)

    public string title = "ECOSYSTEM RESTORED";
    public string thankYou = "You did it — thank you for bringing the reef back to life!";
    public Sprite aluciaWinSprite;       

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
        else if (!won && _shown) SetVisible(false); // host reset -> hide on all screens
    }

    void Show()
    {
        _shown = true;
        if (UISoundManager.Instance != null) UISoundManager.Instance.Play(UISound.Win);
        if (titleText != null) titleText.text = title;
        if (messageText != null)
            messageText.text = AluciaLines.Get("win.thankyou", thankYou);
        if (aluciaImage != null && aluciaWinSprite != null) aluciaImage.sprite = aluciaWinSprite;
        SetVisible(true);
    }

    void SetVisible(bool visible, bool instant = false)
    {
        _shown = visible;
        gameObject.SetActive(true); // keep active so Update runs; group drives visibility
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
        // Only the host actually resets; on a client this is a no-op (guarded in WinCondition).
        if (WinCondition.Instance != null) WinCondition.Instance.Reset();
    }
}
