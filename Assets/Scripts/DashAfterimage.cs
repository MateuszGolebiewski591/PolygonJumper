using UnityEngine;
using System.Collections;

public class DashAfterimage : MonoBehaviour
{
    [SerializeField] private SpriteRenderer image1;
    [SerializeField] private SpriteRenderer image2;
    [SerializeField] private SpriteRenderer image3;
    [SerializeField] private PlayerMovement player;

    public void StartDashEffect(float dashTime)
    {
        StartCoroutine(DashEffect(dashTime));
    }

    public void StartRedirectionEffect(float time)
    {
        StartCoroutine(RedirectionDashEffect(time));
    }

    private IEnumerator RedirectionDashEffect(float dashTime)
    {
        float timeElapsed = 0f;
        while (timeElapsed < dashTime/4f) {
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        StartCoroutine(LeaveImage(image1, dashTime));
        while (timeElapsed < dashTime/2f) {
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        StartCoroutine(LeaveImage(image2, dashTime));
        while (timeElapsed < 3*dashTime/4f) {
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        StartCoroutine(LeaveImage(image3, dashTime));
    }

    private IEnumerator DashEffect(float dashTime)
    {
        float timeElapsed = 0f;
        StartCoroutine(LeaveImage(image1, dashTime));
        while (timeElapsed < dashTime/3f) {
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        StartCoroutine(LeaveImage(image2, dashTime));
        while (timeElapsed < 2*dashTime/3f) {
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        StartCoroutine(LeaveImage(image3, dashTime));
    }

    private IEnumerator LeaveImage(SpriteRenderer image, float dashTime)
    {
        Vector2 position = player.transform.position +  new Vector3(0, 0.127f, 0);
        Quaternion rotation = player.transform.rotation;
        Color colour = image.color;
        colour.a = 1f;
        image.enabled = true;
        image.color = colour;
        float timeElapsed = 0f;
        while (timeElapsed < dashTime)
        {
            timeElapsed += Time.deltaTime;
            image.transform.position = position;
            image.transform.rotation = rotation;
            colour.a = 1f-timeElapsed/dashTime;
            image.color = colour;
            yield return null;
        }
        image.enabled = false;
        image.transform.position = player.transform.position;
        image.transform.rotation = player.transform.rotation;
    }
}
