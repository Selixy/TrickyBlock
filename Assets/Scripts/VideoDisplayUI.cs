using UnityEngine;
using UnityEngine.UI;

public class VideoDisplayUI : MonoBehaviour
{
    public enum PlayerRole { Player1, Player2, MyCamera }

    [Header("Configuration")]
    public PlayerRole targetPlayer = PlayerRole.Player1;

    private Texture2D currentTexture = null;
    private float currentCropX = 0.5f;
    private RectTransform rectTransform = null;
    private RawImage displayImage = null;

    void OnEnable()
    {
        // Récupérer la RawImage automatiquement
        displayImage = GetComponent<RawImage>();
        if (displayImage == null)
        {
            Debug.LogError($"VideoDisplayUI: RawImage component not found on {gameObject.name}");
            return;
        }

        Debug.Log($"[VideoDisplayUI] OnEnable for {targetPlayer} on {gameObject.name}");

        // Récupérer le RectTransform pour les calculs de ratio
        rectTransform = displayImage.GetComponent<RectTransform>();

        // S'abonner aux événements du manager réseau
        if (targetPlayer == PlayerRole.Player1)
        {
            Debug.Log($"[VideoDisplayUI] Subscribing to OnPlayer1VideoReceived");
            CrossVideoNetworkManager.OnPlayer1VideoReceived += OnVideoReceived;
        }
        else if (targetPlayer == PlayerRole.Player2)
        {
            Debug.Log($"[VideoDisplayUI] Subscribing to OnPlayer2VideoReceived");
            CrossVideoNetworkManager.OnPlayer2VideoReceived += OnVideoReceived;
        }
        else if (targetPlayer == PlayerRole.MyCamera)
        {
            Debug.Log($"[VideoDisplayUI] Subscribing to OnMyVideoCapture (DEBUG)");
            CrossVideoNetworkManager.OnMyVideoCapture += OnVideoReceived;
        }
    }

    void OnDisable()
    {
        // Se désabonner pour éviter les fuites mémoire
        if (targetPlayer == PlayerRole.Player1)
        {
            CrossVideoNetworkManager.OnPlayer1VideoReceived -= OnVideoReceived;
        }
        else if (targetPlayer == PlayerRole.Player2)
        {
            CrossVideoNetworkManager.OnPlayer2VideoReceived -= OnVideoReceived;
        }
        else if (targetPlayer == PlayerRole.MyCamera)
        {
            CrossVideoNetworkManager.OnMyVideoCapture -= OnVideoReceived;
        }
    }
    void OnVideoReceived(byte[] jpegData, float cropX)
    {
        Debug.Log($"[VideoDisplayUI] OnVideoReceived called for {targetPlayer} - data size: {jpegData?.Length ?? 0}");

        if (displayImage == null)
        {
            Debug.LogWarning($"VideoDisplayUI: RawImage not assigned!");
            return;
        }

        if (jpegData == null || jpegData.Length == 0)
        {
            Debug.LogWarning($"[VideoDisplayUI] Invalid JPEG data received");
            return;
        }

        // Créer une nouvelle texture avec les bonnes dimensions
        // On utilise 1x1 comme size par défaut, LoadImage va redimensionner automatiquement
        Texture2D newTexture = new Texture2D(1, 1, TextureFormat.RGB24, false);
        newTexture.name = $"VideoFrame_{targetPlayer}";

        if (newTexture.LoadImage(jpegData))
        {
            // Détruire l'ancienne texture
            if (currentTexture != null)
            {
                Destroy(currentTexture);
            }

            currentTexture = newTexture;
            currentCropX = cropX;

            // Assigner à la RawImage en temps réel
            displayImage.texture = currentTexture;

            Debug.Log($"[VideoDisplayUI] ✓ Texture assigned: {currentTexture.width}x{currentTexture.height} for {targetPlayer}");

            // Appliquer le crop
            ApplyCrop();
        }
        else
        {
            Debug.LogError($"[VideoDisplayUI] ✗ Failed to load JPEG data for {targetPlayer} (size: {jpegData.Length})");
            Destroy(newTexture);
        }
    }

    void Update()
    {
        // Mettre à jour le crop à chaque frame (en cas de changement)
        ApplyCrop();
    }

    void ApplyCrop()
    {
        if (displayImage == null || displayImage.texture == null)
            return;

        // Récupérer le ratio de l'écran/RawImage
        float screenAspectRatio = GetScreenAspectRatio();

        // Récupérer le ratio de la texture vidéo
        Texture2D tex = displayImage.texture as Texture2D;
        float textureAspectRatio = (float)tex.width / tex.height;

        // Calculer le ratio de crop automatiquement
        // Si l'écran est plus large que la texture, on crop la largeur
        // Sinon on affiche toute la largeur
        float cropWidthRatio = Mathf.Min(1.0f, screenAspectRatio / textureAspectRatio);

        // Assurer au moins 50% de largeur visible
        cropWidthRatio = Mathf.Max(cropWidthRatio, 0.5f);

        // Calculer la position de crop en centrant sur currentCropX
        float startX = Mathf.Clamp(
            currentCropX - (cropWidthRatio / 2f),
            0f,
            1f - cropWidthRatio
        );

        // Appliquer le rect de crop (x, y, width, height)
        displayImage.uvRect = new Rect(startX, 0f, cropWidthRatio, 1.0f);
    }

    float GetScreenAspectRatio()
    {
        // Récupérer le ratio de la RawImage si possible
        if (rectTransform != null)
        {
            Rect rect = rectTransform.rect;
            if (rect.height > 0)
            {
                return rect.width / rect.height;
            }
        }

        // Sinon utiliser le ratio de l'écran global
        if (Screen.height > 0)
        {
            return (float)Screen.width / Screen.height;
        }

        return 1.0f; // Fallback
    }

    void OnDestroy()
    {
        // Nettoyer la texture
        if (currentTexture != null)
        {
            Destroy(currentTexture);
        }
    }
}
