using UnityEngine;
using TMPro;

public class ServerIPInput : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_InputField serverIPInputField;

    private string defaultServerIP = "127.0.0.1";

    private void Start()
    {
        // Auto-découvrir le composant InputField si non assigné
        if (serverIPInputField == null)
        {
            serverIPInputField = GetComponent<TMP_InputField>();
            if (serverIPInputField == null)
            {
                Debug.LogError("[ServerIPInput] TMP_InputField component not found!");
                return;
            }
        }

        // Initialiser avec l'IP par défaut
        serverIPInputField.text = defaultServerIP;

        // S'abonner aux changements de l'input field
        serverIPInputField.onEndEdit.AddListener(OnServerIPChanged);

        Debug.Log($"[ServerIPInput] Initialized with default IP: {defaultServerIP}");
    }

    /// <summary>
    /// Appelé quand l'utilisateur valide l'IP (Enter ou perte du focus)
    /// </summary>
    private void OnServerIPChanged(string newIP)
    {
        // Valider que l'IP n'est pas vide
        if (string.IsNullOrWhiteSpace(newIP))
        {
            Debug.LogWarning("[ServerIPInput] IP vide, utilisation de l'IP par défaut");
            serverIPInputField.text = defaultServerIP;
            newIP = defaultServerIP;
        }

        // Valider le format de l'IP (simple vérification)
        if (!IsValidIP(newIP))
        {
            Debug.LogWarning($"[ServerIPInput] IP invalide: {newIP}, utilisation de l'IP par défaut");
            serverIPInputField.text = defaultServerIP;
            newIP = defaultServerIP;
        }

        Debug.Log($"[ServerIPInput] Nouvelle IP serveur: {newIP}");

        // Envoyer l'IP à CrossVideoNetworkManager (VideoTcpSender)
        if (CrossVideoNetworkManager.Instance != null)
        {
            CrossVideoNetworkManager.Instance.rustServerIP = newIP;
            Debug.Log($"[ServerIPInput] ✓ IP envoyée à CrossVideoNetworkManager: {newIP}");
        }
        else
        {
            Debug.LogWarning("[ServerIPInput] CrossVideoNetworkManager.Instance non trouvé!");
        }

        // Envoyer l'IP à OscReceiver si disponible
        OscReceiver oscReceiver = FindObjectOfType<OscReceiver>();
        if (oscReceiver != null)
        {
            oscReceiver.SetServerIP(newIP);
            Debug.Log($"[ServerIPInput] ✓ IP envoyée à OscReceiver: {newIP}");
        }
        else
        {
            Debug.LogWarning("[ServerIPInput] OscReceiver non trouvé!");
        }
    }

    /// <summary>
    /// Valide le format basique d'une adresse IP
    /// </summary>
    private bool IsValidIP(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;

        string[] parts = ip.Split('.');

        // Une IP valide a 4 parties
        if (parts.Length != 4)
            return false;

        // Chaque partie doit être un nombre entre 0 et 255
        foreach (string part in parts)
        {
            if (!int.TryParse(part, out int num) || num < 0 || num > 255)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Permet de définir l'IP par code si besoin
    /// </summary>
    public void SetServerIP(string newIP)
    {
        if (serverIPInputField != null)
        {
            serverIPInputField.text = newIP;
            OnServerIPChanged(newIP);
        }
    }

    /// <summary>
    /// Retourne l'IP serveur actuelle
    /// </summary>
    public string GetServerIP()
    {
        return serverIPInputField != null ? serverIPInputField.text : defaultServerIP;
    }

    /// <summary>
    /// Réinitialise l'IP à la valeur par défaut
    /// </summary>
    public void ResetToDefault()
    {
        SetServerIP(defaultServerIP);
    }

    private void OnDestroy()
    {
        // Nettoyer le listener
        if (serverIPInputField != null)
        {
            serverIPInputField.onEndEdit.RemoveListener(OnServerIPChanged);
        }
    }
}
