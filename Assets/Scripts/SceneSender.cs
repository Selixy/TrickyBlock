using UnityEngine;

public class SceneSender : MonoBehaviour
{
    public OscSender oscSender; // Référence à l'OscSender

    void Start()
    {
        if (oscSender == null)
        {
            Debug.LogError("OscSender n'est pas assigné !");
            return;
        }

        // Exemple : Envoyer la scène toutes les 5 secondes
        InvokeRepeating(nameof(SendSceneData), 0f, 5f);
    }

    void SendSceneData()
    {
        // Collecter les données des objets dans la scène
        var objects = FindObjectsOfType<Transform>();
        foreach (var obj in objects)
        {
            if (obj.CompareTag("Sendable")) // Filtrer les objets à envoyer
            {
                string data = SerializeObjectData(obj);
                oscSender.Send("/sceneData", data); // Envoyer les données via OSC
            }
        }

        Debug.Log("Données de la scène envoyées.");
    }

    string SerializeObjectData(Transform obj)
    {
        // Sérialiser les données de l'objet (position, rotation, etc.)
        return $"{obj.name}|{obj.position.x},{obj.position.y},{obj.position.z}|{obj.rotation.eulerAngles.x},{obj.rotation.eulerAngles.y},{obj.rotation.eulerAngles.z}";
    }
}