using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class BackgroundMis : MonoBehaviour
{
    [Header("Colours")]
    [SerializeField] private Color colour = Color.white;

    [Header("Layer Settings")]
    [SerializeField] private float noiseScale = 3f;
    [SerializeField] private float cloudSoftness = 1f;
    [SerializeField] private float warpScale = 1f;
    [SerializeField] private float warpStrength = 0.05f;
    [SerializeField] private float warpScale2 = 1.8f;
    [SerializeField] private float warpSpeed = 0.1f;
    [SerializeField] private float fogSpeed = 2f;
    [SerializeField] private float noiseScale2 = 2f;

    [Header("DirectionSettings")]
    [SerializeField] private Vector2 fogScale = new Vector2(2f, 2f);
    [SerializeField] private Vector2 fogDirection = new Vector2(0.02f, 0.01f);
    [SerializeField] private Vector2 warpDirection = new Vector2(0.2f, -0.1f);
    [SerializeField] private Vector2 warpDirection2 = new Vector2(-0.01f, 0.03f);

    MeshRenderer meshRenderer;
    MaterialPropertyBlock block;

    void OnEnable()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        block = new MaterialPropertyBlock();
        Apply();
    }

    void Apply() //Sets up property block for mist shader
    {
        meshRenderer.GetPropertyBlock(block);
        block.SetColor("_FogColour", colour);
        block.SetFloat("_NoiseScale", noiseScale);
        block.SetFloat("_CloudSoftness", cloudSoftness);
        block.SetFloat("_WarpScale", warpScale);
        block.SetFloat("_WarpStrength", warpStrength);
        block.SetFloat("_WarpScale2", warpScale2);
        block.SetFloat("_WarpSpeed", warpSpeed);
        block.SetFloat("_FogSpeed", fogSpeed);
        block.SetFloat("_NoiseScale2", noiseScale2);
        block.SetVector("_FogScale", fogScale);
        block.SetVector("_FogDirection", fogDirection);
        block.SetVector("_WarpDirection", warpDirection);
        block.SetVector("_WarpDirection2", warpDirection2);
        meshRenderer.SetPropertyBlock(block);
    }
}
