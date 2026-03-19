using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneStreamCapture : MonoBehaviour
{
    [Header("Scene")]
    public string sceneToCapture = "GameScene"; // Seul paramètre à renseigner

    // Tout le reste est automatique
    private Texture2D screenTexture;
    private RenderTexture renderTexture;
    private bool isCapturing = false;
    private Camera captureCamera;
    private RawImage displayImage;
    private int captureWidth;
    private int captureHeight;

    void Start()
    {
        Debug.Log($"[SceneStreamCapture] Loading scene: {sceneToCapture}");
        StartCoroutine(LoadSceneAndCapture());
    }

    IEnumerator LoadSceneAndCapture()
    {
        // Charger la scène en mode additive
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToCapture, LoadSceneMode.Additive);

        // Attendre que la scène soit complètement chargée
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log($"[SceneStreamCapture] Scene '{sceneToCapture}' loaded successfully");

        // Attendre que tout soit initialisé
        yield return new WaitForSeconds(0.5f);

        // Trouver automatiquement la caméra de la scène chargée
        captureCamera = Camera.main;

        // Si pas de Main Camera, chercher n'importe quelle caméra
        if (captureCamera == null)
        {
            captureCamera = FindObjectOfType<Camera>();
        }

        if (captureCamera == null)
        {
            Debug.LogError("[SceneStreamCapture] No Camera found!");
            yield break;
        }

        // Récupérer la RawImage sur le même GameObject que ce script
        displayImage = GetComponent<RawImage>();
        if (displayImage == null)
        {
            Debug.LogError("[SceneStreamCapture] No RawImage found on this GameObject!");
            yield break;
        }

        Debug.Log($"[SceneStreamCapture] RawImage found on {gameObject.name}");

        // Calculer les dimensions basées sur la RawImage
        RectTransform rectTransform = displayImage.GetComponent<RectTransform>();
        float rawImageHeight = rectTransform.rect.height;
        float rawImageWidth = rectTransform.rect.width;

        // Garder le ratio de la texture source
        float sourceAspectRatio = 16f / 9f; // Ratio par défaut

        // Calculer la hauteur de capture = hauteur de la RawImage
        captureHeight = Mathf.Max((int)rawImageHeight, 64);

        // Calculer la largeur pour garder le ratio
        captureWidth = Mathf.Max((int)(captureHeight * sourceAspectRatio), 64);

        renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
        captureCamera.targetTexture = renderTexture;

        screenTexture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);

        isCapturing = true;
        Debug.Log($"[SceneStreamCapture] ✓ Capture started: {captureWidth}x{captureHeight}, camera: {captureCamera.name}");
    }

    void Update()
    {
        if (!isCapturing || screenTexture == null || captureCamera == null)
            return;

        CaptureAndDisplay();
        ApplyCrop();
    }

    void ApplyCrop()
    {
        if (displayImage == null || displayImage.texture == null)
            return;

        // Récupérer le ratio de la RawImage (UI)
        RectTransform rectTransform = displayImage.GetComponent<RectTransform>();
        if (rectTransform == null)
            return;

        float uiWidth = rectTransform.rect.width;
        float uiHeight = rectTransform.rect.height;

        if (uiHeight <= 0)
            return;

        float uiAspectRatio = uiWidth / uiHeight;

        // Ratio de la texture
        float textureAspectRatio = (float)captureWidth / captureHeight;

        // Calculer le crop pour remplir la hauteur, cropper la largeur si nécessaire
        float cropWidth = Mathf.Min(1f, uiAspectRatio / textureAspectRatio);

        // Centrer le crop horizontalement
        float startX = (1f - cropWidth) / 2f;

        // Appliquer le crop
        displayImage.uvRect = new Rect(startX, 0f, cropWidth, 1f);
    }
    void CaptureAndDisplay()
    {
        // Rendre à la RenderTexture
        RenderTexture.active = renderTexture;

        // Lire les pixels de la RenderTexture
        screenTexture.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        screenTexture.Apply();

        RenderTexture.active = null;

        // Assigner la texture à la RawImage si elle existe
        if (displayImage != null)
        {
            displayImage.texture = screenTexture;
        }
    }

    public byte[] GetCurrentFrameAsJPEG()
    {
        if (screenTexture == null)
            return null;

        return screenTexture.EncodeToJPG(50); // Qualité fixe
    }

    public Texture2D GetScreenTexture()
    {
        return screenTexture;
    }

    void OnDestroy()
    {
        // Nettoyer les ressources
        if (renderTexture != null)
        {
            captureCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(renderTexture);
        }

        if (screenTexture != null)
        {
            Destroy(screenTexture);
        }
    }
}
