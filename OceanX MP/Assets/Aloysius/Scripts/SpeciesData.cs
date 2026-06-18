using UnityEngine;
using System.Collections.Generic;
using OceanX.BoidsGPU.Ecosystem;

[CreateAssetMenu(menuName = "OceanX/SpeciesData")]
public class SpeciesData : ScriptableObject
{
    public string speciesName;
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
}
