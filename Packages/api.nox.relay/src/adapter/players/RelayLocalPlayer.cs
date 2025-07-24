namespace src.adapter.players {
	public class RelayLocalPlayer : RelayPlayer {
		public override bool IsLocal()
			=> true;
	}
}