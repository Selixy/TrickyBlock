using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneStreamCapture : SceneCaptureBase
{
    [Header("Scene")]
    public string sceneToCapture = "GameScene";

    void Start()
    {
        Debug.Log($"[SceneStreamCapture] Loading scene: {sceneToCapture}");
        StartCoroutine(LoadSceneAndInitialize());
    }

    IEnumerator LoadSceneAndInitialize()
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

        // Initialiser la capture
        InitializeScene();
        InitializeCapture();
    }

    protected override void InitializeScene()
    {
        // La scène est chargée depuis un fichier, rien à faire ici
        Debug.Log("[SceneStreamCapture] Scene loaded from file");
    }

    void Update()
    {
        CaptureAndDisplay();
        ApplyCrop();
    }

    public byte[] GetCurrentFrameAsJPEG()
    {
        if (screenTexture == null)
            return null;

        return screenTexture.EncodeToJPG(50);
    }

    public Texture2D GetScreenTexture()
    {
        return screenTexture;
    }
}
