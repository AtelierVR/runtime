using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyUserUpdate : ShareObject
    {
        [ShareObjectExport] public string username;
        [ShareObjectExport] public string display;
        [ShareObjectExport] public string email;
        [ShareObjectExport] public string password;
        [ShareObjectExport] public string thumbnail;
        [ShareObjectExport] public string banner;
        [ShareObjectExport] public string[] links;
        [ShareObjectExport] public string home;
    }
}