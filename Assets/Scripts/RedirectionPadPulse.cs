using UnityEngine;
using System.Collections;

public class RedirectionPadPulse : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] fins;
    private MaterialPropertyBlock propertyBlock;

    [SerializeField] private AnimationCurve pulseSizeCurve;
    [SerializeField] private AnimationCurve pulseOpacityCurve;
    [SerializeField] private float pulseLength = 0.2f;
    [SerializeField] private float startingOpacity = 0.4f;
    [SerializeField] private float pulseStrength = 1f;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetFloat("_AlphaValue", 0f);
        foreach (SpriteRenderer fin in fins)
        {
            fin.SetPropertyBlock(propertyBlock);
            fin.enabled = false;
        }
    }

    public void Pulse()
    {
        StartCoroutine(PlayPulse());
    }

    private IEnumerator PlayPulse()
    {
        float timeElapsed = 0f;
        foreach (SpriteRenderer fin in fins) {
            fin.transform.localScale = Vector3.one;
            fin.enabled = true;
        }
        while (timeElapsed < pulseLength)
        {
            timeElapsed += Time.deltaTime;
            float newSize = pulseStrength * pulseSizeCurve.Evaluate(timeElapsed/pulseLength);
            float newOpacity = pulseOpacityCurve.Evaluate(timeElapsed/pulseLength);
            propertyBlock.SetFloat("_AlphaValue",newOpacity*startingOpacity);
            foreach (SpriteRenderer fin in fins) {
                fin.transform.localScale = new Vector3(newSize, newSize, newSize);
                fin.SetPropertyBlock(propertyBlock);
            }
            yield return null;
        }
        foreach (SpriteRenderer fin in fins) fin.enabled = false;
    }
}
