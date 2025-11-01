namespace api.nox.relay {
	public abstract class RelayParameter : RelayProperty {
		protected RelayParameter(RelayEntity entity) : base(entity) { }

		public override string ToString()
			=> $"{GetType().Name}[Key={GetKey()}, Value={GetValue()}, Flags={GetFlags()}, Dirty={GetDirty()}]";
	}
}