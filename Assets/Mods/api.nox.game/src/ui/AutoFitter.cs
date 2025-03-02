using api.nox.game;
using UnityEngine;

public class AutoFitter : MonoBehaviour, UpdateLayout
{
    public RectTransform from;
    public RectTransform rect => GetComponent<RectTransform>();
    
    void Start() => UpdateLayout();
    void OnEnable() => UpdateLayout();
    void OnValidate() => UpdateLayout();

    public void UpdateLayout()
    { 
        if (!from || !rect) return;
        rect.sizeDelta = from.sizeDelta;
    }
}
