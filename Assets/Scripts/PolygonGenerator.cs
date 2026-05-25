using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
[ExecuteAlways]
public class PolygonGenerator : MonoBehaviour
{
    #region setup
    //mesh properties
    Mesh mesh;
    [SerializeField] private Vector2[] polygonPoints;
    [SerializeField] private int[] polygonTriangles;
    private PolygonCollider2D polyCollider;
 
    
    [SerializeField] private int polygonSides;
    [SerializeField] private float polygonRadius;
    [SerializeField] private float rotation;

    void Awake()
    {
        polyCollider = GetComponentInChildren<PolygonCollider2D>();
        mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh = mesh;
        DrawFilled(polygonSides,polygonRadius);
    }
    
    #endregion
 
    private void DrawFilled(int sides, float radius)
    {
        polygonPoints = GetCircumferencePoints(sides,radius).ToArray();
        polygonTriangles = DrawFilledTriangles(polygonPoints);
        mesh.Clear();
        List<Vector3> meshPoints = new List<Vector3>();
        foreach (Vector2 point in polygonPoints)
        {
            meshPoints.Add(new Vector3(point.x, point.y, 0));
        }
        mesh.vertices = meshPoints.ToArray();
        mesh.triangles = polygonTriangles;
        polyCollider.pathCount = 1;
        polyCollider.SetPath(0, polygonPoints);
    }
    
    private List<Vector2> GetCircumferencePoints(int sides, float radius)   
    {
        List<Vector2> points = new List<Vector2>();
        float circumferenceProgressPerStep = (float)1/sides;
        float TAU = 2*Mathf.PI;
        float radianProgressPerStep = circumferenceProgressPerStep*TAU;
        float rotationalOffset = TAU * rotation/360f;
        
        for(int i = 0; i<sides; i++)
        {
            float currentRadian = radianProgressPerStep*i + rotationalOffset;
            points.Add(new Vector2(Mathf.Sin(currentRadian)*radius, Mathf.Cos(currentRadian)*radius));
        }
        return points;
    }
    
    private int[] DrawFilledTriangles(Vector2[] points)
    {   
        int triangleAmount = points.Length - 2;
        List<int> newTriangles = new List<int>();
        for(int i = 0; i<triangleAmount; i++)
        {
            newTriangles.Add(0);
            newTriangles.Add(i+2);
            newTriangles.Add(i+1);
        }
        return newTriangles.ToArray();
    }
}
