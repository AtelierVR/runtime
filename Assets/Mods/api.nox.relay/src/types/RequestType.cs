namespace api.nox.relay.types
{
    public enum RequestType : byte
    {
        Disconnect = 0x00,
        Handshake = 0x01,
        Status = 0x02,
        Latency = 0x03,
        Authentication = 0x04,
        Enter = 0x05,
        Quit = 0x06,
        CustomDataPacket = 0x07,
        PasswordRequirement = 0x08,
        Configuration = 0x09,
        Transform = 0x0C,
        Teleport = 0x0D,
        None = 0xFF
    }
}