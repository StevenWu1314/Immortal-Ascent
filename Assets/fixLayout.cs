using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class fixLayout : MonoBehaviour
{
    private void OnEnable()
    {
        // Force the layout to update at the end of the frame
        StartCoroutine(ExecuteLayoutLoop());
    }

    private System.Collections.IEnumerator ExecuteLayoutLoop()
    {
        // Wait for the end of the frame so all text components and children initialize
        yield return new WaitForEndOfFrame();
        
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Rebuild the layout for this object and its children
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
}
