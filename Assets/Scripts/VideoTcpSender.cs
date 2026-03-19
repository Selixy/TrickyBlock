using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Net;
using System;
using System.Collections;
using System.Threading;

public class CrossVideoNetworkManager : MonoBehaviour
{
    public enum PlayerRole { Player1, Player2 }

    [Header("Identité du Client")]
    public PlayerRole role = PlayerRole.Player1;
    public RawImage displayOtherPlayer;

    [Header("Network Settings")]
    public string rustServerIP = "127.0.0.1";
    public float reconnectionDelay = 3f;

    [Header("Capture Settings (Ma Caméra)")]
    public int width = 640;
    public int height = 480;
    public int fps = 30;
    [Range(10, 100)] public int jpegQuality = 50;

    // Déductions Automatiques des Ports
    private int MySendPort => (role == PlayerRole.Player1) ? 8080 : 8082;
    private int MyReceivePort => (role == PlayerRole.Player1) ? 8081 : 8083;
    private int OscListenPort => (role == PlayerRole.Player1) ? 9005 : 9006;

    // Séparation claire entre l'envoi de ma cam et la réception de celle de l'autre
    private TcpClient sendClient;
    private NetworkStream sendStream;

    private TcpClient receiveClient;
    private NetworkStream receiveStream;

    private float lastSendConnectionAttempt = 0f;
    private WebCamTexture myWebcam;

    private UdpClient udpListener;
    private Thread udpThread;
    private Thread tcpReceiveThread;

    // Etat partagé
    private bool isRunning = true;
    private byte[] latestReceivedJpeg = null;
    private bool hasNewJpeg = false;
    private Texture2D currentDisplayTexture = null;

    // Valeur de crop de l'ADVERSAIRE reçue de Rust (0.0 à 1.0)
    [SerializeField] private float otherPlayerCropX = 0.5f;
    
    [Header("Crop Settings")]
    public float cropHeightRatio = 1.0f; // 100% de la hauteur
    public float cropWidthRatio = 0.625f; // 10:16 standard (62.5% de la largeur)

    void Start()
    {
        isRunning = true;

        // 1. Initialiser et envoyer MA vidéo
        InitWebcam();
        ConnectSendSocket();
        StartCoroutine(SendVideoRoutine());

        // 2. Écouter la vidéo de L'AUTRE
        ConnectReceiveSocket();
        tcpReceiveThread = new Thread(ReceiveVideoThread);
        tcpReceiveThread.Start();

        // 3. Écouter mon OSC (le recadrage que je dois appliquer sur l'autre)
        StartUdpListener();
    }

    void Update()
    {
        // --- AFFICHAGE DE LA CAMÉRA ADVERSE ---
        if (hasNewJpeg && latestReceivedJpeg != null)
        {
            hasNewJpeg = false;
            UpdateReceivedTexture();
        }

        // --- RECADRAGE DE LA CAMÉRA ADVERSE ---
        ApplyCropRect();
    }

    void UpdateReceivedTexture()
    {
        // Créer une nouvelle texture avec les bonnes dimensions
        Texture2D newTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        
        if (newTexture.LoadImage(latestReceivedJpeg))
        {
            // Remplacer l'ancienne texture
            if (currentDisplayTexture != null)
            {
                Destroy(currentDisplayTexture);
            }
            
            currentDisplayTexture = newTexture;
            
            // Assigner à la RawImage
            if (displayOtherPlayer != null)
            {
                displayOtherPlayer.texture = currentDisplayTexture;
            }
            
            Debug.Log($"Texture loaded: {newTexture.width}x{newTexture.height}");
        }
        else
        {
            Destroy(newTexture);
        }
    }

    void ApplyCropRect()
    {
        if (displayOtherPlayer == null || displayOtherPlayer.texture == null)
            return;

        // Calculer la position de crop en centrant sur otherPlayerCropX
        // otherPlayerCropX = 0.0 à 1.0 (position du joueur dans l'image)
        // On veut afficher cropWidthRatio (ex: 62.5%) centré sur cette position
        
        float startX = Mathf.Clamp(
            otherPlayerCropX - (cropWidthRatio / 2f), 
            0f, 
            1f - cropWidthRatio
        );
        
        // Appliquer le rect de crop (x, y, width, height)
        // La hauteur reste toujours 1.0f (full height)
        displayOtherPlayer.uvRect = new Rect(startX, 0f, cropWidthRatio, cropHeightRatio);
    }

    // ==========================================================
    // MODULE: SEND MY WEBCAM
    // ==========================================================
    void InitWebcam()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            myWebcam = new WebCamTexture(devices[0].name, width, height, fps);
            myWebcam.Play();
        }
    }

    void ConnectSendSocket()
    {
        try
        {
            sendClient = new TcpClient(rustServerIP, MySendPort);
            sendStream = sendClient.GetStream();
            lastSendConnectionAttempt = Time.time;
        }
        catch { lastSendConnectionAttempt = Time.time; }
    }

    IEnumerator SendVideoRoutine()
    {
        Texture2D tex = new Texture2D(myWebcam.width, myWebcam.height, TextureFormat.RGB24, false);
        while (isRunning)
        {
            yield return new WaitForEndOfFrame();

            if (sendClient == null || !sendClient.Connected)
            {
                if (Time.time - lastSendConnectionAttempt >= reconnectionDelay)
                    ConnectSendSocket();
                continue;
            }

            if (myWebcam.didUpdateThisFrame)
            {
                tex.SetPixels(myWebcam.GetPixels());
                tex.Apply();
                byte[] jpegBytes = tex.EncodeToJPG(jpegQuality);

                int size = jpegBytes.Length;
                byte[] sizeBytes = BitConverter.GetBytes(size);
                if (BitConverter.IsLittleEndian) Array.Reverse(sizeBytes);

                try
                {
                    sendStream.Write(sizeBytes, 0, sizeBytes.Length);
                    sendStream.Write(jpegBytes, 0, jpegBytes.Length);
                }
                catch
                {
                    if (sendStream != null) sendStream.Close();
                    if (sendClient != null) sendClient.Close();
                    sendClient = null;
                }
            }
        }
    }

    // ==========================================================
    // MODULE: RECEIVE OTHER PLAYER WEBCAM
    // ==========================================================
    void ConnectReceiveSocket()
    {
        try
        {
            receiveClient = new TcpClient(rustServerIP, MyReceivePort);
            receiveStream = receiveClient.GetStream();
        }
        catch { }
    }

    void ReceiveVideoThread()
    {
        while (isRunning)
        {
            if (receiveClient == null || !receiveClient.Connected)
            {
                Thread.Sleep(1000);
                ConnectReceiveSocket();
                continue;
            }

            try
            {
                byte[] sizeBuffer = new byte[4];
                int bytesRead = 0;
                while (bytesRead < 4)
                {
                    int read = receiveStream.Read(sizeBuffer, bytesRead, 4 - bytesRead);
                    if (read == 0) throw new Exception("Disconnected");
                    bytesRead += read;
                }

                if (BitConverter.IsLittleEndian) Array.Reverse(sizeBuffer);
                int imageSize = BitConverter.ToInt32(sizeBuffer, 0);

                byte[] imageBytes = new byte[imageSize];
                bytesRead = 0;
                while (bytesRead < imageSize)
                {
                    int read = receiveStream.Read(imageBytes, bytesRead, imageSize - bytesRead);
                    if (read == 0) throw new Exception("Disconnected");
                    bytesRead += read;
                }

                latestReceivedJpeg = imageBytes;
                hasNewJpeg = true;
            }
            catch
            {
                if (receiveStream != null) receiveStream.Close();
                if (receiveClient != null) receiveClient.Close();
                receiveClient = null;
            }
        }
    }

    // ==========================================================
    // MODULE: RECEIVE CROP OSC
    // ==========================================================
    void StartUdpListener()
    {
        udpThread = new Thread(() =>
        {
            try
            {
                udpListener = new UdpClient(OscListenPort);
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

                while (isRunning)
                {
                    byte[] data = udpListener.Receive(ref anyIP);

                    if (data.Length >= 4)
                    {
                        byte[] floatBytes = new byte[4];
                        Array.Copy(data, data.Length - 4, floatBytes, 0, 4);
                        if (BitConverter.IsLittleEndian) Array.Reverse(floatBytes);

                        float val = BitConverter.ToSingle(floatBytes, 0);
                        if (val >= 0f && val <= 1f) otherPlayerCropX = val;
                    }
                }
            }
            catch { }
        });
        udpThread.IsBackground = true;
        udpThread.Start();
    }

    void OnDestroy()
    {
        isRunning = false;
        if (sendStream != null) sendStream.Close();
        if (sendClient != null) sendClient.Close();

        if (receiveStream != null) receiveStream.Close();
        if (receiveClient != null) receiveClient.Close();

        if (myWebcam != null) myWebcam.Stop();
        if (udpListener != null) udpListener.Close();
        
        // Libérer la texture affichée
        if (currentDisplayTexture != null) Destroy(currentDisplayTexture);
    }
}