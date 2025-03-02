using System;
using Nox.CCK.Language;
using UnityEngine;


namespace Nox.CCK.Utils
{
    public class FpsPreview : MonoBehaviour
    {
        public TextLanguage text;

        void Start()
        {
            _fps = new float[samplesCount];
            for(var i = 0; i < _fps.Length; i++)
                _fps[i] = -1;
            _fpsIndex = 0;
            if (text == null)
                text = GetComponent<TextLanguage>();
        }
        
        uint samplesCount = byte.MaxValue;
        private float[] _fps;
        uint _fpsIndex;
        
        float CalculateFps()
        {
            if (_fpsIndex == 0) 
                return float.NaN;
            float sum = 0;
            for (var i = 0; i < _fpsIndex; i++)
                if (_fps[i] > 0) sum += _fps[i];
            if (sum == 0) 
                return float.NaN;
            return sum / _fpsIndex;
        }
        
        float MinFps()
        {
            if (_fpsIndex == 0) 
                return float.NaN;
            var min = float.MaxValue;
            for (var i = 0; i < _fpsIndex; i++)
                if (_fps[i] > 0 && _fps[i] < min) min = _fps[i];
            return Mathf.Approximately(min, float.MaxValue) ? float.NaN : min;
        }
        
        float MaxFps()
        {
            if (_fpsIndex == 0) 
                return float.NaN;
            var max = float.MinValue;
            for (var i = 0; i < _fpsIndex; i++)
                if (_fps[i] > 0 && _fps[i] > max) max = _fps[i];
            return Mathf.Approximately(max, float.MinValue) ? float.NaN : max;
        }

        private DateTime _lastUpdate;
        
        void Update()
        {
            _fpsIndex = (_fpsIndex + 1) % samplesCount;
            _fps[_fpsIndex] = Time.deltaTime;
            
            if (!text || Time.deltaTime == 0) return;
            
            if (_lastUpdate.AddSeconds(2.5f) > DateTime.Now) return;
            _lastUpdate = DateTime.Now;
            
            var cal = CalculateFps();
            var min = MinFps();
            var max = MaxFps();
            
            text.UpdateText(new[]
            {
                float.IsNaN(cal) ? "0" : (1 / cal).ToString("0"),
                (cal * 1000).ToString("0"),
                float.IsNaN(min) ? "0" : (1 / min).ToString("0"),
                (min * 1000).ToString("0"),
                float.IsNaN(max) ? "0" : (1 / max).ToString("0"),
                (max * 1000).ToString("0"),
                float.IsNaN(Time.deltaTime) ? "0" :  (1 / Time.deltaTime).ToString("0"),
                (Time.deltaTime * 1000).ToString("0"),
                (Time.unscaledDeltaTime * 1000).ToString("0"),
                (Time.smoothDeltaTime * 1000).ToString("0"),
                (Time.time).ToString("0"),
                (Time.unscaledTime).ToString("0"),
                (Time.realtimeSinceStartup).ToString("0"),
                (Time.frameCount).ToString("0"),
                (Time.captureFramerate).ToString("0"),
                (Time.maximumDeltaTime).ToString("0"),
                (Time.maximumParticleDeltaTime).ToString("0"),
                (Time.timeScale).ToString("0.00"),
                (Time.fixedTime).ToString("0"),
                (Time.fixedUnscaledTime).ToString("0"),
                (Time.fixedUnscaledDeltaTime).ToString("0"),
                (Time.fixedDeltaTime).ToString("0"),
            });
        }
    }
}