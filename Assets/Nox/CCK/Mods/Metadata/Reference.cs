namespace Nox.CCK.Mods.Metadata
{
    public interface Reference
    {
        string GetNamespace();
        string GetFile();
        Engine GetEngine();
        Platfrom GetPlatform();
    }
}