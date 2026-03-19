using UnityEngine;

public class PieceDebugVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerControl playerControl;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Color debugColor = Color.cyan;

    private SpriteRenderer spriteRenderer;
    private RectTransform rectTransform;
    private GameObject lastTrackedPiece;
    private int lastWidthValue = -1;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rectTransform = GetComponent<RectTransform>();

        if (playerControl == null)
        {
            playerControl = FindObjectOfType<PlayerControl>();
        }
    }

    private void LateUpdate()
    {
        if (playerControl == null)
            return;

        // On utilise la réflection pour accéder aux champs privés de PlayerControl
        System.Reflection.FieldInfo currentPieceField =
            playerControl.GetType().GetField("currentPiece",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (currentPieceField == null)
            return;

        GameObject currentPiece = currentPieceField.GetValue(playerControl) as GameObject;

        if (currentPiece == null)
        {
            // Pas de pièce active : cacher le debug
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (rectTransform != null) rectTransform.sizeDelta = Vector2.one;
            return;
        }

        lastTrackedPiece = currentPiece;

        // Calculer la largeur de la pièce via ses colliders (en espace mondial)
        float pieceWidth = CalculatePieceWidth(currentPiece);
        int widthValue = RoundWidthToGridValue(pieceWidth);

        // Vérifier si la largeur discrète a changé
        bool widthChanged = (widthValue != lastWidthValue);

        // Adapter la scale X du sprite (discrète: 1, 2, 3 ou 4)
        Vector3 scale = transform.localScale;
        scale.x = widthValue;
        transform.localScale = scale;

        // Adapter la position Y pour que le carré soit à hauteur appropriée
        UpdateDebugVisualPosition(currentPiece, widthValue);

        if (spriteRenderer != null) spriteRenderer.enabled = true;

        // Log chaque frame en debug mode, ou seulement si changement
        if (showDebugInfo)
        {
            float currentRotation = currentPiece.transform.eulerAngles.z;
            if (widthChanged)
            {
                Debug.Log($"[PieceDebugVisual] {currentPiece.name} - Width Value: {widthValue}, Rotation: {currentRotation:F1}°");
                lastWidthValue = widthValue;
            }
        }
    }

    /// <summary>
    /// Arrondit la largeur à une valeur discrète (1, 2, 3 ou 4 cases).
    /// </summary>
    private int RoundWidthToGridValue(float width)
    {
        // Arrondir à l'entier le plus proche, limité entre 1 et 4
        int rounded = Mathf.RoundToInt(width);
        return Mathf.Clamp(rounded, 1, 4);
    }

    /// <summary>
    /// Calcule la largeur réelle de la pièce en fonction de ses colliders en espace mondial.
    /// </summary>
    private float CalculatePieceWidth(GameObject piece)
    {
        BoxCollider2D[] colliders = piece.GetComponentsInChildren<BoxCollider2D>();

        if (colliders.Length == 0)
            return 1f; // Fallback

        float minX = float.MaxValue;
        float maxX = float.MinValue;

        // On calcule en espace mondial pour que la rotation soit prise en compte
        foreach (BoxCollider2D collider in colliders)
        {
            Vector2 size = collider.size;
            Vector3 worldPos = collider.transform.position;

            // Appliquer la rotation du collider pour obtenir les vraies limites X
            Quaternion rotation = collider.transform.rotation;
            Vector2 halfSize = size * 0.5f;

            // Les 4 coins du collider en espace local
            Vector3[] corners = new Vector3[4]
            {
                new Vector3(-halfSize.x, -halfSize.y, 0),
                new Vector3(halfSize.x, -halfSize.y, 0),
                new Vector3(halfSize.x, halfSize.y, 0),
                new Vector3(-halfSize.x, halfSize.y, 0)
            };

            // Transformer les coins en espace mondial et trouver les limites X
            foreach (Vector3 corner in corners)
            {
                Vector3 rotatedCorner = rotation * corner;
                float worldX = worldPos.x + rotatedCorner.x;
                minX = Mathf.Min(minX, worldX);
                maxX = Mathf.Max(maxX, worldX);
            }
        }

        float width = maxX - minX;
        return Mathf.Max(width, 0.1f); // Éviter 0
    }

    /// <summary>
    /// Met à jour la position du debug visual pour qu'il soit centré sur la pièce.
    /// </summary>
    private void UpdateDebugVisualPosition(GameObject piece, int widthValue)
    {
        // Positionner le debug visual à la même position X que la pièce
        Vector3 piecePos = piece.transform.position;
        Vector3 debugPos = transform.position;
        debugPos.x = piecePos.x;
        debugPos.y = piecePos.y;
        transform.position = debugPos;
    }
}
