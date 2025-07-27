using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.relay {
	public class RelayLocalPlayer : RelayPlayer {
		public override bool IsLocal()
			=> true;

		public void SendTransform() {
			var trs = Transforms
				.Where(e => e.Value.DeliveryType == TransformDeliveryType.LocalModified)
				.ToArray();
			if (trs.Length == 0) return;
			foreach (var tr in trs) {
				var packet = types.Transform.InstanceRequestTransform.CreatePlayer(Reference.Id, tr.Key, tr.Value);
				Adapter.Instance.SendTransform(packet).Forget();
				tr.Value.DeliveryType = TransformDeliveryType.None;
				Transforms[tr.Key]    = tr.Value;
			}
		}
	}
}