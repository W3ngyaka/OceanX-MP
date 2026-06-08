using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tablet startup screen.
//
// With LAN auto-discovery, the tablet finds the host and connects on its own — no typing.
// The IP field + Connect button are OPTIONAL: assign them only if you want a manual fallback.
// If discovery can't find a host within _manualFallbackSeconds, the manual entry is revealed
// so you can still connect by hand.
//
// Scene setup (all children optional except this script):
//   Canvas
//     └─ ConnectionScreen (this script)
//           ├─ StatusLabel      (TMP_Text)        — recommended: shows Searching/Connecting/Connected
//           ├─ ManualEntryGroup (GameObject)      — optional: container for the field + button
//           │     ├─ IPInputField (TMP_InputField)
//           │     └─ ConnectButton (Button)
public class ConnectionScreenUI : MonoBehaviour
{
    [Header("Status (recommended)")]
    [SerializeField] private TMP_Text _statusLabel;

    [Header("Manual entry (optional fallback)")]
    [Tooltip("Optional container holding the IP field + Connect button. Hidden until discovery times out.")]
    [SerializeField] private GameObject     _manualEntryGroup;
    [SerializeField] private TMP_InputField _ipInputField;
    [SerializeField] private Button         _connectButton;

    [Tooltip("If no host is found within this many seconds, reveal the manual entry. 0 = always show it.")]
    [SerializeField] private float _manualFallbackSeconds = 10f;

    [Header("After connect")]
    [Tooltip("The root GameObject of the tablet UI to show after connecting.")]
    [SerializeField] private GameObject _tabletUIRoot;

    private const string LastIPKey = "LastHostIP";

    private LanDiscovery _discovery;
    private bool  _connecting;
    private bool  _manualShown;
    private float _searchStartTime;

    // Animated "…" status: when _animatedBase is set, Update appends cycling dots.
    private string _animatedBase;
    private float  _dotTimer;
    private int    _dotCount;

    private void SetStatus(string text, bool animated)
    {
        _animatedBase = animated ? text : null;
        if (_statusLabel != null)
            _statusLabel.text = text;
    }

    private void Start()
    {
        // Restore last used IP (if a field exists)
        if (_ipInputField != null)
            _ipInputField.text = PlayerPrefs.GetString(LastIPKey, "192.168.1.");

        if (_connectButton != null)
            _connectButton.onClick.AddListener(OnConnect);

        // Hide tablet UI until connected
        if (_tabletUIRoot != null)
            _tabletUIRoot.SetActive(false);

        // Manual entry starts hidden; revealed only on timeout (or immediately if _manualFallbackSeconds <= 0).
        SetManualEntryVisible(_manualFallbackSeconds <= 0f);

        // Start LAN auto-discovery: listen for the host broadcasting itself, then auto-connect.
        _discovery = FindFirstObjectByType<LanDiscovery>();
        if (_discovery == null)
        {
            var bootstrap = FindFirstObjectByType<NetworkBootstrap>();
            var owner = bootstrap != null ? bootstrap.gameObject : gameObject;
            _discovery = owner.AddComponent<LanDiscovery>();
        }

        _discovery.HostFound += OnHostDiscovered;
        _discovery.StartListening();
        _searchStartTime = Time.time;

        SetStatus("Searching for host on the network", true);
    }

    private void Update()
    {
        // Animate trailing dots on the status label while searching/connecting.
        if (_animatedBase != null && _statusLabel != null)
        {
            _dotTimer += Time.deltaTime;
            if (_dotTimer >= 0.4f)
            {
                _dotTimer = 0f;
                _dotCount = (_dotCount + 1) % 4;          // 0,1,2,3 dots
                _statusLabel.text = _animatedBase + new string('.', _dotCount);
            }
        }

        // Reveal manual entry if discovery hasn't found a host in time.
        if (!_manualShown && !_connecting && _manualFallbackSeconds > 0f &&
            Time.time - _searchStartTime > _manualFallbackSeconds)
        {
            SetManualEntryVisible(true);
            SetStatus("No host found automatically. Enter the host IP and tap Connect.", false);
        }
    }

    private void OnDestroy()
    {
        if (_discovery != null)
            _discovery.HostFound -= OnHostDiscovered;
    }

    private void SetManualEntryVisible(bool visible)
    {
        _manualShown = visible;

        if (_manualEntryGroup != null)
        {
            _manualEntryGroup.SetActive(visible);
        }
        else
        {
            // No group assigned — toggle the field + button individually if they exist.
            if (_ipInputField != null) _ipInputField.gameObject.SetActive(visible);
            if (_connectButton != null) _connectButton.gameObject.SetActive(visible);
        }
    }

    // Auto-discovery found the host — connect.
    private void OnHostDiscovered(string ip)
    {
        if (_connecting) return;
        if (_ipInputField != null) _ipInputField.text = ip;
        SetStatus($"Host found at {ip}. Connecting", true);
        Connect(ip);
    }

    // Manual Connect button.
    private void OnConnect()
    {
        string ip = _ipInputField != null ? _ipInputField.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(ip))
        {
            SetStatus("Please enter an IP address.", false);
            return;
        }
        Connect(ip);
    }

    // Shared connect path used by both auto-discovery and the manual button.
    private void Connect(string ip)
    {
        if (_connecting) return;
        _connecting = true;

        // Stop listening once we commit to a host (frees the discovery socket).
        if (_discovery != null) _discovery.Stop();

        PlayerPrefs.SetString(LastIPKey, ip);
        PlayerPrefs.Save();

        if (_connectButton != null) _connectButton.interactable = false;
        SetStatus($"Connecting to {ip}", true);

        var bootstrap = FindFirstObjectByType<NetworkBootstrap>();
        if (bootstrap == null)
        {
            SetStatus("Error: NetworkBootstrap not found.", false);
            _connecting = false;
            return;
        }

        bootstrap.ConnectAsClient(ip);

        // Listen for connection result
        Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback  += OnConnected;
        Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback += OnFailed;
    }

    private void OnConnected(ulong clientId)
    {
        Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback  -= OnConnected;
        Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback -= OnFailed;

        SetStatus("Connected!", false);

        // Hide connection screen, show tablet UI
        gameObject.SetActive(false);
        if (_tabletUIRoot != null)
            _tabletUIRoot.SetActive(true);
    }

    private void OnFailed(ulong clientId)
    {
        Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback  -= OnConnected;
        Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback -= OnFailed;

        _connecting = false;
        if (_connectButton != null) _connectButton.interactable = true;
        SetStatus("Connection failed. Retrying discovery", true);

        // Resume auto-discovery so it can find the host again without a manual retry.
        if (_discovery != null)
            _discovery.StartListening();

        _searchStartTime = Time.time;   // restart the manual-fallback timer
    }
}
