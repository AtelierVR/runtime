using System;
using System.Linq;
using Nox.CCK.Convertors;
using Nox.CCK.Language;
using Nox.CCK.Network;
using Nox.Servers;

namespace api.nox.server
{
    [Serializable]
    public class ServerMetadata : IServerMetadata
    {
        public string Title { get; private set; }

        public string Description { get; private set; }

        public string Icon { get; private set; }

        public string Contact { get; private set; }

        private static string Resolve(TranslatedString ts)
        {
            if (ts == null || ts.Count == 0) return null;
            if (ts.TryGetValue(LanguageManager.CurrentLanguage, out var v)) return v;
            if (ts.TryGetValue(LanguageManager.FallbackLanguage, out var vf)) return vf;
            return ts.Values.FirstOrDefault();
        }

        private static string Resolve(DictionnaryOrString dos)
        {
            if (dos == null || dos.Count == 0) return null;
            return dos.Values.FirstOrDefault();
        }

        public static ServerMetadata From(NoxMetadata m)
            => m != null
                ? new ServerMetadata
                {
                    Title = Resolve(m.title),
                    Description = Resolve(m.description),
                    Icon = Resolve(m.icon),
                    Contact = m.contact
                }
                : null;
    }
}
