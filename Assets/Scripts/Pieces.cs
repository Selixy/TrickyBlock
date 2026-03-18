using UnityEngine;

public class Piece : MonoBehaviour
{
    void Update()
    {
        if (transform.position.y < -30f)
        {
            Destroy(gameObject);
        }
    }
}