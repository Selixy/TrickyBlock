using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Classe de base pour capturer et afficher des scènes
/// Factorisation commune entre SceneStreamCapture et SceneReconstructor
/// </summary>
public abstract class SceneCaptureBase : MonoBehaviour
{
    [Header("Capture Settings")]
    protected Texture2D screenTexture;
    protected RenderTexture renderTexture;
    protected Camera captureCamera;
    protected RawImage displayImage;
    protected int captureWidth = 640;
    protected int captureHeight = 480;
    protected bool isCapturing = false;

    /// <summary>
    /// À implémenter par les classes dérivées pour initialiser leur scène
    /// </summary>
    protected abstract void InitializeScene();

    /// <summary>
    /// Initialise les resources de capture (RenderTexture, caméra, RawImage)
    /// </summary>
    protected virtual void InitializeCapture()
    {
        // Récupérer la RawImage
        displayImage = GetComponent<RawImage>();
        if (displayImage == null)
        {
            Debug.LogError($"[{GetType().Name}] No RawImage found on this GameObject!");
            return;
        }

        // Trouver ou créer une caméra
        captureCamera = FindObjectOfType<Camera>();
        if (captureCamera == null)
        {
            GameObject cameraObj = new GameObject("CaptureCamera");
            cameraObj.transform.SetParent(transform.parent);
            captureCamera = cameraObj.AddComponent<Camera>();
        }

        // Calculer les dimensions basées sur la RawImage
        RectTransform rectTransform = displayImage.GetComponent<RectTransform>();
        float rawImageHeight = rectTransform.rect.height;
        float rawImageWidth = rectTransform.rect.width;

        float sourceAspectRatio = 16f / 9f;
        captureHeight = Mathf.Max((int)rawImageHeight, 64);
        captureWidth = Mathf.Max((int)(captureHeight * sourceAspectRatio), 64);

        // Créer les textures
        renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
        captureCamera.targetTexture = renderTexture;
        screenTexture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);

        isCapturing = true;
        Debug.Log($"[{GetType().Name}] ✓ Capture initialized: {captureWidth}x{captureHeight}");
    }

    /// <summary>
    /// Capture et affiche la frame actuelle
    /// </summary>
    protected virtual void CaptureAndDisplay()
    {
        if (!isCapturing || screenTexture == null || captureCamera == null)
            return;

        RenderTexture.active = renderTexture;
        screenTexture.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        screenTexture.Apply();
        RenderTexture.active = null;

        if (displayImage != null)
        {
            displayImage.texture = screenTexture;
        }
    }

    /// <summary>
    /// Applique le crop autonome selon le ratio
    /// </summary>
    protected virtual void ApplyCrop()
    {
        if (displayImage == null || displayImage.texture == null)
            return;

        RectTransform rectTransform = displayImage.GetComponent<RectTransform>();
        if (rectTransform == null)
            return;

        float uiWidth = rectTransform.rect.width;
        float uiHeight = rectTransform.rect.height;

        if (uiHeight <= 0)
            return;

        float uiAspectRatio = uiWidth / uiHeight;
        float textureAspectRatio = (float)captureWidth / captureHeight;
        float cropWidth = Mathf.Min(1f, uiAspectRatio / textureAspectRatio);
        float startX = (1f - cropWidth) / 2f;

        displayImage.uvRect = new Rect(startX, 0f, cropWidth, 1f);
    }

    protected virtual void OnDestroy()
    {
        if (renderTexture != null)
        {
            if (captureCamera != null)
                captureCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(renderTexture);
        }

        if (screenTexture != null)
            Destroy(screenTexture);

        isCapturing = false;
    }
}
