using UnityEngine;

public class PieceCollisionListener : MonoBehaviour
{
    private PlayerControl parentControl;
    private bool triggered = false;

    [Tooltip("Magnitude minimale de l'impact pour déclencher le lock")]
    [SerializeField] private float impactThreshold = 1f;
    [Tooltip("Seuil de verticalité de la normale (1 = parfaitement vertical)")]
    [SerializeField] private float normalThreshold = 0.5f;

    private BoxCollider2D[] boxes;
    private Vector2[] originalSizes;

    private void Awake()
    {
        boxes = GetComponentsInChildren<BoxCollider2D>();
        originalSizes = new Vector2[boxes.Length];
        for (int i = 0; i < boxes.Length; i++)
            originalSizes[i] = boxes[i].size;

        for (int i = 0; i < boxes.Length; i++)
            boxes[i].size = originalSizes[i] * 0.98f;
    }

    public void AssignParent(PlayerControl control)
    {
        parentControl = control;
    }

    private void Update()
    {
        if (triggered) return;
        if (transform.position.y < -20f)
            TriggerLock(null);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (triggered) return;

        bool hasVelocityImpact = collision.relativeVelocity.magnitude >= impactThreshold;

        bool hasVerticalContact = false;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y >= normalThreshold)
            {
                hasVerticalContact = true;
                break;
            }
        }

        if (!hasVelocityImpact && !hasVerticalContact) return;

        TriggerLock(collision);
    }

    private void TriggerLock(Collision2D collision)
    {
        triggered = true;

        for (int i = 0; i < boxes.Length; i++)
            boxes[i].size = originalSizes[i];

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (parentControl != null)
            parentControl.OnPieceCollision(this.gameObject, collision);

        Destroy(this);
    }
}