using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class OscSender : MonoBehaviour
{
    [SerializeField] private int port = 9000;

    private UdpClient udpClient;
    private OscReceiver oscReceiver;

    void Awake()
    {
        try
        {
            oscReceiver = GetComponent<OscReceiver>();
            InitializeUdpClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize OscSender: {e.Message}");
        }
    }

    void InitializeUdpClient()
    {
        if (oscReceiver == null) oscReceiver = GetComponent<OscReceiver>();

        // Utiliser l'IP configurée dans OscReceiver (mise à jour par ServerIPInput)
        string targetIP = oscReceiver?.GetServerIP() ?? "127.0.0.1";
        if (string.IsNullOrEmpty(targetIP)) targetIP = "127.0.0.1";

        udpClient = new UdpClient(targetIP, port);
        Debug.Log($"[OscSender] Initialized on {targetIP}:{port} (IP from OscReceiver)");
    }

    public void Send(string address, string data)
    {
        if (udpClient == null)
        {
            Debug.LogError("OscSender not initialized");
            return;
        }

        try
        {
            byte[] addressBytes = Encoding.ASCII.GetBytes(address);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            // Build OSC packet: address (padded) + type tag (padded) + data (padded)
            int addressLen = ((addressBytes.Length / 4) + 1) * 4;
            int dataLen = ((dataBytes.Length / 4) + 1) * 4;

            byte[] packet = new byte[addressLen + 4 + dataLen]; // address + ",s\0\0" + data

            // Write address with padding
            System.Array.Copy(addressBytes, 0, packet, 0, addressBytes.Length);

            // Write type tag ",s" with padding
            packet[addressLen] = (byte)',';
            packet[addressLen + 1] = (byte)'s';
            packet[addressLen + 2] = 0;
            packet[addressLen + 3] = 0;

            // Write data with padding
            System.Array.Copy(dataBytes, 0, packet, addressLen + 4, dataBytes.Length);

            udpClient.Send(packet, packet.Length);
            string targetIP = oscReceiver?.GetServerIP() ?? "127.0.0.1";
            Debug.Log($"[OscSender] sent to {targetIP}: {address} -> {data.Substring(0, Mathf.Min(50, data.Length))}...");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to send OSC message: {e.Message}");
        }
    }

    void OnDestroy()
    {
        udpClient?.Close();
        udpClient?.Dispose();
    }
}
