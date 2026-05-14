using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Attach to the tablet's UI canvas root.
// Reads the EcosystemDefinition to auto-build one Add/Remove button pair per species,
// then routes every tap through EcosystemNetworkBridge (ServerRpc) to the laptop.
//
// Scene setup (tablet scene / tablet canvas):
//   TabletCanvas
//     └─ TabletUINetworkController (this script)
//           ├─ ButtonContainer  (Vertical Layout Group)
//           └─ StatusLabel      (TMP_Text)
public class TabletUINetworkController : MonoBehaviour
{
    [Header("Simulation data")]
    [Tooltip("Same EcosystemDefinition asset used on the laptop — drag from Project window.")]
    [SerializeField] private EcosystemDefinition _ecosystem;

    [Header("UI")]
    [SerializeField] private Transform  _buttonContainer;
    [SerializeField] private TMP_Text   _statusLabel;

    [Header("Button prefab")]
    [Tooltip("A prefab with a Button component and a TMP_Text child named 'Label'.")]
    [SerializeField] private Button _buttonPrefab;

    // Cached reference — found after the network spawns it on the server.
    private EcosystemNetworkBridge _bridge;

    // Maps species index → live count label (optional, updated via ClientRpc).
    private readonly Dictionary<int, TMP_Text> _countLabels = new();

    private void Start()
    {
        BuildButtons();

        NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += _ => SetStatus("Disconnected from Trifold");
    }

    private void OnConnected(ulong clientId)
    {
        _bridge = FindFirstObjectByType<EcosystemNetworkBridge>();
        SetStatus("Connected to Trifold ✓");
    }

    // -------------------------------------------------------------------------
    // Auto-build buttons from EcosystemDefinition
    // -------------------------------------------------------------------------

    private void BuildButtons()
    {
        if (_ecosystem == null)
        {
            Debug.LogError("[TabletUINetworkController] EcosystemDefinition not assigned.");
            return;
        }

        for (int i = 0; i < _ecosystem.Species.Count; i++)
        {
            SpeciesDefinition species = _ecosystem.Species[i];
            if (species == null) continue;

            int index = i; // capture for lambda

            // Add button
            Button addBtn = Instantiate(_buttonPrefab, _buttonContainer);
            SetButtonLabel(addBtn, $"+ {species.SpeciesName}");
            addBtn.onClick.AddListener(() => SendAdd(index));

            // Remove button
            Button removeBtn = Instantiate(_buttonPrefab, _buttonContainer);
            SetButtonLabel(removeBtn, $"- {species.SpeciesName}");
            removeBtn.onClick.AddListener(() => SendRemove(index));
        }

        // Clear all button
        Button clearBtn = Instantiate(_buttonPrefab, _buttonContainer);
        SetButtonLabel(clearBtn, "Clear All");
        clearBtn.onClick.AddListener(SendClearAll);
    }

    private void SetButtonLabel(Button btn, string text)
    {
        TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = text;
    }

    // -------------------------------------------------------------------------
    // Send commands to laptop via ServerRpc
    // -------------------------------------------------------------------------

    private void SendAdd(int speciesIndex)
    {
        if (!CheckBridge()) return;
        _bridge.AddSpeciesServerRpc(speciesIndex, 1);
        SetStatus($"Adding {_ecosystem.Species[speciesIndex].SpeciesName}…");
    }

    private void SendRemove(int speciesIndex)
    {
        if (!CheckBridge()) return;
        _bridge.RemoveSpeciesServerRpc(speciesIndex, 1);
        SetStatus($"Removing {_ecosystem.Species[speciesIndex].SpeciesName}…");
    }

    private void SendClearAll()
    {
        if (!CheckBridge()) return;
        _bridge.ClearAllServerRpc();
        SetStatus("Clearing ecosystem…");
    }

    // -------------------------------------------------------------------------
    // Receive population sync from laptop (ClientRpc)
    // -------------------------------------------------------------------------

    public void OnPopulationSync(int speciesIndex, int newCount)
    {
        if (_countLabels.TryGetValue(speciesIndex, out TMP_Text label))
            label.text = newCount.ToString();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private bool CheckBridge()
    {
        if (_bridge != null) return true;

        // Try finding it again in case it just spawned
        _bridge = FindFirstObjectByType<EcosystemNetworkBridge>();

        if (_bridge == null)
        {
            SetStatus("Not connected yet — tap again in a moment");
            return false;
        }

        return true;
    }

    private void SetStatus(string message)
    {
        Debug.Log("[TabletUI] " + message);
        if (_statusLabel != null)
            _statusLabel.text = message;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
    }
}
