using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class OscReceiver : MonoBehaviour
{
    [SerializeField] private int port = 9000;
    [Tooltip("Laisser vide pour écouter uniquement en local (127.0.0.1). Renseigner une IP pour un PC distant.")]
    [SerializeField] private string targetIp = "";

    // Événements OSC — PlayerControl s'y abonne
    public event System.Action OnMoveLeft;
    public event System.Action OnMoveRight;
    public event System.Action OnRotateLeft;
    public event System.Action OnRotateRight;
    public event System.Action OnDashLeft;
    public event System.Action OnDashRight;
    public event System.Action OnFastFallStart;
    public event System.Action OnFastFallStop;
    public event System.Action OnDrop;

    private UdpClient udpClient;
    private Thread receiveThread;
    private volatile bool running = false;

    private volatile string lastAddress = "";
    private volatile float lastValue = 0f;
    private volatile bool hasNewData = false;
    private volatile string lastSenderIp = "";
    private volatile int lastSenderPort = 0;

    private IPAddress filterAddress;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(targetIp))
        {
            filterAddress = IPAddress.Loopback;
            Debug.Log($"[OSC] Mode local (127.0.0.1) — port {port}");
        }
        else
        {
            if (IPAddress.TryParse(targetIp, out IPAddress parsed))
            {
                filterAddress = parsed;
                Debug.Log($"[OSC] Mode distant — filtrage sur {targetIp}:{port}");
            }
            else
            {
                Debug.LogError($"[OSC] IP invalide : '{targetIp}' — fallback sur localhost");
                filterAddress = IPAddress.Loopback;
            }
        }

        udpClient = new UdpClient(port);
        running = true;
        receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiveThread.Start();
    }

    private void OnDestroy()
    {
        running = false;
        udpClient?.Close();
        receiveThread?.Abort();
    }

    // ─── Réseau ───────────────────────────────────────────────────────────────

    private void ReceiveLoop()
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref endPoint);

                if (!endPoint.Address.Equals(filterAddress))
                    continue;

                lastSenderIp = endPoint.Address.ToString();
                lastSenderPort = endPoint.Port;
                ParseOsc(data);
            }
            catch { }
        }
    }

    // ─── Parsing OSC ──────────────────────────────────────────────────────────

    private void ParseOsc(byte[] data)
    {
        int i = 0;
        while (i < data.Length && data[i] != 0) i++;
        string address = System.Text.Encoding.ASCII.GetString(data, 0, i);

        int offset = ((i / 4) + 1) * 4;
        offset += 4;

        if (offset + 4 <= data.Length)
        {
            byte[] floatBytes = new byte[4]
            {
                data[offset + 3],
                data[offset + 2],
                data[offset + 1],
                data[offset + 0]
            };
            float value = System.BitConverter.ToSingle(floatBytes, 0);

            lastAddress = address;
            lastValue = value;
            hasNewData = true;
        }
    }

    // ─── Dispatch sur le thread Unity ─────────────────────────────────────────

    private void Update()
    {
        if (!hasNewData) return;
        hasNewData = false;

        Debug.Log($"[OSC] {lastSenderIp}:{lastSenderPort} — '{lastAddress}' = {lastValue}");

        switch (lastAddress)
        {
            case "/p1/move_left": OnMoveLeft?.Invoke(); break;
            case "/p1/move_right": OnMoveRight?.Invoke(); break;
            case "/p1/rotate_left": OnRotateLeft?.Invoke(); break;
            case "/p1/rotate_right": OnRotateRight?.Invoke(); break;
            case "/p1/dash_left": OnDashLeft?.Invoke(); break;
            case "/p1/dash_right": OnDashRight?.Invoke(); break;
            case "/p1/fast_fall":
                if (lastValue >= 1f) OnFastFallStart?.Invoke();
                else OnFastFallStop?.Invoke();
                break;
            case "/p1/drop": OnDrop?.Invoke(); break;
        }
    }
}