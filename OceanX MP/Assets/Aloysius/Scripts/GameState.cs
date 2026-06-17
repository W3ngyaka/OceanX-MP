using UnityEngine;
using System.Collections.Generic;

public class GameState : MonoBehaviour
{
    public static GameState Instance;

    public List<SpeciesData> allSpecies;

    public Dictionary<string, int> counts = new Dictionary<string, int>();
    public Dictionary<string, bool> unlocked = new Dictionary<string, bool>();
    public Dictionary<string, int> tapCounts = new Dictionary<string, int>();
    public int ecoHealth = 50;

    void Awake()
    {
        Instance = this;
        foreach (var s in allSpecies)
        {
            counts[s.speciesName] = 0;
            unlocked[s.speciesName] = s.startUnlocked;
            tapCounts[s.speciesName] = 0;
        }
    }

    public void AddOne(SpeciesData s)
    {
        if (!unlocked[s.speciesName]) return;
        counts[s.speciesName]++;
        CheckUnlocks();
        RefreshAllBubbles();
    }

    public void RemoveOne(SpeciesData s)
    {
        if (counts[s.speciesName] > 0)
            counts[s.speciesName]--;
        CheckUnlocks();
        RefreshAllBubbles();
    }

public void CheckUnlocks()
    {
        foreach (var s in allSpecies)
        {
            if (unlocked[s.speciesName]) continue;
            if (ecoHealth < s.minHealth) continue;

            bool reqsMet = true;
            foreach (var r in s.requires)
                if (counts[r.species.speciesName] < r.count) { reqsMet = false; break; }

            if (reqsMet)
            {
                unlocked[s.speciesName] = true;
                NotificationManager.Instance?.ShowUnlocked(s);
            }
        }
        RefreshAllBubbles();
    }

    void RefreshAllBubbles()
    {
        var bubbles = FindObjectsByType<SpeciesBubble>(FindObjectsSortMode.None);
        foreach (var b in bubbles)
            b.Refresh();
    }
}
