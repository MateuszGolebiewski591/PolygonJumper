using UnityEngine;

[ExecuteAlways]
public class GroundRayDebug : MonoBehaviour
{
    public PolygonCollider2D col;
    public float rayDistance = 0.2f;
    public LayerMask groundMask;
    public PlayerMovement player;

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

        /*Bounds bounds = col.bounds;

        Vector2 origin = bounds.center;
        Vector2 size = bounds.size;
        Vector2 direction = player.gravity.normalized;

        // Starting cast box
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(origin, size);

        // Direction line
        Vector2 endPosition = origin + direction * rayDistance;

        Gizmos.color = Color.white;
        Gizmos.DrawLine(origin, endPosition);

        // Final box position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(endPosition, size);

        // Actual BoxCast hit
        RaycastHit2D hitb = Physics2D.BoxCast(
            origin,
            size,
            0f,
            direction,
            rayDistance,
            groundMask
        );

        if (hitb.collider != null)
        {
            // Hit point
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(hitb.point, 0.05f);

            // Surface normal
            Gizmos.DrawLine(
                hitb.point,
                hitb.point + hitb.normal * 0.3f
            );
        }

        // Gravity vector from center (for sanity checking)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            origin,
            origin + direction * 0.5f
        );*/
    }
}