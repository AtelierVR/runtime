using UnityEngine;

[AddComponentMenu("Debug/FPS Graph")]
public class FPSGraph : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private Vector2 position = new Vector2(10, 10);
    [SerializeField] private Vector2 size = new Vector2(300, 120);
    [SerializeField] private int historySize = 300;
    [SerializeField] private float targetFPS = 60f;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] private Color graphColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color lowColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color textColor = Color.white;

    // Circular buffer — avoids Queue.ToArray() allocations
    private float[] _history;
    private int _historyHead;
    private int _historyCount;

    private float[] _sortedCache;

    private float _avg, _max, _min, _low1, _low01, _currentFPS;
    private float _statTimer;
    private const float StatRefreshInterval = 0.1f;

    private Texture2D _graphTex;
    private Color32[] _pixelBuffer; // reused every rebuild — no per-frame alloc
    private Color32 _bgColor32;
    private Color32 _graphColor32;
    private Color32 _lowColor32;
    private Color32 _targetLineColor32;

    private GUIStyle _labelStyle;
    private bool _initialized;

    private void Awake()
    {
        _history = new float[historySize];
        _sortedCache = new float[historySize];
        CacheColor32s();
        InitTexture();
    }

    private void CacheColor32s()
    {
        _bgColor32 = backgroundColor;
        _graphColor32 = graphColor;
        _lowColor32 = lowColor;
        _targetLineColor32 = new Color(1f, 1f, 0f, 0.4f);
    }

    private void InitTexture()
    {
        if (_graphTex != null) Destroy(_graphTex);
        int w = (int)size.x;
        int h = (int)size.y;
        _graphTex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        _pixelBuffer = new Color32[w * h];
    }

    private void Update()
    {
        _currentFPS = 1f / Time.unscaledDeltaTime;

        // Push into circular buffer
        int writeIdx = (_historyHead + _historyCount) % historySize;
        _history[writeIdx] = _currentFPS;
        if (_historyCount < historySize)
            _historyCount++;
        else
            _historyHead = (_historyHead + 1) % historySize;

        _statTimer += Time.unscaledDeltaTime;
        if (_statTimer >= StatRefreshInterval)
        {
            _statTimer = 0f;
            RefreshStats();
            RebuildGraph(); // ~10 Hz instead of every frame
        }
    }

    private void RefreshStats()
    {
        int count = _historyCount;
        if (count == 0) return;

        // Copy circular buffer in order into sortedCache — zero alloc
        for (int i = 0; i < count; i++)
            _sortedCache[i] = _history[(_historyHead + i) % historySize];

        float sum = 0f;
        float max = float.MinValue;
        float min = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            float v = _sortedCache[i];
            sum += v;
            if (v > max) max = v;
            if (v < min) min = v;
        }

        _avg = sum / count;
        _max = max;
        _min = min;

        System.Array.Sort(_sortedCache, 0, count);
        _low1 = _sortedCache[Mathf.Max(0, Mathf.FloorToInt(count * 0.01f))];
        _low01 = _sortedCache[Mathf.Max(0, Mathf.FloorToInt(count * 0.001f))];
    }

    private void RebuildGraph()
    {
        int w = _graphTex.width;
        int h = _graphTex.height;
        Color32[] pixels = _pixelBuffer;

        // Fill background — reuse cached Color32
        Color32 bg = _bgColor32;
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = bg;

        float scale = _max > 0f ? _max * 1.1f : targetFPS * 1.5f;

        // Target FPS line
        int targetY = Mathf.Clamp(Mathf.RoundToInt((targetFPS / scale) * h), 0, h - 1);
        Color32 lineCol = _targetLineColor32;
        for (int x = 0; x < w; x++)
            pixels[targetY * w + x] = lineCol;

        // Draw bars directly from circular buffer — no ToArray()
        int count = _historyCount;
        Color32 cGraph = _graphColor32;
        Color32 cLow = _lowColor32;
        float halfTarget = targetFPS * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float fps = _history[(_historyHead + i) % historySize];
            float t = (float)i / historySize;
            int x = Mathf.RoundToInt(t * (w - 1));
            int barH = Mathf.Clamp(Mathf.RoundToInt((fps / scale) * h), 0, h - 1);
            Color32 c = fps < halfTarget ? cLow : cGraph;
            for (int y = 0; y <= barH; y++)
                pixels[y * w + x] = c;
        }

        _graphTex.SetPixels32(pixels);
        _graphTex.Apply(false); // no mipmap update needed
    }

    private void OnGUI()
    {
        if (!_initialized)
        {
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = textColor },
                richText = true
            };
            _initialized = true;
        }

        float statW = 120f;
        float x = position.x;
        float y = position.y;

        GUI.color = backgroundColor;
        GUI.DrawTexture(new Rect(x, y, size.x + statW + 20f, size.y + 20f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (_graphTex != null)
            GUI.DrawTexture(new Rect(x + 5f, y + 10f, size.x, size.y), _graphTex);

        float sx = x + size.x + 10f;
        float sy = y + 10f;
        const float lineH = 18f;

        // Direct calls — avoids string[] allocation each OnGUI
        GUI.Label(new Rect(sx, sy,             statW, lineH), $"<b>FPS</b>  {_currentFPS:F0}", _labelStyle);
        GUI.Label(new Rect(sx, sy + lineH,     statW, lineH), $"Avg  {_avg:F1}", _labelStyle);
        GUI.Label(new Rect(sx, sy + lineH * 2, statW, lineH), $"Max  <color=#88ff88>{_max:F1}</color>", _labelStyle);
        GUI.Label(new Rect(sx, sy + lineH * 3, statW, lineH), $"Min  <color=#ff6666>{_min:F1}</color>", _labelStyle);
        GUI.Label(new Rect(sx, sy + lineH * 4, statW, lineH), $"1%   <color=#ffaa44>{_low1:F1}</color>", _labelStyle);
        GUI.Label(new Rect(sx, sy + lineH * 5, statW, lineH), $"0.1% <color=#ff4444>{_low01:F1}</color>", _labelStyle);
    }

    private void OnDestroy()
    {
        if (_graphTex != null) Destroy(_graphTex);
    }
}
