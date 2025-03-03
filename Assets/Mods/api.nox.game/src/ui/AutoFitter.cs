using Nox.CCK.Utils;
using UnityEngine;

public class AutoFitter : MonoBehaviour, IUpdateLayout
{
    public RectTransform from;
    private RectTransform Rect => GetComponent<RectTransform>();
    
    void Start() => UpdateLayout();
    void OnEnable() => UpdateLayout();
    void OnValidate() => UpdateLayout();

    public void UpdateLayout()
    { 
        if (!from || !Rect) return;
        Rect.sizeDelta = from.sizeDelta;
    }
}
