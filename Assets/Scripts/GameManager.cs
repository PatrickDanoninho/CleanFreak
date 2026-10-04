using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    /// <summary>
    /// Determines what the player can interact with.
    /// </summary>
    private enum TouchState
    {
        FortState,
        RoomState,
        DisplayState
    }

    private TouchState currentTouchState = TouchState.RoomState;

    private Touch touch;

    [SerializeField] public Interactable currentInteractable;

    private Rigidbody heldRB;
    private MeshCollider heldCollider;

    private Vector3 currentTouchWorldPosition;
    private float holdTime;
    private Plane movementPlane;

    [SerializeField] private float holdThreshold = 0.1f;    

    public Action OnTap;
    public Action OnDrag;
    public Action OnReleaseHold;
    public Action OnCloseDisplay;

    public Action OnTouchBegan;
    public Action<Vector3> OnTouchMoved;
    public Action OnTouchStationary;
    public Action OnTouchEnded;
    public Action OnTouchCanceled;

    public Transform displayItemTransform;

    #region Unity

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        movementPlane = new Plane(Vector3.up, new Vector3(0, 1, 0));
    }

    private void Update()
    {
        if (Input.touchCount == 0)
            return;

        touch = Input.GetTouch(0);

        HandleTouch();
    }

    #endregion


    #region Touch Handling

    private void HandleTouch()
    {
        switch (currentTouchState)
        {
            case TouchState.RoomState:
                HandleRoomTouch();
                break;

            case TouchState.DisplayState:
                HandleDisplayTouch();
                break;

            case TouchState.FortState:
                // Ignore touch for now.
                break;
        }
    }

    #endregion


    #region Room State

    private void HandleRoomTouch()
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                TouchBegan();
                break;

            case TouchPhase.Moved:
                TouchMoved();
                break;

            case TouchPhase.Stationary:
                TouchStationary();
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                TouchEnded();
                break;
        }
    }

    private void TouchBegan()
    {
        Ray ray = GetTouchRay();

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        currentInteractable = hit.collider.GetComponent<Interactable>();

        if (currentInteractable == null)
            return;

        heldRB = hit.collider.GetComponent<Rigidbody>();

        heldCollider = hit.collider.GetComponent<MeshCollider>();

        holdTime = 0f;

        GetTouchWorldPosition(out currentTouchWorldPosition);
    }

    private void TouchMoved()
    {
        if (currentInteractable == null)
            return;

        UpdateHold();

        if (currentInteractable.isHeld)
        {
            Drag();
        }
    }

    private void TouchStationary()
    {
        if (currentInteractable == null)
            return;

        UpdateHold();
    }

    private void TouchEnded()
    {
        if (currentInteractable == null)
            return;

        if (currentInteractable.isHeld)
        {
            EndHold();
        }
        else
        {
            HandleTap();
            return;
        }

        ClearCurrentInteractable();
    }

    #endregion


    #region Hold / Drag

    private void UpdateHold()
    {
        holdTime += Time.deltaTime;

        if (!currentInteractable.isHeld &&
            holdTime >= holdThreshold)
        {
            BeginHold();
        }
    }

    private void BeginHold()
    {
        currentInteractable.isHeld = true;

        heldRB.isKinematic = true;
        heldCollider.convex = false;
        heldRB.useGravity = false;
    }

    private void EndHold()
    {
        heldRB.isKinematic = false;
        heldCollider.convex = true;
        heldRB.useGravity = true;

        currentInteractable.isHeld = false;

        OnReleaseHold?.Invoke();
    }

    private void Drag()
    {
        if (!GetTouchWorldPosition(out Vector3 worldPosition))
            return;

        currentTouchWorldPosition = worldPosition;

        currentInteractable.transform.position =
            currentTouchWorldPosition;

        OnDrag?.Invoke();
    }

    #endregion


    #region Tap

    private void HandleTap()
    {
        OnTap?.Invoke();

        // The tap has now selected/interacted with the object.
        currentTouchState = TouchState.DisplayState;

        heldRB.isKinematic = true;
        heldCollider.convex = false;
        heldRB.useGravity = false;

        currentInteractable.OnInteract();
    }

    #endregion


    #region Display State

    public void HandleDisplayTouch()
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                OnTouchBegan?.Invoke();
                break;

            case TouchPhase.Moved:
                OnTouchMoved?.Invoke(touch.deltaPosition);
                break;

            case TouchPhase.Stationary:
                OnTouchStationary?.Invoke();
                break;

            case TouchPhase.Ended:
                OnTouchEnded?.Invoke();
                break;

            case TouchPhase.Canceled:
                OnTouchCanceled?.Invoke();
                break;
        }
    }
    #endregion

    #region Touch Utilities

    public Ray GetTouchRay()
    {
        return Camera.main.ScreenPointToRay(touch.position);
    }

    private bool GetTouchWorldPosition(out Vector3 worldPosition)
    {
        Ray ray = GetTouchRay();

        if (movementPlane.Raycast(
                ray,
                out float distance))
        {
            worldPosition = ray.GetPoint(distance);
            return true;
        }

        worldPosition = Vector3.zero;
        return false;
    }

    private void ClearCurrentInteractable()
    {
        currentInteractable = null;
        heldRB = null;
        heldCollider = null;
        holdTime = 0f;
    }

    #endregion


    #region Menu Buttons

    public void OnPressCloseDisplay()
    {
        OnCloseDisplay?.Invoke();

        currentTouchState = TouchState.RoomState;

        ClearCurrentInteractable();
    }

    #endregion
}