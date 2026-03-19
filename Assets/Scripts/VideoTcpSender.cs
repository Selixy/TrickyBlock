using UnityEngine;
using System.Net.Sockets;
using System;
using System.Collections;

public class VideoTcpSender : MonoBehaviour
{
    [Header("Network Settings")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 8080;

    [Header("Capture Settings")]
    public int width = 640;
    public int height = 480;
    public int fps = 30;
    [Range(10, 100)]
    public int jpegQuality = 50; // Qualité JPEG (réduire pour plus de perf)

    private WebCamTexture webcamTexture;
    private TcpClient tcpClient;
    private NetworkStream networkStream;

    void Start()
    {
        // 1. Initialiser la capture vidéo locale (Webcam)
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("No camera detected");
            return;
        }

        // On prend la première caméra disponible
        webcamTexture = new WebCamTexture(devices[0].name, width, height, fps);
        webcamTexture.Play();

        // 2. Se connecter au serveur Rust
        ConnectToServer();

        // 3. Démarrer la coroutine d'envoi
        StartCoroutine(SendVideoFrames());
    }

    void ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient(serverIP, serverPort);
            networkStream = tcpClient.GetStream();
            Debug.Log("Connected to Rust tracking server!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection failed: {e.Message}");
        }
    }

    IEnumerator SendVideoFrames()
    {
        // Texture2D intermédiaire pour lire les pixels de la WebCamTexture
        Texture2D tex = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);

        while (true)
        {
            // On attend la fin de la frame pour lire les pixels de manière safe
            yield return new WaitForEndOfFrame();

            if (webcamTexture.didUpdateThisFrame && tcpClient != null && tcpClient.Connected)
            {
                // Copier les pixels de la webcam vers la Texture2D
                tex.SetPixels(webcamTexture.GetPixels());
                tex.Apply();

                // Encoder en JPEG
                byte[] jpegBytes = tex.EncodeToJPG(jpegQuality);

                // Préparer les 4 octets de taille en Big Endian
                int size = jpegBytes.Length;
                byte[] sizeBytes = BitConverter.GetBytes(size);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(sizeBytes); // Conversion en Big Endian
                }

                try
                {
                    // Envoyer la taille puis l'image
                    networkStream.Write(sizeBytes, 0, sizeBytes.Length);
                    networkStream.Write(jpegBytes, 0, jpegBytes.Length);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Lost connection to server: {e.Message}. Reconnecting...");
                    ConnectToServer();
                }
            }

            // On peut ajouter un petit délai si on ne veut pas envoyer toutes les frame (ex: yield return new WaitForSeconds(0.05f))
        }
    }

    void OnDestroy()
    {
        if (webcamTexture != null) webcamTexture.Stop();
        if (networkStream != null) networkStream.Close();
        if (tcpClient != null) tcpClient.Close();
    }
}
