using api.nox.game.Controllers;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Logger = Nox.CCK.Logger;

namespace api.nox.game.UI
{
    public class FadeTransition : MonoBehaviour
    {
        public static FadeTransition Instance;

        void Awake()
        {
            Instance = this;
        }

        public Camera Camera => PlayerController.GetCurrentController().PlayerCamera;
        public MeshRenderer Renderer => GetComponent<MeshRenderer>();

        public Material Material => Renderer.material;

        public float Near => Camera.nearClipPlane;
        public float Far => Camera.farClipPlane;

        public float Duration = 1.0f;
        public string ShaderProperty = "_Color";

        public bool FollowCamera = false;

        public static async UniTask MakeFadeIn() => await Instance.FadeIn();
        public static async UniTask MakeFadeOut() => await Instance.FadeOut();

        public async UniTask FadeIn()
        {
            Logger.Log("Fading in 0");
            var material = Material;
            var near = Far;
            var far = Near * 3.5f;
            FollowCamera = true;
            material.SetColor(ShaderProperty, Color.black);
            transform.localScale = new Vector3(far, far, far);
            float time = 0;
            while (time < Duration)
            {
                time += Time.deltaTime;
                material.SetColor(ShaderProperty, Color.Lerp(Color.black, Color.clear, time / Duration));
                transform.localScale = new Vector3(
                    Mathf.Lerp(far, near, time / Duration),
                    Mathf.Lerp(far, near, time / Duration),
                    Mathf.Lerp(far, near, time / Duration)
                );
                await UniTask.Yield();
            }
            material.SetColor(ShaderProperty, Color.clear);
            transform.localScale = new Vector3(near, near, near);
            FollowCamera = false;
            Logger.Log("Fading in 1");
        }

        public async UniTask FadeOut()
        {
            Logger.Log("Fading out 0");
            var material = Material;
            var near = Far;
            var far = Near * 3.5f;
            FollowCamera = true;
            material.SetColor(ShaderProperty, Color.clear);
            transform.localScale = new Vector3(near, near, near);
            float time = 0;
            while (time < Duration)
            {
                time += Time.deltaTime;
                material.SetColor(ShaderProperty, Color.Lerp(Color.clear, Color.black, time / Duration));
                transform.localScale = new Vector3(
                    Mathf.Lerp(near, far, time / Duration),
                    Mathf.Lerp(near, far, time / Duration),
                    Mathf.Lerp(near, far, time / Duration)
                );
                await UniTask.Yield();
            }
            material.SetColor(ShaderProperty, Color.black);
            transform.localScale = new Vector3(far, far, far);
            FollowCamera = true;
            Logger.Log("Fading out 1");
        }

        void Update()
        {
            if (FollowCamera)
                transform.position = Camera.transform.position;
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