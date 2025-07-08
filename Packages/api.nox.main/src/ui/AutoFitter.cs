using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

public class AutoFitter : MonoBehaviour
{
    public RectTransform from;
    private RectTransform Rect => GetComponent<RectTransform>();

    void Start() => UpdateLayout();
    void OnEnable() => UpdateLayout();
    void OnValidate() => UpdateLayout();

    private Vector2 _lastSize = Vector2.zero;

    void Update()
    {
        if (from.rect.size == _lastSize) return;
        _lastSize = from.rect.size;
        UpdateLayout();
    }

    public void UpdateLayout()
    {
        if (!from || !Rect) return;

        var fromSize = from.rect.size;
        var rectSize = Rect.rect.size;

        // change scale to fit inside from
        var scaleX = fromSize.x / rectSize.x;
        var scaleY = fromSize.y / rectSize.y;
        var scale = Mathf.Min(scaleX, scaleY);
        Rect.localScale = new Vector3(scale, scale, 1);
    }
}