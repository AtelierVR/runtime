using UnityEngine;

public class AutoFitter : MonoBehaviour
{
    public RectTransform from;
    public RectTransform rect => GetComponent<RectTransform>();
    
    void Start() => Fit();
    void OnEnable() => Fit();
    void OnValidate() => Fit();

    public void Fit()
    { 
        if (from == null || rect == null) return;
        rect.sizeDelta = from.sizeDelta;
    }
}
