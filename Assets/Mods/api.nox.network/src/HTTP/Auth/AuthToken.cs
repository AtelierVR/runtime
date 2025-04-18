using Nox.CCK.Utils;

namespace api.nox.network.Auths
{
    public class AuthToken : INoxObject
    {
        [NoxPublic(NoxAccess.Read)] public string Token;
        [NoxPublic(NoxAccess.Read)] public bool IsIntegrity;

        public string ToHeader() => IsIntegrity ? $"Integrity {Token}" : $"Bearer {Token}";

        public override string ToString()
            => $"{GetType().Name}[Token={Token}, IsIntegrity={IsIntegrity}]";
    }
}