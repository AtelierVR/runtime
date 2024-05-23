namespace Nox.CCK.Mods.Events
{
    public interface Event<T>
    {
        void Invoke(T arg);
    }
}