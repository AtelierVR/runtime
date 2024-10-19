namespace api.nox.network.Relays
{
    public enum RequestType : byte
    {
        Disconnect = 0x00,
        Handshake = 0x01,
        Status = 0x02,
        Latency = 0x03,
        Authentification = 0x04,
        Enter = 0x05,
        Quit = 0x06,
        CustomDataPacket = 0x07,
        PasswordRequirement = 0x08,
        Configuration = 0x09,
        Transform = 0x0C,
        Teleport = 0x0D,
        None = 0xFF
    }

    public enum ResponseType : byte
    {
        Disconnect = 0x00,
        Handshake = 0x01,
        Status = 0x02,
        Latency = 0x03,
        Authentification = 0x04,
        Enter = 0x05,
        Quit = 0x06,
        CustomDataPacket = 0x07,
        Configuration = 0x09,
        Join = 0x0A,
        Leave = 0x0B,
        Transform = 0x0C,
        Teleport = 0x0D
    }



    public enum RelayCustomDataPacketType : byte
    {
        None = 0,
        Request = 1,
        Sent = 2,
        Receive = 3,
        Response = 4,
        Callback = 5
    }


    public enum RelayPasswordRequirementResult : byte
    {
        Success = 0,
        Invalid = 1
    }


    public enum RelayConfigurationAction : byte
    {
        Success = 0,
        Invalid = 1,
        WorldData = 2,
        WorldLoaded = 3,
        ModList = 4,
        ModsLoaded = 5,
        ModWarn = 6
    }


}