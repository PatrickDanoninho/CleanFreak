using UnityEngine;

public class CleaningDebugVisualizer : MonoBehaviour
{
    public Vector3 localPosition;
    public float radius = 0.05f;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(
            transform.TransformPoint(localPosition),
            radius
        );
    }
}
