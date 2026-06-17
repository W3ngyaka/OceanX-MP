using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "OceanX/SpeciesData")]
public class SpeciesData : ScriptableObject
{
    public string speciesName;
    public string sciName;
    public string tier;
    [TextArea(2, 4)] public string description;
    public Sprite photo;
    public bool startUnlocked;
    public int minHealth;

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
