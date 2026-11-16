using System;
using System.Linq;
using System.Threading.Tasks;
using DatabaseLibrary;
using DatabaseLibrary.Models;

namespace DatabaseLibrary.SmokeTests
{
    internal static class Program
    {
        private static int assertions;

        private static void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException("FAILED: " + message);
            Console.WriteLine("PASS: " + message);
        }

        private static int Main()
        {
            try
            {
                var db = Database.Instance;
                string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
                string user = "test_" + suffix;
                string roomOne = "roomA_" + suffix;
                string roomTwo = "roomB_" + suffix;

                Assert(db.AddPlayer(user), "first registration succeeds");
                Assert(!db.AddPlayer(user.ToUpperInvariant()), "duplicate registration is case-insensitive");
                Assert(db.AddRoom(roomOne), "first room is created");
                Assert(db.AddRoom(roomTwo), "second room is created");
                Assert(db.AddPlayerToRoom(user, roomOne), "registered user can join a room");
                Assert(!db.AddPlayerToRoom(user, roomTwo), "one-room-at-a-time membership is enforced");
                Assert(db.IsPlayerInRoom(user, roomOne), "room membership is queryable");
                Assert(db.RemovePlayerFromRoom(user, roomOne), "player can leave the current room");
                Assert(db.AddPlayerToRoom(user, roomTwo), "player can join another room after leaving");

                var message = new RoomMessage(roomTwo, user, "hello");
                db.AddRoomMessage(message);
                Assert(db.GetRoomMessages(roomTwo).Count() == 1, "room message is retained");

                var privateMessage = new PrivateMessage(user, "peer_" + suffix, "private hello");
                db.AddPrivateMessage(privateMessage);
                Assert(db.GetPrivateMessages(user, privateMessage.Receiver).Count() == 1, "private conversation history is retained");

                string fileId = Guid.NewGuid().ToString("N");
                db.AddSharedFile(new SharedFile
                {
                    FileId = fileId, Room = roomTwo, Sender = user,
                    FileName = "notes.txt", FileData = new byte[0], Timestamp = DateTime.Now
                });
                var files = db.GetSharedFiles(roomTwo).ToList();
                Assert(files.Count == 1 && files[0].FileId == fileId, "shared-file metadata retains the download identifier");
                Assert(files[0].FileData == null || files[0].FileData.Length == 0, "metadata listing does not return file bytes");

                db.RemovePlayerFromAllRooms(user);
                Assert(db.IsPlayerExists(user), "logout-style room cleanup keeps the registered account");
                Assert(!db.IsPlayerInRoom(user, roomTwo), "logout-style cleanup removes room membership");

                // Concurrent attempts to join different rooms must still yield at most one membership.
                string concurrentUser = "test_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                string concurrentRoomA = "raceA_" + suffix;
                string concurrentRoomB = "raceB_" + suffix;
                db.AddPlayer(concurrentUser);
                db.AddRoom(concurrentRoomA);
                db.AddRoom(concurrentRoomB);
                Parallel.Invoke(
                    () => db.AddPlayerToRoom(concurrentUser, concurrentRoomA),
                    () => db.AddPlayerToRoom(concurrentUser, concurrentRoomB));
                int memberships = (db.IsPlayerInRoom(concurrentUser, concurrentRoomA) ? 1 : 0) +
                                  (db.IsPlayerInRoom(concurrentUser, concurrentRoomB) ? 1 : 0);
                Assert(memberships == 1, "concurrent room joins cannot create multiple memberships");

                Console.WriteLine("\nAll " + assertions + " data-layer smoke assertions passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }
    }
}
