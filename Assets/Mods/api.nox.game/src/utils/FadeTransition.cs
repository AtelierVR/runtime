using System;
using api.nox.game.controllers;
using Cysharp.Threading.Tasks;
using Logger = Nox.CCK.Utils.Logger;
using UnityEngine;

namespace api.nox.game.UI
{
    public class FadeTransition : MonoBehaviour
    {
        public static FadeTransition Instance;

        void Awake()
        {
            Instance = this;
        }

        public Camera Camera => PlayerController.Instance.currentController.playerCamera;
        public MeshRenderer Renderer => GetComponent<MeshRenderer>();

        public Material Material => Renderer.material;

        public float Near => Camera.nearClipPlane;
        public float Far => Camera.farClipPlane;

        public float Duration = 1.0f;
        public string ShaderProperty = "_Color";

        public bool FollowCamera = false;

        public static async UniTask MakeFadeIn()
        {
            if (Instance)
                await Instance.FadeIn();
        }

        public static async UniTask MakeFadeOut()
        {
            if (Instance)
                await Instance.FadeOut();
            
        }

        public async UniTask FadeIn()
        {
            Logger.Log("Fading In");
            try
            {
                var material = Material;
                if (!material || !material.HasColor(ShaderProperty))
                    return;
                Renderer.enabled = true;
                FollowCamera = true;
                material.SetColor(ShaderProperty, Color.black);
                transform.localScale = Vector3.one * Far;
                float time = 0;
                while (time < Duration)
                {
                    time += Time.deltaTime;
                    material.SetColor(ShaderProperty, Color.Lerp(Color.black, Color.clear, time / Duration));
                    transform.localScale = Vector3.one * Mathf.Lerp(Far, Near, time / Duration);
                    await UniTask.Yield();
                }

                material.SetColor(ShaderProperty, Color.clear);
                transform.localScale = Vector3.one * Near;
                FollowCamera = false;
                Renderer.enabled = false;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            Logger.Log("Fading In End");
        }

        public async UniTask FadeOut()
        {
            Logger.Log("Fading Out");
            try
            {
                var material = Material;
                if (!material || !material.HasColor(ShaderProperty))
                    return;
                FollowCamera = true;
                Renderer.enabled = true;
                material.SetColor(ShaderProperty, Color.clear);
                transform.localScale = Vector3.one * Near;
                float time = 0;
                while (time < Duration)
                {
                    time += Time.deltaTime;
                    material.SetColor(ShaderProperty, Color.Lerp(Color.clear, Color.black, time / Duration));
                    transform.localScale = Vector3.one * Mathf.Lerp(Near, Far, time / Duration);
                    await UniTask.Yield();
                }

                material.SetColor(ShaderProperty, Color.black);
                transform.localScale = Vector3.one * Far;
                FollowCamera = true;
                Renderer.enabled = true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            Logger.Log("Fading Out End");
        }

        void Update()
        {
            if (FollowCamera)
                transform.position = Camera.transform.position;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(FadeTransition))]
        public class FadeTransitionEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                var fade = target as FadeTransition;
                if (GUILayout.Button("Fade In"))
                {
                    fade.FadeIn().Forget();
                }

                if (GUILayout.Button("Fade Out"))
                {
                    fade.FadeOut().Forget();
                }
            }
        }
#endif
    }
}