using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SplitElement : MonoBehaviour
{
    public uint spanx = 1;
    public uint spany = 1;
    public uint index = 0;

    public uint width = 9;
    public uint height = 9;
    public bool UseParentDimensions = true;

    public uint Width => UseParentDimensions && Container != null ? Container.width : width;
    public uint Height => UseParentDimensions && Container != null ? Container.height : height;

    public SplitContainer Container => GetComponentInParent<SplitContainer>();

    public void OnValidate() => OnValidateFix();
    public void OnValidateFix(bool formparent = false)
    {
        var cwidth = Width;
        var cheight = Height;

        if (spanx > cwidth)
            spanx = cwidth;

        if (spany > cheight)
            spany = cheight;

        if (index >= cheight * cwidth)
            index = cheight * cwidth - 1;

        var rect = GetComponent<RectTransform>();
        var parent = rect.parent.GetComponent<RectTransform>();

        var parentwidth = parent.rect.width;
        var parentheight = parent.rect.height;
        var calculatedwidth = parentwidth / cwidth * spanx;
        var calculatedheight = parentheight / cheight * spany;
        var x = index % cwidth;
        var y = index / cwidth;
        var offsetx = parentwidth / cwidth * x;
        var offsety = parentheight / cheight * y;
        
        rect.pivot = new Vector2(0, 1);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);

        rect.sizeDelta = new Vector2(calculatedwidth, calculatedheight);
        rect.anchoredPosition = new Vector2(offsetx, -offsety);

        if (Container != null && !formparent)
            Container.OnValidateFix(true);
    }

    public void OnEnable() => OnValidateFix();
    public void Start() => OnValidateFix();
}
