using UnityEngine;
using System.Collections.Generic;
using OceanX.BoidsGPU.Ecosystem;

[CreateAssetMenu(menuName = "OceanX/SpeciesData")]
public class SpeciesData : ScriptableObject
{
    public string speciesName;

    [Tooltip("Stable ID used to match this species to its row in SpeciesContent.csv (e.g. " +
             "\"blacktip_reef_shark\"). Leave blank to fall back to matching by speciesName. Using an id " +
             "means the display name can be reworded or translated without breaking the info card.")]
    public string contentId;

    public string sciName;
    public string tier;
    public bool startUnlocked;
    public int minHealth;

    [Tooltip("Link to the simulation species this card represents. Drives add/remove of the " +
             "real fish and supplies the live population used by the unlock requirements.")]
    public SpeciesDataGPU gpuSpecies;

    [System.Serializable]
    public class Requirement
    {
        public SpeciesData species;
        public int count;
    }
    public List<Requirement> requires = new List<Requirement>();

    [TextArea(2, 3)] public string hint1;
    [TextArea(2, 3)] public string hint2;
    [TextArea(2, 3)] public string hint3;
    [TextArea(2, 3)] public string addedMessage;

    [Header("Audio")]
    [Tooltip("Optional host/big-screen sting played the FIRST time this species is introduced (0 -> 1). " +
             "Leave empty to fall back to the generic music swell.")]
    public AudioClip introSound;
    [Range(0f, 1f)] public float introVolume = 1f;
}
