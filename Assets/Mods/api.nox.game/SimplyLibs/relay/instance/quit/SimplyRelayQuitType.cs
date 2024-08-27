namespace Nox.SimplyLibs
{

    public enum SimplyRelayQuitType : byte
    {
        Normal = 0,
        Timeout = 1,
        ModerationKick = 2,
        VoteKick = 3,
        ConfigurationError = 4,
        UnknowError = 5
    }
}