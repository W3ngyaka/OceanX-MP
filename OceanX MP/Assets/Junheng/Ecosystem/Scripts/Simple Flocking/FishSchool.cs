using UnityEngine;
using System.Collections.Generic;

public class FishSchool : MonoBehaviour
{
  [Header("Spawning")]
  public GameObject fishPrefab;
  public int fishCount = 20;
  public float spawnRadius = 2.0f;

  [Header("Flocking Settings")]
  public Fish.Settings settings = new Fish.Settings();
  public Fish.DebugSettings debugSettings = new Fish.DebugSettings();

  [Header("Path")]
  public Trace trace;

  private static List<FishSchool> allSchools = new List<FishSchool>();

  void Awake()
  {
    allSchools.Add(this);
  }

  void OnDestroy()
  {
    allSchools.Remove(this);
  }

  void Start()
  {
    settings.Trace = trace;

    for( int i = 0; i < fishCount; i++ )
    {
      var offset = Random.insideUnitSphere * spawnRadius;
      var go = Instantiate( fishPrefab, transform.position + offset, Random.rotation );
      go.transform.parent = transform;

      var fish = go.GetComponent<Fish>();
      if( fish != null )
      {
        fish.SettingsRef = settings;
        fish.DebugSettingsRef = debugSettings;
      }
    }
  }

  // Called by Fish.Start() to retrieve the school settings for a given fish GameObject
  public static Fish.Settings GetSettings( GameObject fishObj )
  {
    foreach( var school in allSchools )
    {
      if( fishObj.transform.IsChildOf(school.transform) )
        return school.settings;
    }
    return null;
  }
}
