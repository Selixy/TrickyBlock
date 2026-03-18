using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerControl : MonoBehaviour
{
    [Header("Ressources")]
    [SerializeField] private string resourcesFolder = "Pieces";

    [Header("Spawn")]
    [SerializeField] private Transform spawnParent;
    [SerializeField] private float fallSpeed = 4f;
    [SerializeField] private float spawnHeightOffset = 10f;
    [SerializeField] private float raycastSpread = 10f;
    [SerializeField] private float raycastStep = 0.25f;
    [SerializeField] private float raycastDistance = 200f;

    [Header("Déplacement joueur")]
    [SerializeField] private float stepSize = 0.5f;
    [SerializeField] private float moveCooldown = 0.08f;
    [SerializeField] private float moveRepeatDelay = 0.15f;
    [SerializeField] private float moveRepeatInterval = 0.08f;
    [SerializeField] private InputActionReference moveLeftAction;
    [SerializeField] private InputActionReference moveRightAction;

    [Header("Dash")]
    [SerializeField] private float dashSize = 1f;
    [SerializeField] private float dashCooldown = 0.2f;
    [SerializeField] private InputActionReference dashLeftAction;
    [SerializeField] private InputActionReference dashRightAction;

    [Header("Rotation")]
    [SerializeField] private InputActionReference rotateLeftAction;
    [SerializeField] private InputActionReference rotateRightAction;

    [Header("Chute rapide")]
    [SerializeField] private float fastFallMultiplier = 2f;
    [SerializeField] private InputActionReference fastFallAction;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask = ~0;

    private const float ROTATE_SNAP_SPEED = 150f;
    private const float STEP_ROTATION = 90f;

    private OscReceiver oscReceiver;
    private GameObject[] piecePrefabs;
    private float defaultHeight;
    private GameObject currentPiece;
    private Rigidbody2D currentPieceRb;

    private float targetRotationZ;
    private Vector3 previousPosition;
    private bool isFastFalling = false;
    public float SpawnY { get; private set; }

    private float lastMoveTime = -999f;
    private float lastDashTime = -999f;

    private Coroutine moveLeftCoroutine;
    private Coroutine moveRightCoroutine;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        if (spawnParent == null) spawnParent = this.transform;
        defaultHeight = transform.position.y;
        previousPosition = transform.position;

        GameObject networkObj = GameObject.Find("Network");
        if (networkObj != null)
        {
            oscReceiver = networkObj.GetComponent<OscReceiver>();
            if (oscReceiver != null)
            {
                Debug.Log("[PlayerControl] OscReceiver trouvé sur 'Network'");
                SubscribeOsc(); // ← ici, pas dans OnEnable
            }
            else
                Debug.LogWarning("[PlayerControl] 'Network' trouvé mais pas de OscReceiver");
        }
        else
            Debug.LogWarning("[PlayerControl] GameObject 'Network' introuvable");

        CreatePiece();
    }

    private void OnEnable()
    {
        // Input System uniquement — OSC géré dans Start
        SubscribeMove(moveLeftAction, OnMoveLeftPerformed, OnMoveLeftCanceled);
        SubscribeMove(moveRightAction, OnMoveRightPerformed, OnMoveRightCanceled);

        EnableAction(dashLeftAction, OnDashLeft);
        EnableAction(dashRightAction, OnDashRight);
        EnableAction(rotateLeftAction, OnRotateLeft);
        EnableAction(rotateRightAction, OnRotateRight);

        if (fastFallAction != null)
        {
            fastFallAction.action.Enable();
            fastFallAction.action.performed += OnFastFallStarted;
            fastFallAction.action.canceled += OnFastFallStopped;
        }
    }

    private void OnDisable()
    {
        UnsubscribeMove(moveLeftAction, OnMoveLeftPerformed, OnMoveLeftCanceled);
        UnsubscribeMove(moveRightAction, OnMoveRightPerformed, OnMoveRightCanceled);

        DisableAction(dashLeftAction, OnDashLeft);
        DisableAction(dashRightAction, OnDashRight);
        DisableAction(rotateLeftAction, OnRotateLeft);
        DisableAction(rotateRightAction, OnRotateRight);

        if (fastFallAction != null)
        {
            fastFallAction.action.performed -= OnFastFallStarted;
            fastFallAction.action.canceled -= OnFastFallStopped;
            fastFallAction.action.Disable();
        }

        UnsubscribeOsc();
        StopAllCoroutines();
    }

    // ─── Abonnement OSC ───────────────────────────────────────────────────────

    private void SubscribeOsc()
    {
        if (oscReceiver == null) return;
        oscReceiver.OnMoveLeft += OscMoveLeft;
        oscReceiver.OnMoveRight += OscMoveRight;
        oscReceiver.OnDashLeft += OscDashLeft;
        oscReceiver.OnDashRight += OscDashRight;
        oscReceiver.OnRotateLeft += OscRotateLeft;
        oscReceiver.OnRotateRight += OscRotateRight;
        oscReceiver.OnFastFallStart += OscFastFallStart;
        oscReceiver.OnFastFallStop += OscFastFallStop;
        Debug.Log("[PlayerControl] Abonné aux événements OSC");
    }

    private void UnsubscribeOsc()
    {
        if (oscReceiver == null) return;
        oscReceiver.OnMoveLeft -= OscMoveLeft;
        oscReceiver.OnMoveRight -= OscMoveRight;
        oscReceiver.OnDashLeft -= OscDashLeft;
        oscReceiver.OnDashRight -= OscDashRight;
        oscReceiver.OnRotateLeft -= OscRotateLeft;
        oscReceiver.OnRotateRight -= OscRotateRight;
        oscReceiver.OnFastFallStart -= OscFastFallStart;
        oscReceiver.OnFastFallStop -= OscFastFallStop;
    }

    // ─── Helpers abonnement Input System ──────────────────────────────────────

    private void EnableAction(InputActionReference r, System.Action<InputAction.CallbackContext> cb)
    {
        if (r == null) return;
        r.action.Enable();
        r.action.performed += cb;
    }

    private void DisableAction(InputActionReference r, System.Action<InputAction.CallbackContext> cb)
    {
        if (r == null) return;
        r.action.performed -= cb;
        r.action.Disable();
    }

    private void SubscribeMove(InputActionReference r,
        System.Action<InputAction.CallbackContext> performed,
        System.Action<InputAction.CallbackContext> canceled)
    {
        if (r == null) return;
        r.action.Enable();
        r.action.performed += performed;
        r.action.canceled += canceled;
    }

    private void UnsubscribeMove(InputActionReference r,
        System.Action<InputAction.CallbackContext> performed,
        System.Action<InputAction.CallbackContext> canceled)
    {
        if (r == null) return;
        r.action.performed -= performed;
        r.action.canceled -= canceled;
        r.action.Disable();
    }

    // ─── Cooldowns ────────────────────────────────────────────────────────────

    private bool CanMove()
    {
        if (Time.time - lastMoveTime < moveCooldown) return false;
        lastMoveTime = Time.time;
        return true;
    }

    private bool CanDash()
    {
        if (Time.time - lastDashTime < dashCooldown) return false;
        lastDashTime = Time.time;
        return true;
    }

    // ─── Déplacement ──────────────────────────────────────────────────────────

    private void MoveStep(float direction)
    {
        Vector3 p = transform.position;
        transform.position = new Vector3(p.x + direction * stepSize, p.y, p.z);
        Physics2D.SyncTransforms();
    }

    private void DashStep(float direction)
    {
        Vector3 p = transform.position;
        transform.position = new Vector3(p.x + direction * dashSize, p.y, p.z);
        Physics2D.SyncTransforms();
    }

    private void StartRepeatMove(float direction)
    {
        if (direction < 0)
        {
            if (moveLeftCoroutine != null) StopCoroutine(moveLeftCoroutine);
            moveLeftCoroutine = StartCoroutine(RepeatMove(direction));
        }
        else
        {
            if (moveRightCoroutine != null) StopCoroutine(moveRightCoroutine);
            moveRightCoroutine = StartCoroutine(RepeatMove(direction));
        }
    }

    private IEnumerator RepeatMove(float direction)
    {
        if (CanMove()) MoveStep(direction);
        yield return new WaitForSeconds(moveRepeatDelay);
        while (true)
        {
            if (CanMove()) MoveStep(direction);
            yield return new WaitForSeconds(moveRepeatInterval);
        }
    }

    // ─── Input callbacks Input System ─────────────────────────────────────────

    private void OnMoveLeftPerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log("[INPUT][Clavier] MoveLeft performed");
        if (moveLeftCoroutine != null) StopCoroutine(moveLeftCoroutine);
        moveLeftCoroutine = StartCoroutine(RepeatMove(-1f));
    }
    private void OnMoveLeftCanceled(InputAction.CallbackContext ctx)
    {
        Debug.Log("[INPUT][Clavier] MoveLeft canceled");
        if (moveLeftCoroutine != null) { StopCoroutine(moveLeftCoroutine); moveLeftCoroutine = null; }
    }
    private void OnMoveRightPerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log("[INPUT][Clavier] MoveRight performed");
        if (moveRightCoroutine != null) StopCoroutine(moveRightCoroutine);
        moveRightCoroutine = StartCoroutine(RepeatMove(1f));
    }
    private void OnMoveRightCanceled(InputAction.CallbackContext ctx)
    {
        Debug.Log("[INPUT][Clavier] MoveRight canceled");
        if (moveRightCoroutine != null) { StopCoroutine(moveRightCoroutine); moveRightCoroutine = null; }
    }

    private void OnDashLeft(InputAction.CallbackContext ctx)
    {
        Debug.Log("[INPUT][Clavier] DashLeft");
        if (CanDash()) DashStep(-1f);
    }
    private void OnDashRight(InputAction.CallbackContext ctx)
    {
        Debug.Log("[INPUT][Clavier] DashRight");
        if (CanDash()) DashStep(1f);
    }

    private void OnRotateLeft(InputAction.CallbackContext ctx)
    {
        Debug.Log("[INPUT][Clavier] RotateLeft");
        targetRotationZ += STEP_ROTATION;
    }
    private void OnRotateRight(InputAction.CallbackContext ctx)
    {
        Debug.Log("[INPUT][Clavier] RotateRight");
        targetRotationZ -= STEP_ROTATION;
    }

    private void OnFastFallStarted(InputAction.CallbackContext ctx)
    {
        Debug.Log("[INPUT][Clavier] FastFall START");
        isFastFalling = true;
    }
    private void OnFastFallStopped(InputAction.CallbackContext ctx)
    {
        Debug.Log("[INPUT][Clavier] FastFall STOP");
        isFastFalling = false;
    }

    // ─── Input callbacks OSC ──────────────────────────────────────────────────

    private void OscMoveLeft() { Debug.Log("[INPUT][OSC] MoveLeft"); StartRepeatMove(-1f); }
    private void OscMoveRight() { Debug.Log("[INPUT][OSC] MoveRight"); StartRepeatMove(1f); }
    private void OscDashLeft() { Debug.Log("[INPUT][OSC] DashLeft"); if (CanDash()) DashStep(-1f); }
    private void OscDashRight() { Debug.Log("[INPUT][OSC] DashRight"); if (CanDash()) DashStep(1f); }
    private void OscRotateLeft() { Debug.Log("[INPUT][OSC] RotateLeft"); targetRotationZ += STEP_ROTATION; }
    private void OscRotateRight() { Debug.Log("[INPUT][OSC] RotateRight"); targetRotationZ -= STEP_ROTATION; }
    private void OscFastFallStart() { Debug.Log($"[INPUT][OSC] FastFall START — {fallSpeed} → {fallSpeed * fastFallMultiplier} u/s"); isFastFalling = true; }
    private void OscFastFallStop() { Debug.Log("[INPUT][OSC] FastFall STOP"); isFastFalling = false; }

    // ─── Mouvement physique ───────────────────────────────────────────────────

    private void Update()
    {
        if (currentPiece != null)
        {
            float newZ = Mathf.LerpAngle(
                currentPiece.transform.eulerAngles.z,
                targetRotationZ,
                1f - Mathf.Exp(-ROTATE_SNAP_SPEED * Time.deltaTime)
            );
            currentPiece.transform.rotation = Quaternion.Euler(0f, 0f, newZ);
        }
    }

    private void FixedUpdate()
    {
        ApplyFall();
        SyncPieceVelocity();
        previousPosition = transform.position;
    }

    private void ApplyFall()
    {
        if (fallSpeed == 0f) return;
        float speed = isFastFalling ? fallSpeed * fastFallMultiplier : fallSpeed;
        transform.position += Vector3.down * speed * Time.fixedDeltaTime;
    }

    private void SyncPieceVelocity()
    {
        if (currentPieceRb == null) return;
        float velocityY = (transform.position.y - previousPosition.y) / Time.fixedDeltaTime;
        currentPieceRb.linearVelocity = new Vector2(0f, velocityY);
    }

    // ─── Raycast spawn ────────────────────────────────────────────────────────

    private float GetSpawnHeight()
    {
        float originX = transform.position.x;
        float originY = transform.position.y + raycastDistance * 0.5f;
        float highestHit = float.MinValue;
        int piecesMask = LayerMask.GetMask("Pieces");

        for (float offsetX = -raycastSpread; offsetX <= raycastSpread; offsetX += raycastStep)
        {
            Vector2 origin = new Vector2(originX + offsetX, originY);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, raycastDistance, piecesMask);
            if (hit.collider != null && hit.point.y > highestHit)
                highestHit = hit.point.y;
        }

        if (highestHit == float.MinValue)
        {
            Debug.LogWarning("GetSpawnHeight: aucun hit, fallback sur defaultHeight.");
            highestHit = defaultHeight;
        }

        return highestHit + spawnHeightOffset;
    }

    // ─── Pièces ───────────────────────────────────────────────────────────────

    private void CreatePiece()
    {
        if (piecePrefabs == null || piecePrefabs.Length == 0)
            piecePrefabs = Resources.LoadAll<GameObject>(resourcesFolder);

        if (piecePrefabs == null || piecePrefabs.Length == 0)
        {
            Debug.LogWarning($"CreatePiece: aucun prefab trouvé dans Resources/{resourcesFolder}.");
            return;
        }

        float spawnY = GetSpawnHeight();
        SpawnY = spawnY;

        Vector3 spawnPos = new Vector3(transform.position.x, spawnY, transform.position.z);
        transform.position = spawnPos;
        previousPosition = spawnPos;

        GameObject prefab = piecePrefabs[Random.Range(0, piecePrefabs.Length)];
        if (prefab == null) return;

        GameObject instance = Instantiate(prefab, spawnParent);
        instance.name = prefab.name;

        Rigidbody2D rb2d = instance.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.bodyType = RigidbodyType2D.Dynamic;
            rb2d.gravityScale = 0f;
        }

        PieceCollisionListener listener = instance.AddComponent<PieceCollisionListener>();
        listener.AssignParent(this);

        currentPiece = instance;
        currentPieceRb = rb2d;
        targetRotationZ = instance.transform.eulerAngles.z;
    }

    public void OnPieceCollision(GameObject piece, Collision2D collision)
    {
        if (piece == null) return;

        currentPiece = null;
        currentPieceRb = null;

        piece.transform.parent = null;

        Rigidbody2D rb2d = piece.GetComponent<Rigidbody2D>();
        if (rb2d != null)
            rb2d.gravityScale = 1f;

        CreatePiece();
    }
}