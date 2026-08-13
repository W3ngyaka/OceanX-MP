using UnityEngine;
using System.Collections.Generic;
using OceanX.BoidsGPU.Ecosystem;

[CreateAssetMenu(menuName = "OceanX/SpeciesData")]
public class SpeciesData : ScriptableObject
{
    public string speciesName;

        public string contentId;

    public string sciName;
    public string tier;
    public bool startUnlocked;
    public int minHealth;

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
        public AudioClip introSound;
    [Range(0f, 1f)] public float introVolume = 1f;
}
