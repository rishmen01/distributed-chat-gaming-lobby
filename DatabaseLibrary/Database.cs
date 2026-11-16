using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseLibrary.Models;

namespace DatabaseLibrary
{
    /// <summary>
    /// In-memory data store for the assignment demonstration.
    /// Registered users are intentionally kept after logout; active room
    /// membership is separate from registration.
    /// </summary>
    public sealed class Database
    {
        private static readonly Lazy<Database> instance = new Lazy<Database>(() => new Database());
        public static Database Instance => instance.Value;

        private readonly HashSet<string> players = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> rooms =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<RoomMessage>> roomMessages =
            new Dictionary<string, List<RoomMessage>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<PrivateMessage> privateMessages = new List<PrivateMessage>();
        private readonly Dictionary<string, List<SharedFile>> roomFiles =
            new Dictionary<string, List<SharedFile>>(StringComparer.OrdinalIgnoreCase);

        private Database() { }

        public bool AddPlayer(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            lock (players) return players.Add(username.Trim());
        }

        // Retained for administrative/test use. Logout must not call this method.
        public bool RemovePlayer(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            lock (players)
            {
                bool removed = players.Remove(username.Trim());
                if (removed)
                {
                    lock (rooms)
                        foreach (var members in rooms.Values) members.Remove(username.Trim());
                }
                return removed;
            }
        }

        public bool IsPlayerExists(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            lock (players) return players.Contains(username.Trim());
        }

        public bool AddRoom(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName)) return false;
            lock (rooms)
            {
                if (rooms.ContainsKey(roomName.Trim())) return false;
                rooms.Add(roomName.Trim(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                return true;
            }
        }

        public IEnumerable<string> GetRooms()
        {
            lock (rooms) return rooms.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>Atomically enforces registration and one-room-at-a-time membership.</summary>
        public bool AddPlayerToRoom(string username, string roomName)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(roomName)) return false;
            username = username.Trim();
            roomName = roomName.Trim();
            lock (players)
            {
                if (!players.Contains(username)) return false;
                lock (rooms)
                {
                    if (!rooms.ContainsKey(roomName)) return false;
                    foreach (var entry in rooms)
                    {
                        if (entry.Value.Contains(username))
                            return string.Equals(entry.Key, roomName, StringComparison.OrdinalIgnoreCase);
                    }
                    return rooms[roomName].Add(username);
                }
            }
        }

        public bool RemovePlayerFromRoom(string username, string roomName)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(roomName)) return false;
            lock (rooms)
                return rooms.TryGetValue(roomName.Trim(), out var members) && members.Remove(username.Trim());
        }

        public void RemovePlayerFromAllRooms(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return;
            lock (rooms)
                foreach (var members in rooms.Values) members.Remove(username.Trim());
        }

        public bool IsPlayerInRoom(string username, string roomName)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(roomName)) return false;
            lock (rooms)
                return rooms.TryGetValue(roomName.Trim(), out var members) && members.Contains(username.Trim());
        }

        public IEnumerable<string> GetPlayersInRoom(string roomName)
        {
            lock (rooms)
            {
                if (!rooms.TryGetValue(roomName ?? string.Empty, out var members)) return new List<string>();
                return members.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            }
        }

        public void AddRoomMessage(RoomMessage msg)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            lock (roomMessages)
            {
                if (!roomMessages.TryGetValue(msg.Room, out var messages))
                {
                    messages = new List<RoomMessage>();
                    roomMessages.Add(msg.Room, messages);
                }
                messages.Add(msg);
            }
        }

        public IEnumerable<RoomMessage> GetRoomMessages(string roomName)
        {
            lock (roomMessages)
                return roomMessages.TryGetValue(roomName ?? string.Empty, out var messages)
                    ? messages.ToList() : new List<RoomMessage>();
        }

        public void AddPrivateMessage(PrivateMessage msg)
        {
            if (msg == null) throw new ArgumentNullException(nameof(msg));
            lock (privateMessages) privateMessages.Add(msg);
        }

        public IEnumerable<PrivateMessage> GetPrivateMessages(string from, string to)
        {
            lock (privateMessages)
                return privateMessages.Where(m =>
                    (string.Equals(m.Sender, from, StringComparison.OrdinalIgnoreCase) && string.Equals(m.Receiver, to, StringComparison.OrdinalIgnoreCase)) ||
                    (string.Equals(m.Sender, to, StringComparison.OrdinalIgnoreCase) && string.Equals(m.Receiver, from, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(m => m.Timestamp).ToList();
        }

        public void AddSharedFile(SharedFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            lock (roomFiles)
            {
                if (!roomFiles.TryGetValue(file.Room, out var files))
                {
                    files = new List<SharedFile>();
                    roomFiles.Add(file.Room, files);
                }
                files.Add(file);
            }
        }

        public IEnumerable<SharedFile> GetSharedFiles(string roomName)
        {
            lock (roomFiles)
                return roomFiles.TryGetValue(roomName ?? string.Empty, out var files)
                    ? files.Select(f => new SharedFile
                    {
                        FileId = f.FileId, Room = f.Room, Sender = f.Sender,
                        FileName = f.FileName, FileData = new byte[0], Timestamp = f.Timestamp
                    }).ToList()
                    : new List<SharedFile>();
        }
    }
}
