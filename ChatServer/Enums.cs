namespace ChatServer
{
    enum AuthMethod
    {
        UsernameOnly,
        Full,
        InviteCode
    }

    enum LogMessageType
    {
        Config,
        Network,
        Chat,
        Auth,
        UserEvent,
        Packet
    }
}