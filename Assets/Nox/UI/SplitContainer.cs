using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SplitContainer : MonoBehaviour
{
    public uint width = 9;
    public uint height = 9;

    public void OnValidate() => OnValidate();
    public void OnValidateFix(bool formelement = false)
    {
        if (!formelement)
            foreach (var element in GetComponentsInChildren<SplitElement>())
                element.OnValidateFix(true);
    }

    public void OnEnable() => OnValidateFix();
    public void Start() => OnValidateFix();
}
