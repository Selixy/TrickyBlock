using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LocalIPDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI ipDisplayText;

    private void Start()
    {
        if (ipDisplayText == null)
        {
            ipDisplayText = GetComponent<TextMeshProUGUI>();
            if (ipDisplayText == null)
            {
                Debug.LogError("[LocalIPDisplay] TextMeshProUGUI component not found!");
                return;
            }
        }

        // Trouver et afficher l'IP locale Ethernet
        string localIP = GetLocalIPAddress();
        DisplayIP(localIP);
    }

    /// <summary>
    /// Récupère l'adresse IP locale en utilisant les APIs disponibles
    /// </summary>
    private string GetLocalIPAddress()
    {
        try
        {
            // Utiliser Dns pour récupérer le hostname et les adresses associées
            string hostName = System.Net.Dns.GetHostName();
            System.Net.IPHostEntry ipHostInfo = System.Net.Dns.GetHostEntry(hostName);

            Debug.Log($"[LocalIPDisplay] Hostname: {hostName}");

            // Chercher une adresse IPv4 valide (pas loopback)
            foreach (System.Net.IPAddress ipAddress in ipHostInfo.AddressList)
            {
                // Vérifier que c'est une adresse IPv4
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    string ip = ipAddress.ToString();

                    // Ignorer loopback
                    if (!ip.StartsWith("127."))
                    {
                        Debug.Log($"[LocalIPDisplay] IP locale trouvée: {ip}");
                        return ip;
                    }
                }
            }

            return "IP non trouvée";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LocalIPDisplay] Erreur lors de la récupération de l'IP: {e.Message}");
            return "Erreur";
        }
    }

    /// <summary>
    /// Affiche l'IP dans le TextMeshPro
    /// </summary>
    private void DisplayIP(string ipAddress)
    {
        if (ipDisplayText != null)
        {
            ipDisplayText.text = ipAddress;
            Debug.Log($"[LocalIPDisplay] Affichage de l'IP: {ipAddress}");
        }
        else
        {
            Debug.LogWarning("[LocalIPDisplay] ipDisplayText non assigné");
        }
    }

    /// <summary>
    /// Permet de rafraîchir l'IP manuellement
    /// </summary>
    public void RefreshIP()
    {
        string localIP = GetLocalIPAddress();
        DisplayIP(localIP);
    }
}
