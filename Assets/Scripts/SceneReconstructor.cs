using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SceneReconstructor : SceneCaptureBase
{
    [SerializeField] private OscReceiver oscReceiver;
    [SerializeField] private string additiveSceneName = "MainPlayer"; // Scène à reconstruire

    private Dictionary<string, GameObject> reconstructedObjects = new Dictionary<string, GameObject>();
    private Transform objectsContainer;

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
        if (oscReceiver == null)
        {
            oscReceiver = FindObjectOfType<OscReceiver>();
        }

        if (oscReceiver != null)
        {
            oscReceiver.OnSceneDataReceived += OnReceiveSceneData;
            Debug.Log("[SceneReconstructor] Registered to OSC receiver");
        }
        else
        {
            Debug.LogError("[SceneReconstructor] No OscReceiver found!");
        }

        // Initialiser la scène et la capture
        InitializeScene();
        InitializeCapture();
    }

    protected override void InitializeScene()
    {
        // Créer un GameObject conteneur pour les objets reconstruits
        objectsContainer = new GameObject("ReconstructedObjects").transform;
        objectsContainer.SetParent(transform.parent);

        Debug.Log("[SceneReconstructor] Scene initialized");
    }

    void Update()
    {
        // Capture continue
        CaptureAndDisplay();
        ApplyCrop();
    }

    void OnReceiveSceneData(string json)
    {
        try
        {
            SceneData sceneData = JsonUtility.FromJson<SceneData>(json);

            if (sceneData == null || sceneData.objects.Count == 0)
                return;

            // Update or create objects
            var receivedNames = new HashSet<string>();

            foreach (var obj in sceneData.objects)
            {
                receivedNames.Add(obj.name);
                UpdateOrCreateObject(obj);
            }

            // Remove objects that are no longer in the scene
            var keysToRemove = reconstructedObjects.Keys.Where(k => !receivedNames.Contains(k)).ToList();
            foreach (var key in keysToRemove)
            {
                Destroy(reconstructedObjects[key]);
                reconstructedObjects.Remove(key);
            }

            Debug.Log($"[SceneReconstructor] Updated {sceneData.objects.Count} objects");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SceneReconstructor] JSON Parse error: {e.Message}");
        }
    }

    void UpdateOrCreateObject(SceneObject sceneObj)
    {
        GameObject go;

        if (reconstructedObjects.ContainsKey(sceneObj.name))
        {
            // Update existing
            go = reconstructedObjects[sceneObj.name];
        }
        else
        {
            // Create new
            go = new GameObject(sceneObj.name);
            go.transform.SetParent(objectsContainer, false);
            reconstructedObjects[sceneObj.name] = go;

            // Enlever les Rigidbody (physique calculée côté P2)
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            Rigidbody2D rb2d = go.GetComponent<Rigidbody2D>();
            if (rb2d != null) Destroy(rb2d);
        }

        // Update transform
        go.transform.position = sceneObj.position;
        go.transform.eulerAngles = sceneObj.rotation;
        go.transform.localScale = sceneObj.scale;
    }

    new void OnDestroy()
    {
        if (oscReceiver != null)
        {
            oscReceiver.OnSceneDataReceived -= OnReceiveSceneData;
        }

        if (objectsContainer != null)
        {
            Destroy(objectsContainer.gameObject);
        }

        base.OnDestroy();
    }
}
