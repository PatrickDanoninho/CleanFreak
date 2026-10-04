using System;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Unity.Collections;

/// <summary>
/// This class should store the currently used cleaning supply.
/// It communicates with the Interactable class to pass the supply information.
/// </summary>
public class CleaningManager : MonoBehaviour
{
    GameManager gameManager;

    public struct SurfacePoint
    {
        public Vector3 localPosition;
        public Vector3 localNormal;
        public Vector2 uv;

        public SurfacePoint(
            Vector3 _localPosition,
            Vector3 _localNormal,
            Vector2 _uv)
        {
            localPosition = _localPosition;
            localNormal = _localNormal;
            uv = _uv;
        }
    }

    public static CleaningManager Instance;

    public enum CleaningSupplyType
    {
        Hand,
        Duster,
        Soap,
        Brush,
        Cloth
    }

    [SerializeField]
    public CleaningSupplyType currentSupply;

    [Header("Dust Painting")]
    [SerializeField] private Texture2D brushTexture;
    [SerializeField] private Shader dustPaintShader;
    [SerializeField] private Vector2 brushSize = new Vector2(0.1f, 0.1f);

    private Material dustPaintMaterial;

    [Header("Cleaning Visuals")]
    [SerializeField] private CleaningVisual dusterVisual;
    [SerializeField] private CleaningVisual soapVisual;
    [SerializeField] private CleaningVisual brushVisual;
    [SerializeField] private CleaningVisual clothVisual;

    private VisualCleaning visualCleaning;

    [Header("Cleaning Slider")]
    [SerializeField] private Slider cleaningSlider;
    [SerializeField] private TextMeshProUGUI cleaningPercentage;
    [SerializeField] private float cleaningProgressTimer;
    const float CleaningProgressUpdateInterval = 0.1f;

    public void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Start()
    {
        gameManager = GameManager.Instance;

        visualCleaning = new VisualCleaning(
            dusterVisual,
            soapVisual,
            brushVisual,
            clothVisual
        );

        gameManager.OnTouchBegan += StartCleaning;
        gameManager.OnTouchMoved += CheckToolOrRotate;
        gameManager.OnTouchEnded += StopCleaning;
        gameManager.OnTouchCanceled += StopCleaning;

        dustPaintMaterial = new Material(dustPaintShader);
    }

    private void OnDestroy()
    {
        if (dustPaintMaterial != null)
            Destroy(dustPaintMaterial);
    }

    public void UpdateCleaningSlider(float percentage)
    {
        cleaningSlider.value = percentage;
        cleaningPercentage.text = $"{Mathf.RoundToInt(percentage * 100)}%";
    }
    public void CalculateCleaningPercentage(RenderTexture texture) 
    {
        if (texture == null)
            return;

        AsyncGPUReadback.Request(texture, 0, TextureFormat.RGBA32,
            request =>
            {
                if (request.hasError)
                {
                    Debug.LogError("GPU readback error.");
                    return;
                }

                NativeArray<Color32> pixels = request.GetData<Color32>();

                float dirtAmountTotal = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    dirtAmountTotal += pixels[i].g / 255f;
                }

                float dirtAmount = dirtAmountTotal / pixels.Length;

                float cleaningPercentage = 1f - dirtAmount;

                UpdateCleaningSlider(cleaningPercentage);
            });
    }

    #region Cleaning Supply

    public void OnDusterSelected()
    {
        currentSupply = CleaningSupplyType.Duster;
    }

    public void OnSoapSelected()
    {
        currentSupply = CleaningSupplyType.Soap;
    }

    public void OnBrushSelected()
    {
        currentSupply = CleaningSupplyType.Brush;
    }

    public void OnClothSelected()
    {
        currentSupply = CleaningSupplyType.Cloth;
    }

    public void OnHandSelected() 
    {
        currentSupply = CleaningSupplyType.Hand;
    }

    #endregion

    #region Cleaning

    public void CheckToolOrRotate(Vector3 touchDelta) 
    {
        if (currentSupply == CleaningSupplyType.Hand)
        {
            // Rotate the object
            RotateInteractable(touchDelta);
        }
        else
        {
            Clean();
        }
    }

    public void RotateInteractable(Vector3 touchDelta)
    {
        if (gameManager.currentInteractable == null)
            return;
        // Rotate the object based on touch movement
        
        gameManager.currentInteractable.RotateInPlace(touchDelta);
    }

    public void StartCleaning()
    {
        if (gameManager.currentInteractable == null)
            return;

    }

    public void StopCleaning()
    {
        visualCleaning.StopVisual(currentSupply);
    }

    public void Clean()
    {
        if (gameManager.currentInteractable == null)
            return;

        if (!TryGetCleaningSurface(out SurfacePoint surfacePoint))
            return;

        PerformCleaning(surfacePoint);
    }

    private bool TryGetCleaningSurface(out SurfacePoint surfacePoint)
    {
        Ray ray = gameManager.GetTouchRay();

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Interactable hitInteractable = hit.collider.GetComponentInParent<Interactable>();

            if (hitInteractable == gameManager.currentInteractable)
            {
                Transform objectTransform = hitInteractable.transform;

                Vector3 localPosition =
                    objectTransform.InverseTransformPoint(hit.point);

                Vector3 localNormal =
                    objectTransform.InverseTransformDirection(hit.normal).normalized;

                surfacePoint = new SurfacePoint(
                    localPosition,
                    localNormal,
                    hit.textureCoord
                );

                return true;
            }
        }

        surfacePoint = default;
        return false;
    }

    #endregion

    private void PerformCleaning(SurfacePoint surfacePoint)
    {
        if (gameManager.currentInteractable == null)
            return;

        switch (gameManager.currentInteractable.currentCleanState)
        {
            case Interactable.CleaningState.Dusty:

                if (currentSupply == CleaningSupplyType.Duster) 
                {
                    CleanDust(surfacePoint);
                    visualCleaning.StartVisual(currentSupply);
                }
                break;

            case Interactable.CleaningState.Stained:

                if (currentSupply == CleaningSupplyType.Soap)
                    ApplySoap(surfacePoint);

                break;

            case Interactable.CleaningState.Soappy:

                if (currentSupply == CleaningSupplyType.Brush)
                    CleanStain(surfacePoint);

                break;

            case Interactable.CleaningState.Shinable:

                if (currentSupply == CleaningSupplyType.Cloth)
                    MakeItShine(surfacePoint);

                break;

            case Interactable.CleaningState.Clean:
                break;
        }
    }

    private void CleanDust(SurfacePoint surfacePoint)
    {
        Interactable interactable = gameManager.currentInteractable;

        if (interactable == null)
            return;

        Collider[] hitColliders = Physics.OverlapSphere(surfacePoint.localPosition, 2f);
        if(hitColliders.Length > 0) 
        {
            MeshCollider meshCol = hitColliders[0].gameObject.GetComponent<MeshCollider>();
            Vector3[] hitPoints = meshCol.sharedMesh.vertices;
            foreach (Vector3 point in hitPoints)
            {
                Debug.DrawRay(point, surfacePoint.localNormal * 0.1f, Color.yellow, 1f);
                
            }
        }

        PaintDust(surfacePoint.uv);

        Vector3 worldPosition =
            interactable.transform.TransformPoint(surfacePoint.localPosition);

        Vector3 worldNormal =
            interactable.transform
                .TransformDirection(surfacePoint.localNormal)
                .normalized;

        Vector3 visualPosition = worldPosition + worldNormal * 1f;

        visualCleaning.MoveVisual(currentSupply, visualPosition);

        Debug.DrawRay(
            worldPosition,
            worldNormal * 0.1f,
            Color.red,
            1f
        );
    }

    private void ApplySoap(SurfacePoint surfacePoint)
    {
    }

    private void CleanStain(SurfacePoint surfacePoint)
    {
    }

    private void MakeItShine(SurfacePoint surfacePoint)
    {
    }

    private void PaintDust(Vector2 uv)
    {
        Interactable interactable = gameManager.currentInteractable;

        if (interactable == null)
            return;

        RenderTexture dustMask = interactable.dustMaskTexture;

        if (dustMask == null)
            return;


        dustPaintMaterial.SetTexture("_BrushTex", brushTexture);

        dustPaintMaterial.SetVector( "_BrushPosition", uv);

        dustPaintMaterial.SetVector( "_BrushSize", brushSize);

        RenderTexture temporary = RenderTexture.GetTemporary(dustMask.descriptor);

        Graphics.Blit(dustMask, temporary, dustPaintMaterial);

        cleaningProgressTimer += Time.deltaTime;
        if (cleaningProgressTimer >= CleaningProgressUpdateInterval)
        {
            cleaningProgressTimer = 0f;
            CalculateCleaningPercentage(temporary);
        }
        
        Graphics.Blit(temporary,dustMask);

        RenderTexture.ReleaseTemporary(temporary);
    }
}