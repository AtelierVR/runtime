using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Nox.CCK.Mods.Metadata
{
    public interface Person
    {

        string GetName();
        string GetEmail();
        string GetWebsite();
        T Get<T>(string key) where T : class;
        bool Has<T>(string key) where T : class;
        Dictionary<string, object> GetAll();
    }
}