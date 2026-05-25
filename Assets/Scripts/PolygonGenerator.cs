using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
 [ExecuteAlways]
public class PolygonGenerator : MonoBehaviour
{
    #region setup
    //mesh properties
    Mesh mesh;
    public Vector3[] polygonPoints;
    public int[] polygonTriangles;
 
    
    public int polygonSides;
    public float polygonRadius;
    
    void Start()
    {
        mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh = mesh;
        DrawFilled(polygonSides,polygonRadius);
    }
    #endregion
 
    void DrawFilled(int sides, float radius)
    {
        polygonPoints = GetCircumferencePoints(sides,radius).ToArray();
        polygonTriangles = DrawFilledTriangles(polygonPoints);
        mesh.Clear();
        mesh.vertices = polygonPoints;
        mesh.triangles = polygonTriangles;
    }
    
    List<Vector3> GetCircumferencePoints(int sides, float radius)   
    {
        List<Vector3> points = new List<Vector3>();
        float circumferenceProgressPerStep = (float)1/sides;
        float TAU = 2*Mathf.PI;
        float radianProgressPerStep = circumferenceProgressPerStep*TAU;
        
        for(int i = 0; i<sides; i++)
        {
            float currentRadian = radianProgressPerStep*i;
            points.Add(new Vector3(Mathf.Sin(currentRadian)*radius, Mathf.Cos(currentRadian)*radius,0));
        }
        return points;
    }
    
    int[] DrawFilledTriangles(Vector3[] points)
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
