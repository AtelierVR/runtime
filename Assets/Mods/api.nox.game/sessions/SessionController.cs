using System;
using Cysharp.Threading.Tasks;

namespace api.nox.game.sessions
{
    public interface ISessionController
    {
        public string SessionType => "unknown";
        public Session GetSession();

        public string GetTitle() => GetSession().group + " " + GetSession().id;
        public string GetThumbnail() => null;
        public string GetDescription() => "";

        internal void SetSession(Session session);
        public UniTask<bool> Prepare();
        public UniTask Close();
        public void Update() { }
    }
}