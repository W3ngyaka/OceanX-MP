using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tablet startup screen — lets you type the host IP before connecting.
// Remembers the last IP used via PlayerPrefs.
//
// Scene setup:
//   Canvas
//     └─ ConnectionScreen (this script)
//           ├─ IPInputField   (TMP_InputField)
//           ├─ ConnectButton  (Button)
//           └─ StatusLabel    (TMP_Text, optional)
public class ConnectionScreenUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _ipInputField;
    [SerializeField] private Button         _connectButton;
    [SerializeField] private TMP_Text       _statusLabel;

    [Tooltip("The root GameObject of the tablet UI to show after connecting.")]
    [SerializeField] private GameObject _tabletUIRoot;

    private const string LastIPKey = "LastHostIP";

    private void Start()
    {
        // Restore last used IP
        _ipInputField.text = PlayerPrefs.GetString(LastIPKey, "192.168.1.");

        _connectButton.onClick.AddListener(OnConnect);

        if (_statusLabel != null)
            _statusLabel.text = "Enter host IP and press Connect.";

        // Hide tablet UI until connected
        if (_tabletUIRoot != null)
            _tabletUIRoot.SetActive(false);
    }

    private void OnConnect()
    {
        string ip = _ipInputField.text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            if (_statusLabel != null) _statusLabel.text = "Please enter an IP address.";
            return;
        }

        PlayerPrefs.SetString(LastIPKey, ip);
        PlayerPrefs.Save();

        _connectButton.interactable = false;
        if (_statusLabel != null) _statusLabel.text = $"Connecting to {ip}...";

        var bootstrap = FindFirstObjectByType<NetworkBootstrap>();
        if (bootstrap == null)
        {
            if (_statusLabel != null) _statusLabel.text = "Error: NetworkBootstrap not found.";
            return;
        }

        bootstrap.ConnectAsClient(ip);

        // Listen for connection result
        Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback    += OnConnected;
        Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback   += OnFailed;
    }

    private void OnConnected(ulong clientId)
    {
        Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback  -= OnConnected;
        Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback -= OnFailed;

        if (_statusLabel != null) _statusLabel.text = "Connected!";

        // Hide connection screen, show tablet UI
        gameObject.SetActive(false);
        if (_tabletUIRoot != null)
            _tabletUIRoot.SetActive(true);
    }

    private void OnFailed(ulong clientId)
    {
        Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback  -= OnConnected;
        Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback -= OnFailed;

        _connectButton.interactable = true;
        if (_statusLabel != null) _statusLabel.text = "Connection failed. Check the IP and try again.";
    }
}
