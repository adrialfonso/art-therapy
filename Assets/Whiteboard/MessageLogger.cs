using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MessageLogger : MonoBehaviour
{
    public TMP_Text logText;
    public float displayTime = 3f;
    public float fadeDuration = 0.5f;

    private Coroutine currentCoroutine;

    // Static list to hold all instances of MessageLogger
    private static List<MessageLogger> allLoggers = new List<MessageLogger>();

    void Start()
    {
        allLoggers.Add(this);
        SetAlpha(0);
    }

    // This method logs a message to all instances of MessageLogger
    public void Log(string message)
    {
        foreach (var logger in allLoggers)
        {
            logger.InternalLog(message);
        }
    }

    // Internal method to handle logging for this instance
    private void InternalLog(string message)
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        logText.text = message;
        SetAlpha(1);

        currentCoroutine = StartCoroutine(HideAfterTime());
    }

    private IEnumerator HideAfterTime()
    {
        yield return new WaitForSeconds(displayTime);

        float elapsed = 0f;
        Color c = logText.color;

        // Fade out text over fadeDuration
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            c.a = Mathf.Lerp(1, 0, t);
            logText.color = c;

            yield return null;
        }

        SetAlpha(0);
        currentCoroutine = null;
    }

    private void SetAlpha(float value)
    {
        Color c = logText.color;
        c.a = value;
        logText.color = c;
    }
}
