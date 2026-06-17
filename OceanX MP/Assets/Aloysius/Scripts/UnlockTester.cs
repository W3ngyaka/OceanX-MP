using UnityEngine;

/// <summary>
/// TESTING ONLY - remove before final build
/// Press keys to simulate adding species and test unlock flow
/// </summary>
public class UnlockTester : MonoBehaviour
{
    void Update()
    {
        if (GameState.Instance == null) return;

        // Press 1 - add Damselfish x2 + Mullet x2 (unlocks Scad)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            GameState.Instance.counts["Reticulated Damselfish"] = 2;
            GameState.Instance.counts["Fringelip Mullet"] = 2;
            GameState.Instance.CheckUnlocks();
            Debug.Log("Added Damselfish x2 + Mullet x2 — Scad should unlock");
        }

        // Press 2 - add more to unlock Trevally + Snapper
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            GameState.Instance.counts["Fringelip Mullet"] = 3;
            GameState.Instance.counts["Bullethead Parrotfish"] = 2;
            GameState.Instance.counts["Eyestripe Surgeonfish"] = 2;
            GameState.Instance.CheckUnlocks();
            Debug.Log("Added Mullet x3 + Parrotfish x2 + Surgeonfish x2 — Trevally + Snapper should unlock");
        }

        // Press 3 - unlock Ray
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            GameState.Instance.counts["Streaked Spinefoot"] = 2;
            GameState.Instance.CheckUnlocks();
            Debug.Log("Added Spinefoot x2 — Ray should unlock");
        }

        // Press 4 - unlock Grouper + Moray
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            GameState.Instance.counts["Yellowstripe Scad"] = 2;
            GameState.Instance.counts["Bluefin Trevally"] = 1;
            GameState.Instance.counts["Russells Snapper"] = 2;
            GameState.Instance.counts["Bluespotted Ray"] = 1;
            GameState.Instance.CheckUnlocks();
            Debug.Log("Added Scad x2 + Trevally x1 + Snapper x2 + Ray x1 — Grouper + Moray should unlock");
        }

        // Press 5 - unlock Shark
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            GameState.Instance.counts["Brown-Marbled Grouper"] = 1;
            GameState.Instance.counts["Giant Moray"] = 1;
            GameState.Instance.counts["Bluefin Trevally"] = 2;
            GameState.Instance.counts["Yellowstripe Scad"] = 3;
            GameState.Instance.CheckUnlocks();
            Debug.Log("Full ecosystem — Shark should unlock!");
        }

        // Press 0 - reset everything
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            foreach (var s in GameState.Instance.allSpecies)
            {
                GameState.Instance.counts[s.speciesName] = 0;
                GameState.Instance.unlocked[s.speciesName] = s.startUnlocked;
            }
            GameState.Instance.CheckUnlocks();
            Debug.Log("Reset — only primary consumers unlocked");
        }
    }
}
