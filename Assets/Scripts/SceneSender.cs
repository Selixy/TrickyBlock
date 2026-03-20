using UnityEngine;
using System.Collections.Generic;

public class SceneSender : MonoBehaviour
{
    [SerializeField] private float sendInterval = 0.033f;
    private float lastSendTime = 0f;
    private OscSender oscSender;

    [System.Serializable]
    public class SceneObject
    {
        public string name;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }

    [System.Serializable]
    public class SceneData
    {
        public List<SceneObject> objects = new List<SceneObject>();
    }

    void Start()
    {
        // Trouver OscSender dans la scène additive
        if (oscSender == null)
        {
            oscSender = FindObjectOfType<OscSender>();
            if (oscSender == null)
            {
                Debug.LogError("[SceneSender] OscSender not found in scene!");
                return;
            }
            Debug.Log("[SceneSender] OscSender found automatically");
        }
    }

    void Update()
    {
        if (Time.time - lastSendTime >= sendInterval)
        {
            SendSceneData();
            lastSendTime = Time.time;
        }
    }

    void SendSceneData()
    {
        SceneData sceneData = new SceneData();

        // Envoyer les PIÈCES (les objets avec le script Piece)
        var pieces = FindObjectsOfType<Piece>();
        foreach (var piece in pieces)
        {
            sceneData.objects.Add(new SceneObject
            {
                name = piece.gameObject.name,
                position = piece.transform.position,
                rotation = piece.transform.eulerAngles,
                scale = piece.transform.localScale
            });
        }

        // Envoyer aussi la CAMÉRA (pour la capture texture)
        var camera = FindObjectOfType<Camera>();
        if (camera != null)
        {
            sceneData.objects.Add(new SceneObject
            {
                name = camera.gameObject.name,
                position = camera.transform.position,
                rotation = camera.transform.eulerAngles,
                scale = camera.transform.localScale
            });
        }

        if (sceneData.objects.Count == 0)
            return;

        string json = JsonUtility.ToJson(sceneData);
        oscSender.Send("/scene", json);

        Debug.Log($"[SceneSender] Envoyé {sceneData.objects.Count} objets (pièces + caméra)");
    }
}