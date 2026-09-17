using UnityEngine;

public class PolygonAppearance : MonoBehaviour
{
    [Header("Shader Colours")]
    public Color borderColour = Color.green;
    public Color outerColour = Color.red;
    public Color innerColour = Color.blue;
    public Color centreColour = Color.white;
    public float preset = 0;

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        ApplyColours();
    }

    public void ApplyColours() //Colour properties
    {
        if (meshRenderer == null)
            return;

        meshRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor("_Outer", borderColour);
        propertyBlock.SetColor("_Inner1", outerColour);
        propertyBlock.SetColor("_Inner2", innerColour);
        propertyBlock.SetColor("_Inner3", centreColour);
        propertyBlock.SetFloat("_Preset", preset);

        meshRenderer.SetPropertyBlock(propertyBlock);
    }
}