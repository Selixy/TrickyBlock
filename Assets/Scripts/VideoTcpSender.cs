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

    // === SINGLETON POUR ACCÈS GLOBAL ===
    public static CrossVideoNetworkManager Instance { get; private set; }

    [Header("Identité du Client")]
    public PlayerRole role = PlayerRole.Player1;

    [Header("Network Settings")]
    public string rustServerIP = "127.0.0.1";
    public float reconnectionDelay = 3f;
    public int connectionTimeoutMS = 2000; // 2 secondes de timeout

    [Header("Capture Settings (Ma Caméra)")]
    public int width = 640;
    public int height = 480;
    public int fps = 30;
    [Range(10, 100)] public int jpegQuality = 50;

    // === ÉVÉNEMENTS POUR LES SCÈNES ADDITIVES ===
    public delegate void OnVideoFrameReceived(byte[] jpegData, float cropX);
    public static event OnVideoFrameReceived OnPlayer1VideoReceived;
    public static event OnVideoFrameReceived OnPlayer2VideoReceived;
    public static event OnVideoFrameReceived OnMyVideoCapture; // Pour déboguer sa propre caméra

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
    private Thread tcpSendConnectionThread;
    private Thread tcpReceiveConnectionThread;

    private DateTime nextSendConnectionAttempt = DateTime.Now;
    private DateTime nextReceiveConnectionAttempt = DateTime.Now;

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

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        isRunning = true;

        // 1. Initialiser la webcam
        InitWebcam();

        // 2. Démarrer les threads de connexion (non-bloquants)
        tcpSendConnectionThread = new Thread(SendConnectionThread);
        tcpSendConnectionThread.IsBackground = true;
        tcpSendConnectionThread.Start();

        tcpReceiveConnectionThread = new Thread(ReceiveConnectionThread);
        tcpReceiveConnectionThread.IsBackground = true;
        tcpReceiveConnectionThread.Start();

        // 3. Démarrer la coroutine d'envoi (pour la webcam)
        StartCoroutine(SendVideoRoutine());

        // 4. Démarrer le thread de réception
        tcpReceiveThread = new Thread(ReceiveVideoThread);
        tcpReceiveThread.IsBackground = true;
        tcpReceiveThread.Start();

        // 5. Écouter mon OSC (le recadrage que je dois appliquer sur l'autre)
        StartUdpListener();
    }

    void Update()
    {
        // --- ENVOI DES DONNÉES AUX SCÈNES ADDITIVES VIA ÉVÉNEMENTS ---
        if (hasNewJpeg && latestReceivedJpeg != null)
        {
            hasNewJpeg = false;

            Debug.Log($"[CrossVideoNetworkManager] Dispatching video for role: {role}, cropX: {otherPlayerCropX}");

            // Dispatcher l'événement selon le rôle du joueur actuel
            if (role == PlayerRole.Player1)
            {
                Debug.Log("Invoking OnPlayer2VideoReceived");
                OnPlayer2VideoReceived?.Invoke(latestReceivedJpeg, otherPlayerCropX);
            }
            else
            {
                Debug.Log("Invoking OnPlayer1VideoReceived");
                OnPlayer1VideoReceived?.Invoke(latestReceivedJpeg, otherPlayerCropX);
            }
        }
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

            Debug.Log($"Texture loaded: {newTexture.width}x{newTexture.height}");
        }
        else
        {
            Destroy(newTexture);
        }
    }

    void ApplyCropRect()
    {
        // Cette méthode n'est plus nécessaire ici
        // Le crop est géré par les classes UI dans les scènes additives
    }

    // ==========================================================
    // MODULE: SEND MY WEBCAM
    // ==========================================================
    void SendConnectionThread()
    {
        while (isRunning)
        {
            if (sendClient == null || !sendClient.Connected)
            {
                if (DateTime.Now >= nextSendConnectionAttempt)
                {
                    ConnectSendSocket();
                    nextSendConnectionAttempt = DateTime.Now.AddSeconds(reconnectionDelay);
                }
            }
            Thread.Sleep(100); // Petit délai pour ne pas bloquer
        }
    }

    void ReceiveConnectionThread()
    {
        while (isRunning)
        {
            if (receiveClient == null || !receiveClient.Connected)
            {
                if (DateTime.Now >= nextReceiveConnectionAttempt)
                {
                    ConnectReceiveSocket();
                    nextReceiveConnectionAttempt = DateTime.Now.AddSeconds(reconnectionDelay);
                }
            }
            Thread.Sleep(100); // Petit délai pour ne pas bloquer
        }
    }

    void InitWebcam()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            myWebcam = new WebCamTexture(devices[0].name, width, height, fps);
            myWebcam.Play();
            Debug.Log($"[CrossVideoNetworkManager] Webcam initialized: {devices[0].name} ({width}x{height}@{fps}fps)");
        }
        else
        {
            Debug.LogError("[CrossVideoNetworkManager] No webcam device found!");
        }
    }

    void ConnectSendSocket()
    {
        try
        {
            sendClient = new TcpClient();
            sendClient.ReceiveTimeout = connectionTimeoutMS;
            sendClient.SendTimeout = connectionTimeoutMS;

            // Connexion asynchrone avec timeout
            IAsyncResult result = sendClient.BeginConnect(rustServerIP, MySendPort, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(connectionTimeoutMS, true);

            if (success && sendClient.Connected)
            {
                sendStream = sendClient.GetStream();
                Debug.Log($"[CrossVideoNetworkManager] Connected to send socket {MySendPort}");
            }
            else
            {
                sendClient.Close();
                sendClient = null;
            }

            lastSendConnectionAttempt = (float)DateTime.Now.TimeOfDay.TotalSeconds;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CrossVideoNetworkManager] Send connection failed: {e.Message}");
            lastSendConnectionAttempt = (float)DateTime.Now.TimeOfDay.TotalSeconds;
        }
    }

    IEnumerator SendVideoRoutine()
    {
        // Attendre que la webcam soit prête
        int waitCount = 0;
        while (myWebcam == null || !myWebcam.isPlaying)
        {
            waitCount++;
            if (waitCount > 50)
            {
                Debug.LogError("[CrossVideoNetworkManager] Webcam failed to start!");
                yield break;
            }
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("[CrossVideoNetworkManager] Webcam ready, starting capture");

        // Attendre quelques frames supplémentaires que la webcam remplisse son buffer
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForEndOfFrame();
        }

        Texture2D tex = new Texture2D(myWebcam.width, myWebcam.height, TextureFormat.RGB24, false);
        int frameCount = 0;

        while (isRunning)
        {
            yield return new WaitForEndOfFrame();

            if (myWebcam == null || !myWebcam.isPlaying)
            {
                Debug.LogWarning("[CrossVideoNetworkManager] Webcam stopped!");
                yield break;
            }

            // Essayer de capturer la webcam
            try
            {
                Color[] pixels = myWebcam.GetPixels();

                if (pixels == null || pixels.Length == 0)
                {
                    Debug.LogWarning("[CrossVideoNetworkManager] No pixels from webcam yet");
                    continue;
                }

                tex.SetPixels(pixels);
                tex.Apply();
                byte[] jpegBytes = tex.EncodeToJPG(jpegQuality);

                if (jpegBytes.Length > 0)
                {
                    frameCount++;

                    // Envoyer au serveur si connecté
                    if (sendClient != null && sendClient.Connected)
                    {
                        try
                        {
                            int size = jpegBytes.Length;
                            byte[] sizeBytes = BitConverter.GetBytes(size);
                            if (BitConverter.IsLittleEndian) Array.Reverse(sizeBytes);

                            sendStream.Write(sizeBytes, 0, sizeBytes.Length);
                            sendStream.Write(jpegBytes, 0, jpegBytes.Length);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[CrossVideoNetworkManager] Send error: {e.Message}");
                            if (sendStream != null) sendStream.Close();
                            if (sendClient != null) sendClient.Close();
                            sendClient = null;
                        }
                    }

                    // TOUJOURS dispatcher l'événement local (pour l'affichage debug)
                    OnMyVideoCapture?.Invoke(jpegBytes, 0.5f);

                    if (frameCount % 30 == 0)
                    {
                        Debug.Log($"[CrossVideoNetworkManager] Captured {frameCount} frames, JPEG size: {jpegBytes.Length} bytes");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CrossVideoNetworkManager] Capture error: {e.Message}");
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
            receiveClient = new TcpClient();
            receiveClient.ReceiveTimeout = connectionTimeoutMS;
            receiveClient.SendTimeout = connectionTimeoutMS;

            // Connexion asynchrone avec timeout
            IAsyncResult result = receiveClient.BeginConnect(rustServerIP, MyReceivePort, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(connectionTimeoutMS, true);

            if (success && receiveClient.Connected)
            {
                receiveStream = receiveClient.GetStream();
                Debug.Log($"[CrossVideoNetworkManager] Connected to receive socket {MyReceivePort}");
            }
            else
            {
                receiveClient.Close();
                receiveClient = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CrossVideoNetworkManager] Receive connection failed: {e.Message}");
        }
    }

    void ReceiveVideoThread()
    {
        while (isRunning)
        {
            try
            {
                if (receiveClient == null || !receiveClient.Connected)
                {
                    Thread.Sleep(100); // Short sleep to avoid spinning
                    continue;
                }

                byte[] sizeBuffer = new byte[4];
                int bytesRead = 0;
                while (bytesRead < 4 && isRunning)
                {
                    int read = receiveStream.Read(sizeBuffer, bytesRead, 4 - bytesRead);
                    if (read == 0) throw new Exception("Disconnected");
                    bytesRead += read;
                }

                if (!isRunning) break;

                if (BitConverter.IsLittleEndian) Array.Reverse(sizeBuffer);
                int imageSize = BitConverter.ToInt32(sizeBuffer, 0);

                byte[] imageBytes = new byte[imageSize];
                bytesRead = 0;
                while (bytesRead < imageSize && isRunning)
                {
                    int read = receiveStream.Read(imageBytes, bytesRead, imageSize - bytesRead);
                    if (read == 0) throw new Exception("Disconnected");
                    bytesRead += read;
                }

                if (!isRunning) break;

                latestReceivedJpeg = imageBytes;
                hasNewJpeg = true;
            }
            catch (Exception e)
            {
                if (isRunning)
                {
                    Debug.LogWarning($"[CrossVideoNetworkManager] ReceiveVideoThread error: {e.Message}");
                }

                try
                {
                    if (receiveStream != null) receiveStream.Close();
                    if (receiveClient != null) receiveClient.Close();
                }
                catch { }

                receiveClient = null;

                if (isRunning)
                {
                    Thread.Sleep(1000);
                }
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