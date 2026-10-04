using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
    [SerializeField] private float referenceAspect = 16f / 9f;
    [SerializeField] private float referenceSize = 5f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        float aspect = (float)Screen.width / Screen.height;

        cam.orthographicSize =
            referenceSize * (referenceAspect / aspect);
    }
}
