using TMPro;
using UnityEngine;
using System.Collections;

public class MessageLogger : MonoBehaviour
{
    public TMP_Text logText;
    public float displayTime = 2f;

    private Coroutine currentCoroutine;

    void Start()
    {
        StartCoroutine(HideAfterTime());
    }

    public void Log(string message)
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        logText.text = message;
        currentCoroutine = StartCoroutine(HideAfterTime());
    }

    private IEnumerator HideAfterTime()
    {
        yield return new WaitForSeconds(displayTime);
        logText.text = "";
        currentCoroutine = null;
    }
}
