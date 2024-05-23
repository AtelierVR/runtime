namespace Nox.CCK.Mods
{
    public class CCKDescriptor : Descriptor
    {
        public virtual void OnCCKLoad() { }
        public virtual void OnCCKUnload() { }
        public virtual void OnCCKUpdate() { }

        public bool _cckEnabled = false;
        public bool CCKEnabled
        {
            get => _cckEnabled;
            set
            {
                if (_cckEnabled == value) return;
                _cckEnabled = value;
                if (_cckEnabled) OnCCKLoad();
                else OnCCKUnload();
            }
        }
    }
}