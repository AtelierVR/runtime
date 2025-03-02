using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Profiler : MonoBehaviour
{
    private ushort position = 0;
    public ushort NbFrameSaved = 240;
    private float[] frames = new float[0];
    public float GraphSamples = 100;
    public float GraphMax = 0.05f;
    public float[] GraphMarkers = { 24, 60, 120, 240 };

    public InputActionReference toggleAction;
    public Vector2 Position = new (10, 10);
    public Vector2 Size = new(300, 150);

    void Awake()
    {
        position = 0;
        frames = new float[NbFrameSaved];
        for (int i = 0; i < frames.Length; i++)
            frames[i] = -1;

        toggleAction.action.performed += _ => enabled = !enabled;
    }

    void OnValidate()
    {
        position = 0;
        if (frames.Length != NbFrameSaved)
        {
            frames = new float[NbFrameSaved];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = -1;
        }
    }

    void GetDatas(out float min, out float max, out float avg, out float crt)
    {
        min = float.MaxValue;
        max = float.MinValue;
        avg = 0;
        crt = 0;
        if (frames.Length == 0)
            return;
        crt = frames[position];
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] < min)
                min = frames[i];
            if (frames[i] > max)
                max = frames[i];
            avg += frames[i];
        }
        avg /= frames.Length;
    }

    void Update()
    {
        if (!enabled || frames.Length == 0)
            return;

        frames[position] = Time.deltaTime;
        position = (ushort)((position + 1) % frames.Length);
    }

    private float[] floats = new float[4];
    private DateTime _lastLatencyRequest = DateTime.MinValue;
    void OnGUI()
    {
        if (!enabled || frames.Length == 0)
            return;

        if (_lastLatencyRequest.AddSeconds(1) < DateTime.Now)
        {
            _lastLatencyRequest = DateTime.Now;
            GetDatas(out floats[0], out floats[1], out floats[2], out floats[3]);
        }

        GUI.DrawTexture(
            new Rect(Position.x - 1, Position.y - 1, Size.x + 1, Size.y + 1),
            Texture2D.blackTexture
        );


        if (GraphSamples <= 0)
            GraphSamples = 1;
        if (GraphMax <= float.Epsilon)
            GraphMax = float.Epsilon;

        var width = Size.x / GraphSamples;
        var minvalue = 1 / GraphSamples;
        
        var refsample = GraphSamples > frames.Length ? frames.Length : GraphSamples;

        if (refsample > Size.x)
            refsample = Size.x;

        for (float i = 0; i < 1 - minvalue; i += minvalue)
        {
            var value = frames[(position + (ushort)(i * frames.Length)) % frames.Length];
            if (value < 0)
                continue;
            var x = Position.x + Size.x - ((i + minvalue) * Size.x);
            var y = Position.y + Size.y - (value / GraphMax * Size.y);
            var y2 = Position.y + Size.y;
            GUI.DrawTexture(new Rect(x, y, width, y2 - y), Texture2D.whiteTexture);
        }


        for (int i = 0; i < GraphMarkers.Length; i++)
        {
            var sample = GraphMarkers[i];
            var y = Position.y + Size.y - (sample / GraphMax * Size.y);
            if (y < Position.y || y > Position.y + Size.y)
                continue;
            GUI.DrawTexture(new Rect(Position.x, y, Size.x, 1), Texture2D.whiteTexture);
            GUI.Label(new Rect(Position.x + 10, y, Size.x - 20, 20), sample.ToString());
        }


        GUI.Label(
            new Rect(Position.x, Position.y, Size.x, 20),
            "Profiler",
            new GUIStyle { alignment = TextAnchor.MiddleCenter }
        );

        GUI.Label(
            new Rect(Position.x, Position.y + 20, Size.x, 20),
            $"Min: {floats[0]:0.000} Max: {floats[1]:0.000} Avg: {floats[2]:0.000} Crt: {floats[3]:0.000}",
            new GUIStyle { alignment = TextAnchor.MiddleCenter }
        );

    }
}
