using Autohand;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace api.nox.game.Controllers
{
    public enum XRRotationType
    {
        Smooth,
        Snap
    }

    public class XRController : BaseController
    {
        public InputActionManager inputAction;

        public void Start()
        {
            inputAction.enabled = false;
            UniTask.RunOnThreadPool(async () =>
            {
                await UniTask.WaitForSeconds(.1f);
                inputAction.enabled = true;
            }).Forget();
        }

        public override void OnEnable()
        {
            base.OnEnable();
        }

        private void SetRotationType(XRRotationType type) => Player.rotationType = type switch
        {
            XRRotationType.Smooth => Autohand.RotationType.smooth,
            XRRotationType.Snap => Autohand.RotationType.snap,
            _ => Player.rotationType
        };

        private XRRotationType GetRotationType() => Player.rotationType switch
        {
            Autohand.RotationType.smooth => XRRotationType.Smooth,
            Autohand.RotationType.snap => XRRotationType.Snap,
            _ => XRRotationType.Smooth
        };

        public XRRotationType RotationType
        {
            get => GetRotationType();
            set => SetRotationType(value);
        }

        public float SmoothTurnSpeed
        {
            get => Player.smoothTurnSpeed;
            set => Player.smoothTurnSpeed = value;
        }
        public float SnapTurnAngle
        {
            get => Player.snapTurnAngle;
            set => Player.snapTurnAngle = value;
        }
    }
}