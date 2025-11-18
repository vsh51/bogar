namespace Bogar.BLL.Networking;

/// <summary>
/// Defines the supported protocol messages between server and client.
/// </summary>
public enum MessageType : byte
{
    ClientRegister = 0x01,
    ServerRegisterAck = 0x02,
    ServerMatchPrepare = 0x03,
    ServerGameStart = 0x04,
    ServerRequestMove = 0x05,
    ClientMoveResponse = 0x06,
    ServerGameEnd = 0x07,
    ServerError = 0x08,
    ClientDisconnect = 0x09
}
