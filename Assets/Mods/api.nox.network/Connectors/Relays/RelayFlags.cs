namespace api.nox.network.Relays
{
    public enum RelayFlags : byte
    {
        None = 0,
        ServerIsReady = 1,
        AuthVerification = 2,
        AuthRequired = 4
    }
}