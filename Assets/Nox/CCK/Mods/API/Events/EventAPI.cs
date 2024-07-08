namespace Nox.CCK.Mods.Events
{
    public interface EventAPI
    {
        public void Emit(EventContext context);
    }

    public interface EventContext
    {
        public string GetName();
        public string GetDestination();
        public string GetSource();
        public object GetShareObject();
        public void Callback(params object[] args);
    }
}