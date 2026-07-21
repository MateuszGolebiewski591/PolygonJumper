using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(BoxCollider2D))]
public class SpikeStripGenerator : MonoBehaviour
{
    [Header("Spike Settings")]
    [SerializeField] private float maxSpikeWidth = 0.5f;
    [SerializeField] private float spikeHeight = 0.5f;
    [SerializeField] private float spikeRimCompensation = 0.5f;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        Initialise();
        Generate();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Initialise();
        Generate();
    }
#endif

    private void Initialise()
    {
        meshFilter = GetComponent<MeshFilter>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Spike Strip";
            meshFilter.sharedMesh = mesh;
        }
    }

    public void Generate()
    {
        float width = boxCollider.size.x*1.05f; //Spike dimensions and number
        float boxBottom = boxCollider.offset.y - boxCollider.size.y*0.5f;
        int spikeCount = Mathf.Max(1, Mathf.CeilToInt(width / maxSpikeWidth));
        float actualSpikeWidth = width / spikeCount;

        List<Vector3> vertices = new(); //Spike information needed to create the mesh
        List<int> triangles = new();
        List<Vector2> uvs = new();

        float startX = -width * 0.5f; //Far left side

        for (int i = 0; i < spikeCount; i++)
        {
            float left = startX + i * actualSpikeWidth - spikeRimCompensation/2; //Bottom left corner (x coordinate)
            float right = left + actualSpikeWidth + spikeRimCompensation; //Bottom right corner
            float centre = (left + right) * 0.5f; //Top middle corner

            int vertexIndex = vertices.Count; //Starting index of currently added trio

            vertices.Add(new Vector3(left, boxBottom)); //Add the trio to the vertices list
            vertices.Add(new Vector3(centre, boxBottom + spikeHeight));
            vertices.Add(new Vector3(right, boxBottom));

            triangles.Add(vertexIndex); //Generate triangle using the new trio
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);

            float uMin = (float)i / spikeCount; //Vertex coordinates within the UV space
            float uMax = (float)(i + 1) / spikeCount;
            float uMid = (uMin + uMax) * 0.5f;

            uvs.Add(new Vector2(0f, 0f)); //Adding coordinates to UV list
            uvs.Add(new Vector2(0.5f, 1f));   
            uvs.Add(new Vector2(1f, 0f));     
        }

        mesh.Clear();

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}