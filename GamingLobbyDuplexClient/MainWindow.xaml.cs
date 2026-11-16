using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DatabaseLibrary.Models;
using Microsoft.Win32;
using SharedContracts;

namespace GamingLobbyDuplexClient
{
    [CallbackBehavior(UseSynchronizationContext = false, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public partial class MainWindow : Window, InterfaceLobbyDuplexCallback
    {
        private InterfaceLobbyDuplex proxy;
        private DuplexChannelFactory<InterfaceLobbyDuplex> factory;
        private string username;
        private string currentRoom;
        private readonly Dictionary<string, PrivateChatWindow> dmWindows = new Dictionary<string, PrivateChatWindow>(StringComparer.OrdinalIgnoreCase);
        private readonly object proxyGate = new object();
        public MainWindow()
        {
            InitializeComponent();
            SetAuthenticatedControls(false);
            SetRoomChatEnabled(false);
            ClearFlow(ChatBox);
        }

        //===== Menu =====
        private void MenuNewWindow_Click(object sender, RoutedEventArgs e)
        {
            try { new MainWindow().Show(); }
            catch (Exception ex) { MessageBox.Show("Could not open a new window: " + ex.Message); }
        }
        private void MenuNewProcess_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exePath = Assembly.GetExecutingAssembly().Location;
                Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });
            }
            catch (Exception ex) { MessageBox.Show("Could not start a new process: " + ex.Message); }
        }
        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            try { Application.Current.Shutdown(); } catch { Close(); }
        }

        //===== UI helpers =====
        private static void ClearFlow(System.Windows.Controls.RichTextBox rtb) => rtb.Document = new FlowDocument();
        private static void AppendTextLine(System.Windows.Controls.RichTextBox rtb, string text)
        {
            var p = new Paragraph(new Run(text)) { Margin = new Thickness(0, 0, 0, 4) };
            rtb.Document.Blocks.Add(p);
            rtb.ScrollToEnd();
        }
        private void SetAuthenticatedControls(bool authenticated)
        {
            LoginButton.IsEnabled = !authenticated;
            LogoutButton.IsEnabled = authenticated;
            UsernameTextBox.IsEnabled = !authenticated;
            RoomListBox.IsEnabled = authenticated;
            NewRoomTextBox.IsEnabled = authenticated;
            CreateRoomButton.IsEnabled = authenticated;
            UpdateJoinRoomButton();
            LeaveRoomButton.IsEnabled = authenticated && !string.IsNullOrWhiteSpace(currentRoom);
            if (!authenticated) SetRoomChatEnabled(false);
        }

        private void UpdateJoinRoomButton()
        {
            string selectedRoom = RoomListBox.SelectedItem as string;
            JoinRoomButton.IsEnabled = IsAlive(proxy) && !string.IsNullOrWhiteSpace(selectedRoom) &&
                !string.Equals(selectedRoom, currentRoom, StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> ExcludeCurrentPlayer(IEnumerable<string> players, string currentUser)
        {
            return (players ?? Enumerable.Empty<string>())
                .Where(player => !string.Equals(player, currentUser, StringComparison.OrdinalIgnoreCase))
                .OrderBy(player => player, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void ClosePrivateChatsForMissingPlayers(IEnumerable<string> roomPlayers)
        {
            var activePlayers = new HashSet<string>(
                ExcludeCurrentPlayer(roomPlayers, username), StringComparer.OrdinalIgnoreCase);
            foreach (var entry in dmWindows.ToList())
            {
                if (activePlayers.Contains(entry.Key)) continue;
                dmWindows.Remove(entry.Key);
                try { entry.Value?.Close(); } catch { }
            }
        }

        private void SetRoomChatEnabled(bool enabled)
        {
            bool canUseRoom = enabled && IsAlive(proxy) && !string.IsNullOrWhiteSpace(currentRoom);
            MessageTextBox.IsEnabled = canUseRoom;
            SendButton.IsEnabled = canUseRoom;
            ShareRoomFileButton.IsEnabled = canUseRoom;
            PlayerListBox.IsEnabled = canUseRoom;
            RefreshPlayersButton.IsEnabled = canUseRoom;
            PrivateChatButton.IsEnabled = canUseRoom && PlayerListBox.Items.Count > 0;
            LeaveRoomButton.IsEnabled = IsAlive(proxy) && !string.IsNullOrWhiteSpace(currentRoom);
            UpdateJoinRoomButton();
        }

        private static Image BuildImageFromBytes(byte[] bytes, int maxPixels = 320)
        {
            var bi = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.DecodePixelWidth = maxPixels;
                bi.StreamSource = ms;
                bi.EndInit();
                bi.Freeze();
            }
            return new Image { Source = bi, Stretch = Stretch.Uniform, MaxWidth = maxPixels, MaxHeight = maxPixels };
        }

        private static bool IsAlive(InterfaceLobbyDuplex p)
        {
            if (p == null) return false;
            var comm = p as ICommunicationObject;
            return comm != null && comm.State == CommunicationState.Opened;
        }

        public T ExecuteLobby<T>(Func<InterfaceLobbyDuplex, T> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            lock (proxyGate)
            {
                if (!IsAlive(proxy)) throw new InvalidOperationException("Not connected. Please log in again.");
                return operation(proxy);
            }
        }

        public void ExecuteLobby(Action<InterfaceLobbyDuplex> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            lock (proxyGate)
            {
                if (!IsAlive(proxy)) throw new InvalidOperationException("Not connected. Please log in again.");
                operation(proxy);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try { if (!string.IsNullOrWhiteSpace(username)) ExecuteLobby(p => p.Logout(username)); } catch { }
            ClosePrivateChatWindows();
            AbortConnection();
        }

        private void ClosePrivateChatWindows()
        {
            foreach (var chatWindow in dmWindows.Values.ToList())
            {
                try { chatWindow.Close(); } catch { }
            }
            dmWindows.Clear();
        }

        private void AbortConnection()
        {
            try { (proxy as ICommunicationObject)?.Abort(); } catch { }
            try { factory?.Abort(); } catch { }
            proxy = null;
            factory = null;
        }

        //===== Connection =====
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsAlive(proxy))
            {
                MessageBox.Show("You are already logged in. Log out before switching users.");
                return;
            }
            username = UsernameTextBox.Text.Trim();
            if (username.Length < 3 || username.Length > 24 ||
                username.Any(c => !(char.IsLetterOrDigit(c) || c == '_' || c == '-')))
            {
                MessageBox.Show("Username must be 3–24 characters and contain only letters, numbers, hyphens or underscores.");
                return;
            }

            try
            {
                ReconnectProxy();
                currentRoom = null;
                SetAuthenticatedControls(true);
                RefreshRooms();
                ClearFlow(ChatBox);
                SetRoomChatEnabled(false);
                CurrentRoomHeader.Text = "No room selected";
            }
            catch (Exception ex)
            {
                    AbortConnection();
                username = null;
                currentRoom = null;
                SetAuthenticatedControls(false);
                MessageBox.Show($"Login error: {ex.Message}");
            }
        }

        public InterfaceLobbyDuplex ReconnectProxy()
        {
            var binding = new NetTcpBinding
            {
                Security = { Mode = SecurityMode.None },
                MaxReceivedMessageSize = 6 * 1024 * 1024,
                MaxBufferSize = 6 * 1024 * 1024,
                ReaderQuotas =
                {
                    MaxArrayLength = 6 * 1024 * 1024,
                    MaxBytesPerRead = 64 * 1024,
                    MaxDepth = 64,
                    MaxNameTableCharCount = 64 * 1024,
                    MaxStringContentLength = 1024 * 1024
                },
                ReceiveTimeout = TimeSpan.FromMinutes(10),
                SendTimeout = TimeSpan.FromMinutes(2),
                OpenTimeout = TimeSpan.FromSeconds(30),
                CloseTimeout = TimeSpan.FromSeconds(10)
            };
            AbortConnection();
            try
            {
                var endpoint = new EndpointAddress("net.tcp://localhost:8001/LobbyService/Duplex");
                factory = new DuplexChannelFactory<InterfaceLobbyDuplex>(new InstanceContext(this), binding, endpoint);
                factory.Open();
                proxy = factory.CreateChannel();
                (proxy as ICommunicationObject).Open();
                // A false Register result means the name already exists; Login is the
                // authoritative check for a new active session.
                ExecuteLobby(p => p.Register(username));
                bool loggedIn = ExecuteLobby(p => p.Login(username));
                if (!loggedIn)
                    throw new InvalidOperationException("Login was rejected. The username may already be active in another session.");
                return proxy;
            }
            catch
            {
                AbortConnection();
                throw;
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try { if (!string.IsNullOrWhiteSpace(username)) ExecuteLobby(p => p.Logout(username)); }
            catch (Exception ex) { MessageBox.Show("Logout warning: " + ex.Message); }
            CloseConnection();
            ClosePrivateChatWindows();
            username = null;
            currentRoom = null;
            RoomListBox.ItemsSource = null;
            PlayerListBox.ItemsSource = null;
            ClearFlow(ChatBox);
            CurrentRoomHeader.Text = "No room selected";
            SetRoomChatEnabled(false);
            SetAuthenticatedControls(false);
        }

        private void CloseConnection()
        {
            lock (proxyGate)
            {
                try { (proxy as ICommunicationObject)?.Close(); } catch { (proxy as ICommunicationObject)?.Abort(); }
                try { factory?.Close(); } catch { factory?.Abort(); }
                proxy = null;
                factory = null;
            }
        }

        //=====rooms=====
        private void RefreshRooms()
        {
            try
            {
                if (!IsAlive(proxy)) { 
                    MessageBox.Show("Not connected. Please Login first"); 
                    return; 
                }
                RoomListBox.ItemsSource = ExecuteLobby(p => p.GetAllRooms());
                UpdateJoinRoomButton();
            }
            catch (Exception ex) { 
                MessageBox.Show("Rooms load failed: " + ex.Message); 
            }
        }
        private void RefreshPlayers(string roomName)
        {
            try
            {
                if (!IsAlive(proxy)) { 
                    MessageBox.Show("Not connected. Please Login first"); 
                    return; 
                }
                var players = ExecuteLobby(p => p.GetPlayersInRoom(roomName));
                PlayerListBox.ItemsSource = ExcludeCurrentPlayer(players, username);
                ClosePrivateChatsForMissingPlayers(players);
                SetRoomChatEnabled(true);
            }
            catch (Exception ex) { 
                MessageBox.Show("Players load failed: " + ex.Message); 
            }
        }

        private void CreateRoomButton_Click(object sender, RoutedEventArgs e)
        {
            string roomName = NewRoomTextBox.Text.Trim();
            if (string.IsNullOrEmpty(roomName)) 
                return;

            try
            {
                if (!IsAlive(proxy)) { 
                    MessageBox.Show("Not connected. Please Login first"); 
                    return; 
                }
                if (ExecuteLobby(p => p.CreateRoom(roomName))) RoomListBox.SelectedItem = roomName;
                else MessageBox.Show("Room name is invalid or already exists. Use 2–40 letters, numbers, spaces, hyphens or underscores.");
            }
            catch (Exception ex) { MessageBox.Show("Create room failed: " + ex.Message); }
        }

        private void JoinRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (RoomListBox.SelectedItem is string roomName)
            {
                try
                {
                    if (!IsAlive(proxy)) { MessageBox.Show("Not connected. Please Login first"); 
                        return; 
                    }
                    if (ExecuteLobby(p => p.JoinRoom(username, roomName)))
                    {
                        currentRoom = roomName;
                        RoomListBox.SelectedItem = roomName;
                        LoadRoomContext(roomName);
                        MessageBox.Show($"Joined room {roomName}");
                    }
                    else MessageBox.Show("Join failed. Check that the room exists and that you have left your previous room.");
                }
                catch (Exception ex) { 
                    MessageBox.Show("Join failed: " + ex.Message); 
                }
            }
            else MessageBox.Show("Select a room first");
        }

        private void LeaveRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentRoom)) { MessageBox.Show("You are not in a room"); 
                return; 
            }

            try
            {
                if (!IsAlive(proxy)) { 
                    MessageBox.Show("Not connected. Please Login first");
                    return; 
                }
                ExecuteLobby(p => p.LeaveRoom(username, currentRoom));
                ClosePrivateChatWindows();
                currentRoom = null;
                RoomListBox.SelectedItem = null;
                LoadRoomContext(null);
            }
            catch (Exception ex) { 
                MessageBox.Show("Leave failed: " + ex.Message); 
            }
        }

        private void LoadRoomContext(string roomName)
        {
            CurrentRoomHeader.Text = string.IsNullOrEmpty(roomName) ? "No room selected" : $"Room: {roomName}";
            ClearFlow(ChatBox);

            if (!string.IsNullOrEmpty(roomName))
            {
                try
                {
                    if (!IsAlive(proxy)) { 
                        AppendTextLine(ChatBox, "(Not connected)"); 
                        return; 
                    }
                    var roomMessages = ExecuteLobby(p => p.GetMessages(roomName)).OrderBy(m => m.Timestamp).ToList();
                    foreach (var m in roomMessages)
                        AppendTextLine(ChatBox, $"[{m.Timestamp:t}] {m.Sender}: {m.Content}");
                    foreach (var file in ExecuteLobby(p => p.GetSharedFiles(roomName)).OrderBy(f => f.Timestamp))
                    {
                        if (!string.IsNullOrWhiteSpace(file.FileId))
                        {
                            var notice = new SharedContracts.SharedFileNotice
                            {
                                FileId = file.FileId, Room = file.Room, Sender = file.Sender,
                                FileName = file.FileName, Timestamp = file.Timestamp
                            };
                            AppendDownloadLink(ChatBox, "[File] by " + file.Sender + ": ", notice);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendTextLine(ChatBox, $"(Could not load history: {ex.Message})");
                }

                RefreshPlayers(roomName);
                SetRoomChatEnabled(true);
            }
            else
            {
                PlayerListBox.ItemsSource = null;
                SetRoomChatEnabled(false);
                UpdateJoinRoomButton();
            }
        }

        private void RoomListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateJoinRoomButton();
            if (RoomListBox.SelectedItem is string room)
            {
                if (room == currentRoom) LoadRoomContext(room);
                else
                {
                    CurrentRoomHeader.Text = $"Room: {room} (not joined)";
                    SetRoomChatEnabled(false);
                    PlayerListBox.ItemsSource = null;
                    ClearFlow(ChatBox);
                    UpdateJoinRoomButton();
                }
            }
            else
            {
                CurrentRoomHeader.Text = "No room selected";
                SetRoomChatEnabled(false);
                PlayerListBox.ItemsSource = null;
                ClearFlow(ChatBox);
                UpdateJoinRoomButton();
            }
        }

        private void RefreshPlayersButton_Click(object sender, RoutedEventArgs e)
        { 
            if (!string.IsNullOrEmpty(currentRoom)) RefreshPlayers(currentRoom); 
        }

        //===== Room Chat =====
        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentRoom)) { 
                MessageBox.Show("Join a room first"); 
                return; 
            }

            var text = MessageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) 
                return;

            try
            {
                if (!IsAlive(proxy)) { 
                    MessageBox.Show("Not connected. Please Login first"); 
                    return; 
                }
                ExecuteLobby(p => p.SendRoomMessage(new RoomMessage
                {
                    Room = currentRoom,
                    Sender = username,
                    Content = text,
                    Timestamp = DateTime.Now
                }));
                MessageTextBox.Clear();
            }
            catch (Exception ex) { MessageBox.Show("Send failed: " + ex.Message); }
        }

        //===== File Share =====
        private void ShareRoomFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentRoom)) { 
                MessageBox.Show("Join a room first"); 
                return; 
            }

            var open = new OpenFileDialog { Title = $"Share file to {currentRoom}", Filter = "Supported files|*.txt;*.png;*.jpg;*.jpeg;*.gif;*.bmp|Text files|*.txt|Image files|*.png;*.jpg;*.jpeg;*.gif;*.bmp" };
            if (open.ShowDialog() == true)
            {
                try
                {
                    var selectedFile = new FileInfo(open.FileName);
                    if (selectedFile.Length == 0 || selectedFile.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Choose a non-empty file no larger than 5 MB.");
                        return;
                    }
                    string extension = selectedFile.Extension.ToLowerInvariant();
                    if (!(extension == ".txt" || extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".gif" || extension == ".bmp"))
                    {
                        MessageBox.Show("Only text and image files can be shared.");
                        return;
                    }
                    byte[] data = File.ReadAllBytes(open.FileName);

                    if (!IsAlive(proxy)) { MessageBox.Show("Not connected. Please Login first"); 
                        return; 
                    }
                    ExecuteLobby(p => p.ShareFile(new SharedFile
                    {
                        Room = currentRoom,
                        Sender = username,
                        FileName = System.IO.Path.GetFileName(open.FileName),
                        FileData = data,
                        Timestamp = DateTime.Now
                    }));
                }
                catch (Exception ex) { MessageBox.Show("Share failed: " + ex.Message); }
            }
        }

        private void AppendDownloadLink(System.Windows.Controls.RichTextBox rtb, string prefixText, SharedContracts.SharedFileNotice notice)
        {
            var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 6) };
            if (!string.IsNullOrEmpty(prefixText)) paragraph.Inlines.Add(new Run(prefixText));

            var link = new Hyperlink(new Run(notice.FileName))
            {
                ToolTip = "Click to download this shared file",
                Cursor = System.Windows.Input.Cursors.Hand,
                Foreground = Brushes.RoyalBlue,
                TextDecorations = TextDecorations.Underline,
                Focusable = true
            };
            link.Click += (s, e) =>
            {
                e.Handled = true;
                try
                {
                    if (!IsAlive(proxy)) { 
                        MessageBox.Show("Not connected. Please Login first"); 
                        return; }

                    var dlg = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = notice.FileName,
                        Title = $"Save file from room {notice.Room}"
                    };
                    if (dlg.ShowDialog() == true)
                    {
                        byte[] downloaded = ExecuteLobby(p => p.DownloadSharedFile(notice.FileId));
                        File.WriteAllBytes(dlg.FileName, downloaded);
                        MessageBox.Show("Download complete", "File", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex) { MessageBox.Show("Download failed: " + ex.Message); }
            };

            paragraph.Inlines.Add(link);
            paragraph.Inlines.Add(new Run($"  ({notice.Timestamp:t})"));
            rtb.Document.Blocks.Add(paragraph);
            rtb.ScrollToEnd();
        }

        //image check & preview 
        private static bool IsImageFileName(string name)
        {
            try
            {
                var ext = System.IO.Path.GetExtension(name)?.ToLowerInvariant();
                return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp";
            }
            catch { return false; }
        }

        private void AppendImagePreview(System.Windows.Controls.RichTextBox rtb, byte[] bytes, string caption)
        {
            var img = BuildImageFromBytes(bytes, 360);
            var p = new Paragraph { Margin = new Thickness(0, 4, 0, 10) };
            if (!string.IsNullOrEmpty(caption))
                p.Inlines.Add(new Run(caption + " "));
            p.Inlines.Add(new InlineUIContainer(img));
            rtb.Document.Blocks.Add(p);
            rtb.ScrollToEnd();
        }

        //===== private chat =====
        private void OpenPrivateChatButton_Click(object sender, RoutedEventArgs e)
        {
            string otherUser = null;
            if (PlayerListBox.SelectedItem is string selected &&
                !string.Equals(selected, username, StringComparison.OrdinalIgnoreCase))
                otherUser = selected;

            if (string.IsNullOrEmpty(otherUser) ||
                string.Equals(otherUser, username, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Select a different player for private chat.");
                return;
            }

            if (!dmWindows.TryGetValue(otherUser, out var chatWindow) || chatWindow == null)
            {
                chatWindow = new PrivateChatWindow(this, username, otherUser);
                dmWindows[otherUser] = chatWindow;

                chatWindow.Closed += (s, args) =>
                {
                    if (dmWindows.ContainsKey(otherUser)) dmWindows.Remove(otherUser);
                };

                chatWindow.Show();
            }
            else
            {
                chatWindow.Activate();
            }
        }



        //===== Callbacks =====
        public void OnRoomMessageReceived(RoomMessage msg)
        {
            try
            {
                if (!string.IsNullOrEmpty(currentRoom) && msg.Room == currentRoom)
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (string.Equals(currentRoom, msg.Room, StringComparison.OrdinalIgnoreCase))
                            AppendTextLine(ChatBox, $"[{msg.Timestamp:t}] {msg.Sender}: {msg.Content}");
                    }));
            }
            catch (Exception ex) { 
                MessageBox.Show("Callback fail: " + ex.Message); 
            }
        }

        public void OnRoomRosterChanged(string roomName, List<string> players)
        {
            try
            {
                if (!string.IsNullOrEmpty(currentRoom) &&
                    string.Equals(roomName, currentRoom, StringComparison.OrdinalIgnoreCase))
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (string.Equals(currentRoom, roomName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(RoomListBox.SelectedItem as string, roomName, StringComparison.OrdinalIgnoreCase))
                        {
                            PlayerListBox.ItemsSource = ExcludeCurrentPlayer(players, username);
                            ClosePrivateChatsForMissingPlayers(players);
                            SetRoomChatEnabled(true);
                        }
                    }));
            }
            catch (Exception ex) { 
                MessageBox.Show("Callback fail: " + ex.Message); 
            }
        }

        public void OnRoomsUpdated(List<string> rooms)
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    string keepSelection = currentRoom ?? (RoomListBox.SelectedItem as string);
                    RoomListBox.ItemsSource = rooms;
                    if (!string.IsNullOrEmpty(currentRoom) && !rooms.Contains(currentRoom))
                    {
                        currentRoom = null;
                        LoadRoomContext(null);
                    }
                    else if (!string.IsNullOrEmpty(keepSelection) && rooms.Contains(keepSelection))
                    {
                        RoomListBox.SelectedItem = keepSelection;
                    }
                    UpdateJoinRoomButton();
                }));
            }
            catch (Exception ex) { 
                MessageBox.Show("Callback fail: " + ex.Message); 
            }
        }

        public void OnPrivateMessageReceived(PrivateMessage msg)
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    string otherUser = string.Equals(msg.Sender, username, StringComparison.OrdinalIgnoreCase)
                        ? msg.Receiver : msg.Sender;
                    if (string.IsNullOrWhiteSpace(otherUser) ||
                        string.Equals(otherUser, username, StringComparison.OrdinalIgnoreCase)) return;

                    if (!dmWindows.TryGetValue(otherUser, out var w) || w == null)
                    {
                        w = new PrivateChatWindow(this, username, otherUser);
                        dmWindows[otherUser] = w;

                        w.Closed += (s, args) =>
                        {
                            if (dmWindows.ContainsKey(otherUser)) dmWindows.Remove(otherUser);
                        };
                        w.Show();
                    }
                    else
                    {
                        w.AppendPrivateMessage(msg);
                        w.Activate();
                    }
                }));
            }
            catch (Exception ex) { 
                MessageBox.Show("Callback fail: " + ex.Message); 
            }
        }

        public void OnFileSharedNotice(SharedContracts.SharedFileNotice notice)
        {
            try
            {
                if (!string.IsNullOrEmpty(currentRoom) && notice.Room == currentRoom)
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (!string.Equals(currentRoom, notice.Room, StringComparison.OrdinalIgnoreCase)) return;
                        AppendDownloadLink(ChatBox, $"[File] by {notice.Sender}: ", notice);

                        if (IsImageFileName(notice.FileName) && IsAlive(proxy))
                        {
                            try
                            {
                                byte[] bytes;
                                bytes = ExecuteLobby(p => p.DownloadSharedFile(notice.FileId));
                                AppendImagePreview(ChatBox, bytes, "(preview)");
                            }
                            catch (Exception ex2)
                            {
                                AppendTextLine(ChatBox, $"(Image preview failed: {ex2.Message})");
                            }
                        }
                    }));
            }
            catch (Exception ex) { MessageBox.Show("Callback fail: " + ex.Message); }
        }

        //unused in this UI
        public void OnLobbyMessageReceived(RoomMessage msg) { }
        public void OnLobbyFileSharedNotice(SharedContracts.SharedFileNotice notice) { }
        public void OnFileShared(SharedFile file) { }
        public void OnLobbyFileShared(SharedFile file) { }
    }
}
