using System.Collections.Generic;
using System.ServiceModel;
using DatabaseLibrary.Models;

namespace SharedContracts
{
    [ServiceContract(Name = "LobbyDuplex", Namespace = "urn:gaming:lobby",
        CallbackContract = typeof(InterfaceLobbyDuplexCallback), SessionMode = SessionMode.Allowed)]
    public interface InterfaceLobbyDuplex
    {
        [OperationContract] bool Register(string username);
        [OperationContract] bool Login(string username);
        [OperationContract] void Logout(string username);
        [OperationContract] bool CreateRoom(string roomName);
        [OperationContract] bool JoinRoom(string username, string roomName);
        [OperationContract] void LeaveRoom(string username, string roomName);
        [OperationContract] List<string> GetAllRooms();
        [OperationContract] List<string> GetPlayersInRoom(string roomName);
        [OperationContract] void SendRoomMessage(RoomMessage msg);
        [OperationContract] List<RoomMessage> GetMessages(string roomName);
        [OperationContract] void SendPrivateMessage(PrivateMessage msg);
        [OperationContract] List<PrivateMessage> GetPrivateHistory(string userA, string userB);
        [OperationContract] void ShareFile(SharedFile file);
        [OperationContract] List<SharedFile> GetSharedFiles(string roomName);
        [OperationContract] byte[] DownloadSharedFile(string fileId);
    }

    [ServiceContract(Name = "LobbyDuplexCallback", Namespace = "urn:gaming:lobby")]
    public interface InterfaceLobbyDuplexCallback
    {
        [OperationContract(IsOneWay = true)] void OnRoomMessageReceived(RoomMessage msg);
        [OperationContract(IsOneWay = true)] void OnRoomRosterChanged(string roomName, List<string> players);
        [OperationContract(IsOneWay = true)] void OnRoomsUpdated(List<string> rooms);
        [OperationContract(IsOneWay = true)] void OnPrivateMessageReceived(PrivateMessage msg);
        [OperationContract(IsOneWay = true)] void OnFileSharedNotice(SharedFileNotice notice);
        [OperationContract(IsOneWay = true)] void OnLobbyMessageReceived(RoomMessage msg);
        [OperationContract(IsOneWay = true)] void OnLobbyFileSharedNotice(SharedFileNotice notice);
        [OperationContract(IsOneWay = true)] void OnFileShared(SharedFile file);
        [OperationContract(IsOneWay = true)] void OnLobbyFileShared(SharedFile file);
    }

    // Separate non-duplex contract for the polling client. The client uses its named
    // PollingChannelFactory wrapper and refreshes server state through these operations.
    [ServiceContract(Name = "LobbyPolling", Namespace = "urn:gaming:lobby", SessionMode = SessionMode.Allowed)]
    public interface InterfaceLobbyPolling
    {
        [OperationContract] bool Register(string username);
        [OperationContract] bool LoginPolling(string username);
        [OperationContract] void Logout(string username);
        [OperationContract] bool CreateRoom(string roomName);
        [OperationContract] bool JoinRoom(string username, string roomName);
        [OperationContract] void LeaveRoom(string username, string roomName);
        [OperationContract] List<string> GetAllRooms();
        [OperationContract] List<string> GetPlayersInRoom(string roomName);
        [OperationContract] void SendRoomMessage(RoomMessage msg);
        [OperationContract] List<RoomMessage> GetMessages(string roomName);
        [OperationContract] void SendPrivateMessage(PrivateMessage msg);
        [OperationContract] List<PrivateMessage> GetPrivateHistory(string userA, string userB);
        [OperationContract] void ShareFile(SharedFile file);
        [OperationContract] List<SharedFile> GetSharedFiles(string roomName);
        [OperationContract] byte[] DownloadSharedFile(string fileId);
    }

}
