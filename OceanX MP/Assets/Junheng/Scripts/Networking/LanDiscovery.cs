using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// Lightweight LAN auto-discovery over UDP broadcast — no manual IP typing.
//
//   HOST   : broadcasts "OCEANX_HOST:<gamePort>" every second on the discovery port.
//   CLIENT : listens on the discovery port; when it hears a host, raises HostFound(ip)
//            on the MAIN thread exactly once.
//
// The discovery port (default 47777) is separate from the game port (7777) and is only
// used to find the host's IP. Once found, the client connects to <ip>:<gamePort> as normal.
//
// Power on both devices on the same Wi-Fi → the tablet finds the host by itself.
public class LanDiscovery : MonoBehaviour
{
    [Tooltip("UDP port used ONLY for discovery broadcasts. Must match on host and client. Different from the game port.")]
    [SerializeField] private int _discoveryPort = 47777;

    [Tooltip("Identifier so we only react to OUR app's broadcasts, not other UDP traffic.")]
    [SerializeField] private string _magic = "OCEANX_HOST";

    [Tooltip("How often the host re-broadcasts its presence, in seconds.")]
    [SerializeField] private float _broadcastIntervalSeconds = 1f;

    // Raised on the MAIN thread, once, with the host's IP string.
    public event Action<string> HostFound;

    private Thread     _thread;
    private UdpClient  _socket;        // referenced so Stop() can unblock a blocking Receive()
    private volatile bool   _running;
    private volatile string _foundIp;  // set by the listen thread, consumed in Update()
    private bool _fired;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject _multicastLock;
#endif

    // ---------------- HOST ----------------

    public void StartAdvertising(ushort gamePort)
    {
        Stop();
        _running = true;
        _thread = new Thread(() => AdvertiseLoop(gamePort)) { IsBackground = true };
        _thread.Start();
        Debug.Log($"[LanDiscovery] Advertising host on discovery port {_discoveryPort} (game port {gamePort}).");
    }

    private void AdvertiseLoop(ushort gamePort)
    {
        try
        {
            _socket = new UdpClient { EnableBroadcast = true };
            var ep = new IPEndPoint(IPAddress.Broadcast, _discoveryPort);
            byte[] msg = Encoding.UTF8.GetBytes($"{_magic}:{gamePort}");

            int interval = Mathf.Max(100, (int)(_broadcastIntervalSeconds * 1000));
            while (_running)
            {
                try { _socket.Send(msg, msg.Length, ep); }
                catch (Exception e) { Debug.LogWarning($"[LanDiscovery] broadcast send failed: {e.Message}"); }
                Thread.Sleep(interval);
            }
        }
        catch (Exception e) { Debug.LogError($"[LanDiscovery] advertise error: {e}"); }
        finally { CloseSocket(); }
    }

    // ---------------- CLIENT ----------------

    public void StartListening()
    {
        Stop();
        _running = true;
        _fired   = false;
        _foundIp = null;
        _thread = new Thread(ListenLoop) { IsBackground = true };
        _thread.Start();
        Debug.Log($"[LanDiscovery] Listening for a host on discovery port {_discoveryPort}.");
    }

    private void ListenLoop()
    {
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AcquireMulticastLock();   // some Android Wi-Fi drivers drop broadcasts without this
#endif
            _socket = new UdpClient { EnableBroadcast = true };
            _socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.Client.Bind(new IPEndPoint(IPAddress.Any, _discoveryPort));

            var from = new IPEndPoint(IPAddress.Any, 0);
            while (_running)
            {
                byte[] data = _socket.Receive(ref from);   // blocks; throws when Stop() closes the socket
                string text = Encoding.UTF8.GetString(data);
                if (text.StartsWith(_magic, StringComparison.Ordinal))
                {
                    _foundIp = from.Address.ToString();
                    _running = false;   // found one — stop listening
                }
            }
        }
        catch (SocketException)        { /* socket closed by Stop() — expected */ }
        catch (ObjectDisposedException) { /* socket closed by Stop() — expected */ }
        catch (Exception e) { Debug.LogError($"[LanDiscovery] listen error: {e}"); }
        finally
        {
            CloseSocket();
#if UNITY_ANDROID && !UNITY_EDITOR
            ReleaseMulticastLock();
#endif
        }
    }

    // ---------------- MAIN THREAD ----------------

    private void Update()
    {
        if (!_fired && _foundIp != null)
        {
            _fired = true;
            Debug.Log($"[LanDiscovery] Host found at {_foundIp}.");
            HostFound?.Invoke(_foundIp);
        }
    }

    public void Stop()
    {
        _running = false;
        CloseSocket();                 // unblocks a thread stuck in Receive()
        if (_thread != null && _thread.IsAlive)
            _thread.Join(200);
        _thread = null;
    }

    private void CloseSocket()
    {
        try { _socket?.Close(); } catch { }
        _socket = null;
    }

    private void OnDestroy()       => Stop();
    private void OnApplicationQuit() => Stop();

#if UNITY_ANDROID && !UNITY_EDITOR
    private void AcquireMulticastLock()
    {
        try
        {
            using var player   = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var wifi     = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
            _multicastLock = wifi.Call<AndroidJavaObject>("createMulticastLock", "OceanXDiscovery");
            _multicastLock.Call("setReferenceCounted", false);
            _multicastLock.Call("acquire");
        }
        catch (Exception e) { Debug.LogWarning($"[LanDiscovery] multicast lock failed: {e.Message}"); }
    }

    private void ReleaseMulticastLock()
    {
        try { _multicastLock?.Call("release"); } catch { }
        _multicastLock = null;
    }
#endif
}
