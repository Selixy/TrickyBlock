using UnityEngine;

public class GolRunUp : MonoBehaviour
{
    public float descendSpeed = 2f;
    public LayerMask pieceLayer;

    private bool isCountingDown = false;
    private float countdown = 3f;
    private int overlapCount = 0;

    private void Update()
    {
        transform.Translate(Vector2.down * descendSpeed * Time.deltaTime);

        if (!isCountingDown) return;

        if (overlapCount <= 0)
        {
            isCountingDown = false;
            countdown = 3f;
            Debug.Log("Compte à rebours annulé — plus de pièce");
            return;
        }

        countdown -= Time.deltaTime;
        Debug.Log($"Countdown: {countdown:F2} — pièces présentes: {overlapCount}");

        if (countdown <= 0f)
        {
            isCountingDown = false;
            Win();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((pieceLayer.value & (1 << other.gameObject.layer)) == 0) return;

        overlapCount++;
        Debug.Log($"Entrée '{other.name}' — overlapCount={overlapCount}");

        if (!isCountingDown)
        {
            isCountingDown = true;
            countdown = 3f;
            Debug.Log("Compte à rebours démarré");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if ((pieceLayer.value & (1 << other.gameObject.layer)) == 0) return;

        overlapCount = Mathf.Max(0, overlapCount - 1);
        Debug.Log($"Sortie '{other.name}' — overlapCount={overlapCount}");
    }

    private void Win()
    {
        Debug.Log("GOL WIN!");
    }
}