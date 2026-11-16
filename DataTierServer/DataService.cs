using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using DatabaseLibrary;
using DatabaseLibrary.Models;

namespace DataTierServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class DataService : InterfaceData
    {
        private readonly Database db = Database.Instance;
        public bool InsertPlayer(string username) => db.AddPlayer(username);
        public bool RemovePlayer(string username) => db.RemovePlayer(username);
        public bool ValidatePlayer(string username) => db.IsPlayerExists(username);
        public bool CreateRoom(string roomName) => db.AddRoom(roomName);
        public List<string> GetAllRooms() => db.GetRooms().ToList();
        public bool AddPlayerToRoom(string username, string roomName) => db.AddPlayerToRoom(username, roomName);
        public bool RemovePlayerFromRoom(string username, string roomName) => db.RemovePlayerFromRoom(username, roomName);
        public void RemovePlayerFromAllRooms(string username) => db.RemovePlayerFromAllRooms(username);
        public bool IsPlayerInRoom(string username, string roomName) => db.IsPlayerInRoom(username, roomName);
        public List<string> GetPlayersInRoom(string roomName) => db.GetPlayersInRoom(roomName).ToList();
        public void InsertRoomMessage(RoomMessage msg) => db.AddRoomMessage(msg);
        public List<RoomMessage> GetMessages(string roomName) => db.GetRoomMessages(roomName).ToList();
        public void InsertPrivateMessage(PrivateMessage msg) => db.AddPrivateMessage(msg);
        public List<PrivateMessage> GetPrivateMessages(string from, string to) => db.GetPrivateMessages(from, to).ToList();
        public void InsertSharedFile(SharedFile file) => db.AddSharedFile(file);
        public List<SharedFile> GetSharedFiles(string roomName) => db.GetSharedFiles(roomName).ToList();
    }
}
