using UnityEngine;

[ExecuteAlways]
public class GroundRayDebug : MonoBehaviour
{
    public PolygonCollider2D col;
    public float rayDistance = 0.2f;
    public LayerMask groundMask;

    void OnDrawGizmos()
    {
        if (col == null) return;

        Vector2[] colliderPoints = col.points;

        for (int i = 0; i < colliderPoints.Length; i++)
        {
            Vector2 vA = transform.TransformPoint(colliderPoints[i]);
            Vector2 vB = transform.TransformPoint(colliderPoints[(i + 1) % colliderPoints.Length]);

            Vector2 midPoint = (vA + vB)/2f;
            Vector2 surfaceVector = vB - vA;

            Vector2 normal = new Vector2(-surfaceVector.y, surfaceVector.x).normalized;
            if (Vector2.Distance(transform.position, midPoint+normal) > Vector2.Distance(transform.position, midPoint)) normal = -normal;


            Vector2 end = midPoint + normal * rayDistance;

            // Default color (no hit)
            Gizmos.color = Color.red;

            RaycastHit2D hit = Physics2D.Raycast(midPoint, normal, rayDistance, groundMask);

            if (hit)
            {
                Gizmos.color = Color.green;

                // Draw hit point
                Gizmos.DrawSphere(hit.point, 0.02f);
            }

            // Draw ray
            Gizmos.DrawLine(midPoint, end);

            // Draw midpoint
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(midPoint, 0.02f);
        }
    }
}