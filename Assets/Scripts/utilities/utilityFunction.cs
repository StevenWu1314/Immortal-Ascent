using UnityEngine;
using System.Collections;
using TMPro;
using Unity.VisualScripting;


public static class utilityFunction
{
    public static TextMesh createWorldText(string text, Transform parent = null, Vector3 localposition = default(Vector3), int fontSize = 20, Color color = default(Color), TextAnchor textAnchor = TextAnchor.MiddleCenter, TextAlignment textAlignment = TextAlignment.Center, int sortingOrder = 0)
    {
        if(color == null) color = Color.white;
        return createWorldText(parent, text, localposition, fontSize, color, textAnchor, textAlignment, sortingOrder);
    }
    public static TextMesh createWorldText(Transform parent, string text, Vector3 localposition, int fontSize, Color color, TextAnchor textAnchor, TextAlignment textAlignment, int sortingOrder)
    {
        GameObject gameObject = new GameObject("World_Text", typeof(TextMesh));
        gameObject.tag = "worldtexts";
        Transform transform = gameObject.transform;
        transform.SetParent(parent, false);
        transform.localPosition = localposition;
        TextMesh textMesh = gameObject.GetComponent<TextMesh>();
        textMesh.anchor = textAnchor;
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = fontSize;
        textMesh.alignment = textAlignment;
        textMesh.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;  
        return textMesh;
    }


    
}