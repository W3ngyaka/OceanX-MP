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
    [Tooltip("Local dismiss so a visitor can keep playing. Does NOT reset the exhibit -- that stays " +
             "with the host, and a tablet that blocks all input until someone resets it is a soft-lock.")]
    public Button continueButton;

    [Header("Copy")]
    public string title = "REEF RESTORED";

    [Tooltip("The takeaway. Every affordance on this tablet pushes the visitor to ADD -- there is an " +
             "Add button, counts read like progress bars, species unlock as rewards. The winning reef " +
             "is the opposite: predators at one school each, prey well under their caps. Say so.")]
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
        if (continueButton != null) continueButton.onClick.AddListener(Dismiss);
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
        Compose();
        SetVisible(true);
    }

    // Local dismiss only. The visitor may want to keep tinkering after winning, and the exhibit
    // reset stays the host's job.
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
