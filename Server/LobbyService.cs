using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using DataTierServer;
using DatabaseLibrary.Models;
using SharedContracts;

namespace Server
{
    // A single service instance serialises operations that touch session and callback state.
    // The data store separately protects its collections with locks.
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Single, AutomaticSessionShutdown = false)]
    public sealed class LobbyService : InterfaceLobbyDuplex, InterfaceLobbyPolling
    {
        private const int MaxFileBytes = 5 * 1024 * 1024;
        private const int MaxMessageLength = 2000;
        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".txt", ".png", ".jpg", ".jpeg", ".gif", ".bmp" };

        private readonly InterfaceData dataProxy;
        private readonly Dictionary<string, InterfaceLobbyDuplexCallback> userCallbacks =
            new Dictionary<string, InterfaceLobbyDuplexCallback>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> sessionUsers = new Dictionary<string, string>();
        private readonly HashSet<string> pollingUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> userRooms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly string sharedRoot;

        public LobbyService(InterfaceData dataProxy)
        {
            this.dataProxy = dataProxy ?? throw new ArgumentNullException(nameof(dataProxy));
            sharedRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SharedFiles");
            Directory.CreateDirectory(sharedRoot);
        }

        public bool Register(string username)
        {
            username = NormalizeUsername(username);
            if (username == null) return false;
            bool added = dataProxy.InsertPlayer(username);
            Console.WriteLine("[REGISTER] {0} {1}", username, added ? "added" : "already registered");
            return added;
        }

        public bool Login(string username)
        {
            username = NormalizeUsername(username);
            string sessionId = OperationContext.Current?.SessionId;
            if (username == null || string.IsNullOrWhiteSpace(sessionId) || !dataProxy.ValidatePlayer(username))
                return false;
            if (userCallbacks.ContainsKey(username) || pollingUsers.Contains(username) || sessionUsers.ContainsKey(sessionId)) return false;

            var callback = OperationContext.Current.GetCallbackChannel<InterfaceLobbyDuplexCallback>();
            userCallbacks.Add(username, callback);
            sessionUsers.Add(sessionId, username);
            Console.WriteLine("[LOGIN] {0} (duplex)", username);
            return true;
        }

        public bool LoginPolling(string username)
        {
            username = NormalizeUsername(username);
            string sessionId = OperationContext.Current?.SessionId;
            if (username == null || string.IsNullOrWhiteSpace(sessionId) || !dataProxy.ValidatePlayer(username))
                return false;
            if (userCallbacks.ContainsKey(username) || pollingUsers.Contains(username) || sessionUsers.ContainsKey(sessionId))
                return false;
            sessionUsers.Add(sessionId, username);
            pollingUsers.Add(username);
            Console.WriteLine("[LOGIN] {0} (polling)", username);
            return true;
        }

        public void Logout(string username)
        {
            string current = GetCurrentUser();
            if (!string.Equals(current, username, StringComparison.OrdinalIgnoreCase))
                throw new FaultException("You may only log out your own session.");
            CleanupSession(current);
            Console.WriteLine("[LOGOUT] {0}", current);
        }

        public bool CreateRoom(string roomName)
        {
            RequireUser();
            roomName = NormalizeRoomName(roomName);
            if (roomName == null) return false;
            bool created = dataProxy.CreateRoom(roomName);
            if (created)
            {
                var rooms = dataProxy.GetAllRooms();
                NotifyAllDuplex(cb => cb.OnRoomsUpdated(rooms));
            }
            Console.WriteLine("[ROOM] '{0}' {1}", roomName, created ? "created" : "exists");
            return created;
        }

        public bool JoinRoom(string username, string roomName)
        {
            string current = RequireUser();
            if (!string.Equals(current, username, StringComparison.OrdinalIgnoreCase))
                throw new FaultException("You may only join a room as the logged-in player.");
            roomName = NormalizeRoomName(roomName);
            if (roomName == null || !dataProxy.AddPlayerToRoom(current, roomName)) return false;
            userRooms[current] = roomName;
            BroadcastRoster(roomName);
            Console.WriteLine("[JOIN] {0} -> {1}", current, roomName);
            return true;
        }

        public void LeaveRoom(string username, string roomName)
        {
            string current = RequireUser();
            if (!string.Equals(current, username, StringComparison.OrdinalIgnoreCase))
                throw new FaultException("You may only leave a room as the logged-in player.");
            if (!userRooms.TryGetValue(current, out var activeRoom) ||
                !string.Equals(activeRoom, roomName, StringComparison.OrdinalIgnoreCase)) return;
            dataProxy.RemovePlayerFromRoom(current, activeRoom);
            userRooms.Remove(current);
            BroadcastRoster(activeRoom);
            Console.WriteLine("[LEAVE] {0} -/-> {1}", current, activeRoom);
        }

        public List<string> GetAllRooms()
        {
            RequireUser();
            return dataProxy.GetAllRooms();
        }

        public List<string> GetPlayersInRoom(string roomName)
        {
            string current = RequireUser();
            RequireRoomMembership(current, roomName);
            return dataProxy.GetPlayersInRoom(roomName);
        }

        public void SendRoomMessage(RoomMessage msg)
        {
            string current = RequireUser();
            if (msg == null || !string.Equals(current, msg.Sender, StringComparison.OrdinalIgnoreCase))
                throw new FaultException("Invalid message sender.");
            RequireRoomMembership(current, msg.Room);
            string content = (msg.Content ?? string.Empty).Trim();
            if (content.Length == 0 || content.Length > MaxMessageLength)
                throw new FaultException("Message must contain 1 to 2000 characters.");
            msg.Sender = current;
            msg.Content = content;
            msg.Timestamp = DateTime.Now;
            dataProxy.InsertRoomMessage(msg);
            var players = dataProxy.GetPlayersInRoom(msg.Room);
            foreach (var player in players)
                Notify(player, cb => cb.OnRoomMessageReceived(msg));
            Console.WriteLine("[MSG] {0} :: {1}: {2}", msg.Room, current, content);
        }

        public List<RoomMessage> GetMessages(string roomName)
        {
            string current = RequireUser();
            RequireRoomMembership(current, roomName);
            return dataProxy.GetMessages(roomName).OrderBy(m => m.Timestamp).ToList();
        }

        public void SendPrivateMessage(PrivateMessage msg)
        {
            string current = RequireUser();
            if (msg == null || !string.Equals(current, msg.Sender, StringComparison.OrdinalIgnoreCase))
                throw new FaultException("Invalid private-message sender.");
            if (string.IsNullOrWhiteSpace(msg.Receiver) || string.Equals(current, msg.Receiver, StringComparison.OrdinalIgnoreCase))
                throw new FaultException("Choose another player for a private message.");
            if (!userRooms.TryGetValue(current, out var room) ||
                !userRooms.TryGetValue(msg.Receiver, out var receiverRoom) ||
                !string.Equals(room, receiverRoom, StringComparison.OrdinalIgnoreCase))
                throw new FaultException("Both players must be in the same room to exchange private messages.");
            msg.Sender = current;
            msg.Content = (msg.Content ?? string.Empty).Trim();
            if (msg.Content.Length == 0 || msg.Content.Length > MaxMessageLength)
                throw new FaultException("Private message must contain 1 to 2000 characters.");
            msg.Timestamp = DateTime.Now;
            dataProxy.InsertPrivateMessage(msg);
            Notify(msg.Receiver, cb => cb.OnPrivateMessageReceived(msg));
            Console.WriteLine("[DM] {0} -> {1}", current, msg.Receiver);
        }

        public List<PrivateMessage> GetPrivateHistory(string userA, string userB)
        {
            string current = RequireUser();
            if (!string.Equals(current, userA, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(current, userB, StringComparison.OrdinalIgnoreCase))
                throw new FaultException("You may only retrieve a conversation in which you participate.");
            return dataProxy.GetPrivateMessages(userA, userB).OrderBy(m => m.Timestamp).ToList();
        }

        public void ShareFile(SharedFile file)
        {
            string current = RequireUser();
            if (file == null || !string.Equals(current, file.Sender, StringComparison.OrdinalIgnoreCase))
                throw new FaultException("Invalid file sender.");
            RequireRoomMembership(current, file.Room);
            string originalName = Path.GetFileName(file.FileName ?? string.Empty);
            string extension = Path.GetExtension(originalName);
            if (string.IsNullOrWhiteSpace(originalName) || originalName.Length > 120 ||
                originalName.Any(c => !(char.IsLetterOrDigit(c) || c == ' ' || c == '.' || c == '_' || c == '-')) ||
                originalName.Contains("..") || !AllowedExtensions.Contains(extension))
                throw new FaultException("Use a simple file name and share only .txt or common image files (.png, .jpg, .jpeg, .gif, .bmp).");
            if (file.FileData == null || file.FileData.Length == 0 || file.FileData.Length > MaxFileBytes)
                throw new FaultException("File must be non-empty and no larger than 5 MB.");
            if (!extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) && !HasValidImageSignature(extension, file.FileData))
                throw new FaultException("The file contents do not match the selected image type.");

            string id = Guid.NewGuid().ToString("N");
            string roomDir = GetRoomDirectory(file.Room);
            Directory.CreateDirectory(roomDir);
            string path = Path.Combine(roomDir, id + "_" + originalName);
            File.WriteAllBytes(path, file.FileData);

            var metadata = new SharedFile
            {
                FileId = id, Room = file.Room, Sender = current, FileName = originalName,
                FileData = new byte[0], Timestamp = DateTime.Now
            };
            dataProxy.InsertSharedFile(metadata);
            var notice = new SharedFileNotice
            {
                FileId = id, Room = file.Room, Sender = current, FileName = originalName,
                Length = file.FileData.Length, Timestamp = metadata.Timestamp
            };
            foreach (var player in dataProxy.GetPlayersInRoom(file.Room))
                Notify(player, cb => cb.OnFileSharedNotice(notice));
            Console.WriteLine("[FILE] {0} shared {1} in {2}", current, originalName, file.Room);
        }

        public List<SharedFile> GetSharedFiles(string roomName)
        {
            string current = RequireUser();
            RequireRoomMembership(current, roomName);
            return dataProxy.GetSharedFiles(roomName);
        }

        public byte[] DownloadSharedFile(string fileId)
        {
            string current = RequireUser();
            if (string.IsNullOrWhiteSpace(fileId) || fileId.Length != 32)
                throw new FaultException("Invalid file identifier.");
            SharedFile metadata = null;
            foreach (var room in dataProxy.GetAllRooms())
            {
                metadata = dataProxy.GetSharedFiles(room).FirstOrDefault(f => f.FileId == fileId);
                if (metadata != null) break;
            }
            if (metadata == null) throw new FaultException("File not found.");
            RequireRoomMembership(current, metadata.Room);
            string path = FindPathById(fileId);
            if (path == null || !File.Exists(path)) throw new FaultException("File content is unavailable.");
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length > MaxFileBytes) throw new FaultException("Stored file exceeds the download limit.");
            return bytes;
        }

        private string GetCurrentUser()
        {
            string sessionId = OperationContext.Current?.SessionId;
            return !string.IsNullOrWhiteSpace(sessionId) && sessionUsers.TryGetValue(sessionId, out var user) ? user : null;
        }

        private string RequireUser()
        {
            string user = GetCurrentUser();
            if (user == null) throw new FaultException("Please log in before performing this operation.");
            return user;
        }

        private void RequireRoomMembership(string username, string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName) || !dataProxy.IsPlayerInRoom(username, roomName))
                throw new FaultException("You must be a member of this room to perform this operation.");
        }

        private void CleanupSession(string username)
        {
            string session = sessionUsers.FirstOrDefault(p => string.Equals(p.Value, username, StringComparison.OrdinalIgnoreCase)).Key;
            if (!string.IsNullOrEmpty(session)) sessionUsers.Remove(session);
            userCallbacks.Remove(username);
            pollingUsers.Remove(username);
            userRooms.Remove(username);
            dataProxy.RemovePlayerFromAllRooms(username);
            foreach (var room in dataProxy.GetAllRooms()) BroadcastRoster(room);
        }

        private void BroadcastRoster(string roomName)
        {
            var players = dataProxy.GetPlayersInRoom(roomName);
            foreach (var player in players)
                Notify(player, cb => cb.OnRoomRosterChanged(roomName, players));
        }

        private void Notify(string username, Action<InterfaceLobbyDuplexCallback> action)
        {
            if (pollingUsers.Contains(username)) return;
            if (!userCallbacks.TryGetValue(username, out var callback)) return;
            try { action(callback); }
            catch (CommunicationException) { CleanupSession(username); }
            catch (TimeoutException) { CleanupSession(username); }
            catch (ObjectDisposedException) { CleanupSession(username); }
        }

        private void NotifyAllDuplex(Action<InterfaceLobbyDuplexCallback> action)
        {
            foreach (var username in userCallbacks.Keys.ToList()) Notify(username, action);
        }

        private static string NormalizeUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            username = username.Trim();
            if (username.Length < 3 || username.Length > 24) return null;
            if (username.Any(c => !(char.IsLetterOrDigit(c) || c == '_' || c == '-'))) return null;
            return username;
        }

        private static string NormalizeRoomName(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName)) return null;
            roomName = roomName.Trim();
            if (roomName.Length < 2 || roomName.Length > 40) return null;
            if (roomName.Any(c => !(char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-'))) return null;
            return roomName;
        }

        private string GetRoomDirectory(string roomName)
        {
            // Base64Url of the UTF-8 room name avoids collisions such as "team room" vs "team_room".
            string encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(roomName))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return Path.Combine(sharedRoot, encoded);
        }

        private string FindPathById(string id)
        {
            foreach (var directory in Directory.GetDirectories(sharedRoot))
            {
                var path = Directory.GetFiles(directory).FirstOrDefault(f =>
                    Path.GetFileName(f).StartsWith(id + "_", StringComparison.OrdinalIgnoreCase));
                if (path != null) return path;
            }
            return null;
        }

        private static bool HasValidImageSignature(string extension, byte[] data)
        {
            if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                return data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47;
            if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                return data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;
            if (extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
                return data.Length >= 6 && System.Text.Encoding.ASCII.GetString(data, 0, 3) == "GIF";
            if (extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
                return data.Length >= 2 && data[0] == (byte)'B' && data[1] == (byte)'M';
            return false;
        }
    }
}
