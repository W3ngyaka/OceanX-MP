using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using OceanX.BoidsGPU.Ecosystem;

public class SpeciesBubble : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Sprite cardImage;
    public string speciesKey;

    [Tooltip("The species this bubble represents. Drag in the SpeciesDataGPU asset " +
             "(e.g. Bluefin Trevally_Data). Used to resolve the netcode index.")]
    public SpeciesDataGPU speciesData;

    public List<SpeciesBubble> prey = new List<SpeciesBubble>();
    public List<SpeciesBubble> predators = new List<SpeciesBubble>();

    private float holdDuration = 0.5f;
    private float holdTimer = 0f;
    private bool isHolding = false;
    private bool longPressTriggered = false;

    // Index into the ecosystem species list; -1 = no netcode target (cosmetic only).
    private int _speciesIndex = -1;

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(OnTap);

        ResolveSpeciesIndex();
    }

    private void ResolveSpeciesIndex()
    {
        if (speciesData == null) return;   // cosmetic-only bubble

        if (TabletEcosystemUIGPU.Instance == null)
        {
            Debug.LogWarning($"[SpeciesBubble] No TabletEcosystemUIGPU in scene — '{name}' can't resolve a netcode index.", this);
            return;
        }

        _speciesIndex = TabletEcosystemUIGPU.Instance.GetSpeciesIndex(speciesData);
        if (_speciesIndex < 0)
            Debug.LogWarning($"[SpeciesBubble] '{speciesData.SpeciesName}' is not in the ecosystem species list.", this);
    }

    void Update()
    {
        if (!isHolding) return;
        holdTimer += Time.unscaledDeltaTime;
        if (holdTimer >= holdDuration && !longPressTriggered)
        {
            longPressTriggered = true;
            Debug.Log($"Long press triggered on {gameObject.name}");
            FoodWebLines.Instance.ShowConnections(this);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        holdTimer = 0f;
        longPressTriggered = false;
        Debug.Log($"PointerDown on {gameObject.name}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"PointerUp on {gameObject.name}, longPressTriggered={longPressTriggered}");
        isHolding = false;

        if (longPressTriggered)
        {
            longPressTriggered = false;
            FoodWebLines.Instance.HideConnections();
        }
        else
        {
            OnTap();
        }
    }

    void OnTap()
    {
        if (ModalController.Instance != null && cardImage != null)
            ModalController.Instance.Open(cardImage, _speciesIndex);
    }
}
