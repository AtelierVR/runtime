namespace api.nox.network.Auths
{
    [System.Serializable]
    public class LogoutResponse
    {
        public bool success;

        public override string ToString()
            => $"{GetType().Name}[success={success}]";
    }
}