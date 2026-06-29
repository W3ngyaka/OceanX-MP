using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Routes a species bubble's tap to TabletAddRemoveUIGPU so Add/Remove + population
/// target the tapped species — WITHOUT editing the UI-team's SpeciesBubble script.
///
/// Setup: select all the species bubbles and Add Component → this. No per-bubble
/// wiring or asset dragging: it reads the bubble's own SpeciesBubble.data.gpuSpecies
/// and tells the (singleton) controller which species is selected.
///
/// Works alongside SpeciesBubble's own pointer handling — both receive the tap and
/// do their own thing (the bubble fills the info panel; this sets the add/remove target).
/// </summary>
[RequireComponent(typeof(SpeciesBubble))]
public class BubbleSelectHook : MonoBehaviour, IPointerClickHandler
{
    private SpeciesBubble _bubble;

    private void Awake()
    {
        _bubble = GetComponent<SpeciesBubble>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_bubble == null || _bubble.data == null) return;
        if (TabletAddRemoveUIGPU.Instance != null)
            TabletAddRemoveUIGPU.Instance.Select(_bubble.data.gpuSpecies);
    }
}
