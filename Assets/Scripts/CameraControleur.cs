using UnityEngine;

public class CameraControleur : MonoBehaviour
{
    private PlayerControl playerControl;

    [Header("Offsets")]
    [SerializeField] private float cameraOffset = -10f;
    [SerializeField] private float lerpSpeed = 5f;

    private float minHeight;

    private void Start()
    {
        if (playerControl == null)
            playerControl = FindObjectOfType<PlayerControl>();

        minHeight = transform.position.y; // Store the initial height as the minimum height
    }

    private void Update()
    {
        if (playerControl == null) return;

        Vector3 targetPosition = new Vector3(transform.position.x, Mathf.Max(playerControl.SpawnY + cameraOffset, minHeight), transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
    }
}