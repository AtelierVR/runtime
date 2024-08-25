namespace api.nox.network.Relays.Auth
{
    public enum AuthResult : byte
    {
        Success = 0,
        InvalidToken = 1,
        CannotContactAuthServer = 2,
        Blacklisted = 3,
        NotReady = 4,
        Unknown = 5
    }
}