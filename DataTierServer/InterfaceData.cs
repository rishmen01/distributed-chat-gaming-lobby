using System.Collections.Generic;
using System.ServiceModel;
using DatabaseLibrary.Models;

namespace DataTierServer
{
    [ServiceContract]
    public interface InterfaceData
    {
        [OperationContract] bool InsertPlayer(string username);
        [OperationContract] bool RemovePlayer(string username);
        [OperationContract] bool ValidatePlayer(string username);
        [OperationContract] bool CreateRoom(string roomName);
        [OperationContract] List<string> GetAllRooms();
        [OperationContract] bool AddPlayerToRoom(string username, string roomName);
        [OperationContract] bool RemovePlayerFromRoom(string username, string roomName);
        [OperationContract] void RemovePlayerFromAllRooms(string username);
        [OperationContract] bool IsPlayerInRoom(string username, string roomName);
        [OperationContract] List<string> GetPlayersInRoom(string roomName);
        [OperationContract] void InsertRoomMessage(RoomMessage msg);
        [OperationContract] List<RoomMessage> GetMessages(string roomName);
        [OperationContract] void InsertPrivateMessage(PrivateMessage msg);
        [OperationContract] List<PrivateMessage> GetPrivateMessages(string from, string to);
        [OperationContract] void InsertSharedFile(SharedFile file);
        [OperationContract] List<SharedFile> GetSharedFiles(string roomName);
    }
}
