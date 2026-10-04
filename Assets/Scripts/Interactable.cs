using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class Interactable : MonoBehaviour, IInteractable
{
    GameManager gameManager;
    CleaningManager cleaningManager;

    [Header("Identity")]
    [SerializeField] string Name;
    [SerializeField] Vector3 startPosition;
    [SerializeField] Vector3 inRoomSize = new Vector3(1, 1, 1);
    [SerializeField] Vector3 displaySize = new Vector3(9, 9, 9);

    [Header("Textures")]
    Material objectMaterial;
    [SerializeField] private Texture2D dustMaskSource;
    public RenderTexture dustMaskTexture;

    [Header("Validation")]
    public bool isHeld = false;

    private MeshFilter meshFilter;
    private Mesh mesh;
    public Mesh Mesh => mesh;
    private Renderer objectRenderer;

    public enum CleaningState
    {
        Dusty,
        Stained,
        Soappy,
        Shinable,
        Clean
    }
    public CleaningState currentCleanState = CleaningState.Dusty;

    public void Start()
    {
        gameManager = GameManager.Instance;
        cleaningManager = CleaningManager.Instance;

        Name = gameObject.name;

        meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter != null )
            mesh = meshFilter.sharedMesh;

        objectMaterial = gameObject.GetComponent<Renderer>().material;

        gameManager.OnCloseDisplay += OnStopDisplaying;
    }

    public void OnInteract()
    {
        Debug.Log("Interacted with " + gameObject.name);
        //Store the original position and size of the object
        startPosition = gameObject.transform.position;

        //Display object to be cleaned
        gameObject.transform.position = gameManager.displayItemTransform.position;
        gameObject.transform.localScale = displaySize;

        //Check if interactable is inside camera frustum
        objectRenderer = gameObject.GetComponent<Renderer>();
        AreCornersOutside();

        CreateDustMask();
    }
    public void OnDropped()
    {
        Debug.Log("Dropped " + gameObject.name);
    }

    public void OnStopDisplaying()
    {
        //Reset object to original position and size
        gameObject.transform.position = startPosition;
        gameObject.transform.localScale = inRoomSize;
    }

    public void CreateDustMask()
    {
        if (dustMaskSource == null)
            return;

        // Already created — keep the current cleaning progress.
        if (dustMaskTexture != null)
            return;

        if (dustMaskTexture != null)
            dustMaskTexture.Release();

        dustMaskTexture = new RenderTexture(
            dustMaskSource.width,
            dustMaskSource.height,
            0,
            RenderTextureFormat.ARGB32
        )
        {
            name = $"{gameObject.name}_DustMask",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        dustMaskTexture.Create();

        Graphics.Blit(dustMaskSource, dustMaskTexture);

        objectMaterial.SetTexture(
            "_DustMask",
            dustMaskTexture
        );
    }

    public void AreCornersOutside()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        Bounds bounds = objectRenderer.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3[] corners = new Vector3[8]
        {
            new Vector3 (min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, max.y, max.z)
        };

        // We will find the boundaries of our object in Viewport Space (0 to 1)
        float minViewportX = float.MaxValue;
        float maxViewportX = float.MinValue;
        float minViewportY = float.MaxValue;
        float maxViewportY = float.MinValue;

        //If any corner is outside the camera frustum, scale the object down to fit within the frustum
        foreach (Vector3 corner in corners)
        {
            // Convert world corner to viewport space (X: 0 to 1, Y: 0 to 1)
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(corner);

            // If the object is physically behind the camera, handle it safely
            if (viewportPos.z <= 0) continue;

            if (viewportPos.x < minViewportX) minViewportX = viewportPos.x;
            if (viewportPos.x > maxViewportX) maxViewportX = viewportPos.x;
            if (viewportPos.y < minViewportY) minViewportY = viewportPos.y;
            if (viewportPos.y > maxViewportY) maxViewportY = viewportPos.y;
        }

        // Calculate how wide and tall the object appears on the screen (in 0-1 percentage)
        float currentViewportWidth = maxViewportX - minViewportX;
        float currentViewportHeight = maxViewportY - minViewportY;

        // We want the object to fit entirely within the 0.0 to 1.0 screen bounds.
        // If you want a 5% safety margin buffer around the edges, change 1.0f to 0.95f
        float targetViewportWidth = 1.0f;
        float targetViewportHeight = 1.0f;

        // Calculate the limiting scale factor for width and height
        float scaleFactorX = targetViewportWidth / currentViewportWidth;
        float scaleFactorY = targetViewportHeight / currentViewportHeight;

        // We must use the smaller scale factor so it fits both horizontally AND vertically
        float finalScaleFactor = Mathf.Min(scaleFactorX, scaleFactorY);

        // Only scale down if it actually exceeds the screen boundaries
        if (finalScaleFactor < 1.0f)
        {
            // Multiply your current localScale uniformly by the required percentage reduction
            gameObject.transform.localScale *= finalScaleFactor;

            Debug.Log($"Scaled down uniformly by {finalScaleFactor * 100}% to fit frustum.");
        }
    }

    public void RotateInPlace(Vector3 touchDelta) 
    {
        Quaternion horizontalRotation = Quaternion.AngleAxis(-touchDelta.x * 0.05f, Camera.main.transform.up);

        Quaternion verticalRotation = Quaternion.AngleAxis(touchDelta.y * 0.05f, Camera.main.transform.right);

        transform.rotation = horizontalRotation * verticalRotation * transform.rotation;
    }
}
