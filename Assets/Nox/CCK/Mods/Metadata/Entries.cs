namespace Nox.CCK.Mods.Metadata
{
    public interface Entries
    {
        string[] GetMain();
        string[] GetClient();
        string[] GetInstance();
        string[] GetEditor();
    }
}