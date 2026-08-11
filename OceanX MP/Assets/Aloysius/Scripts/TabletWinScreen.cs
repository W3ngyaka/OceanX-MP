using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OceanX.BoidsGPU.Ecosystem;

// Tablet win overlay. Deliberately NOT a copy of the host's WinScreen: the host owns the
// celebration (Alucia, spectacle, visible to bystanders), and the tablet owns the DEBRIEF -- the
// reef this particular visitor built, and the rule they just discovered without being told.
//
// This is the one screen in the whole exhibit where being explicit is correct. Everywhere else the
// guidance is deliberately vague so the visitor works the ratios out themselves; here the game is
// over, so we finally name the lesson.
public class TabletWinScreen : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup group;
    public TMP_Text titleText;
    public TMP_Text lessonText;
    public TMP_Text checksText;
    public TMP_Text rosterText;
    
    public Button continueButton;

    public ExhibitReset exhibitReset;

    [Header("Copy")]
    public string title = "REEF RESTORED";

    
    [TextArea(2, 4)]
    public string lesson = "You did it with only a few predators. A real reef needs balance, not more fish — " +
                           "too many hunters and the whole web collapses.";

    [Tooltip("One line tying it to the real world. This is a museum; that is the point.")]
    [TextArea(2, 4)]
    public string realWorldNote = "Overfishing sharks and groupers can unravel a reef the same way.";

    [Header("Timing")]
    public float fadeDuration = 0.5f;

    private bool _shown;
    private Coroutine _fade;

    void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        SetVisible(false, instant: true);
        if (continueButton != null) continueButton.onClick.AddListener(RestartExhibit);
    }

    void Update()
    {
        var wc = WinCondition.Instance;
        if (wc == null) return;

        // Hiding on !Won covers the host reset: WinCondition.Reset() clears the latch, so a new
        // visitor never inherits the last one's debrief.
        if (!wc.Won && _shown) { SetVisible(false); return; }
        if (wc.Won && !_shown) Show();
    }

    void Show()
    {
        if (continueButton != null) continueButton.interactable = true;   // re-arm after a restart
        Compose();
        SetVisible(true);
    }

    // Full restart of the whole exhibit, on BOTH screens. ExhibitReset.TriggerReset sends an RPC to
    // the host, which clears populations and unlocks, plays its wipe, and drops both devices back to
    // the attract screen. This panel then hides itself automatically once WinCondition.Won clears.
    public void RestartExhibit()
    {
        if (!_shown) return;

        // One press only. The reset is a networked round-trip, so the panel stays up for a moment
        // afterwards -- without this a visitor can fire several resets before the first lands.
        if (continueButton != null) continueButton.interactable = false;

        var reset = exhibitReset != null
            ? exhibitReset
            : FindFirstObjectByType<ExhibitReset>(FindObjectsInactive.Include);

        if (reset != null)
        {
            reset.TriggerReset();
        }
        else
        {
            // No reset in the scene: fall back to just closing the panel rather than trapping the
            // visitor behind a dead button.
            Debug.LogWarning("[TabletWinScreen] No ExhibitReset found; dismissing locally instead.", this);
            Dismiss();
        }
    }

    // Local hide, with no effect on the exhibit. Kept so the panel can be closed without a reset.
    public void Dismiss()
    {
        if (_shown) SetVisible(false);
    }

    void Compose()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        var ui  = TabletEcosystemUIGPU.Instance;

        if (titleText  != null) titleText.text  = title;
        if (lessonText != null) lessonText.text = lesson;

        if (ui == null || ui.Ecosystem == null || net == null) return;

        // AGGREGATE, do not list. A twelve-row roster is twelve lines all making the same point;
        // the PATTERN is the lesson -- predators at one school, prey at five or six -- so collapse
        // to that pattern and let the numbers carry it. Museum visitors are standing up.
        int predMin = int.MaxValue, predMax = 0, predCount = 0;
        int preyMin = int.MaxValue, preyMax = 0, preyCount = 0;
        int present = 0, total = 0;

        foreach (var sp in ui.Ecosystem.Species)
        {
            if (sp == null) continue;
            total++;
            int idx = ui.GetSpeciesIndex(sp);
            int n = idx >= 0 ? net.GetPopulation(idx) : 0;
            if (n > 0) present++;

            bool isPredator = sp.Role == SpeciesRoleGPU.Apex || sp.Role == SpeciesRoleGPU.Mesopredator;
            if (isPredator) { predCount++; if (n < predMin) predMin = n; if (n > predMax) predMax = n; }
            else            { preyCount++; if (n < preyMin) preyMin = n; if (n > preyMax) preyMax = n; }
        }
        if (predMin == int.MaxValue) predMin = 0;
        if (preyMin == int.MaxValue) preyMin = 0;

        if (checksText != null)
            checksText.text = present + " of " + total + " species, all healthy";

        // Two lines that ARE the insight: the contrast between the predator and prey numbers.
        if (rosterText != null)
            rosterText.text = predCount + " predators\u2003" + Range(predMin, predMax) + " each\n"
                            + preyCount + " prey species\u2003" + Range(preyMin, preyMax) + " each";
    }

    private static string Range(int lo, int hi) => lo == hi ? lo.ToString() : lo + "\u2013" + hi;

    void SetVisible(bool visible, bool instant = false)
    {
        _shown = visible;
        if (group == null) return;

        // Blocks raycasts while up so a stray tap does not change the reef behind the debrief, but
        // the Continue button stays reachable because it lives inside this group.
        group.blocksRaycasts = visible;
        group.interactable = visible;

        if (instant || fadeDuration <= 0f)
        {
            if (_fade != null) { StopCoroutine(_fade); _fade = null; }
            group.alpha = visible ? 1f : 0f;
            return;
        }

        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeTo(visible ? 1f : 0f));
    }

    System.Collections.IEnumerator FadeTo(float target)
    {
        float from = group.alpha, t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, fadeDuration);
            group.alpha = Mathf.Lerp(from, target, t);
            yield return null;
        }
        group.alpha = target;
        _fade = null;
    }
}
