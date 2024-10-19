namespace api.nox.network.Auths
{
    public class AuthToken
    {
        public string token;
        public bool isIntegrity;

        public string ToHeader() => isIntegrity ? $"Integrity {token}" : $"Bearer {token}";
    }
}