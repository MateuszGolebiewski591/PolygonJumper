using UnityEngine;
using System.Collections;

public class PolygonPulseAppearance : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;
    public Color color = Color.white;
    [SerializeField] private AnimationCurve pulseSizeCurve;
    [SerializeField] private AnimationCurve pulseOpacityCurve;
    [SerializeField] private float pulseLength = 0.2f;
    [SerializeField] private float startingOpacity = 0.4f;
    [SerializeField] private float pulseStrength = 1f;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        ApplyColours();
        meshRenderer.enabled = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        ApplyColours();
    }
#endif

    public void ApplyColours()
    {
        if (meshRenderer == null)
            return;

        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_CustomColor", color);
        propertyBlock.SetFloat("_CustomOpacity", startingOpacity);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    public void Pulse()
    {
        StartCoroutine(PlayPulse());
        AudioManager.Instance.PlayPulseSound();
    }

    public IEnumerator PlayPulse()
    {
        float timeElapsed = 0f;
        Color colour = color;
        transform.localScale = Vector3.one;
        meshRenderer.enabled = true;
        while (timeElapsed < pulseLength)
        {
            timeElapsed += Time.deltaTime;
            float newSize = pulseStrength * pulseSizeCurve.Evaluate(timeElapsed/pulseLength);
            float newOpacity = pulseOpacityCurve.Evaluate(timeElapsed/pulseLength);
            transform.localScale = new Vector3(newSize, newSize, newSize);
            propertyBlock.SetFloat("_CustomOpacity",newOpacity*startingOpacity);
            meshRenderer.SetPropertyBlock(propertyBlock);
            yield return null;
        }
        meshRenderer.enabled = false;
    }
}