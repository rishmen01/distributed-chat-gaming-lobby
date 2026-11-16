using DatabaseLibrary.Models;
using SharedContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GamingLobbyDuplexClient
{
    public partial class PrivateChatWindow : Window
    {
        private readonly string myUser;
        private readonly string otherUser;
        private readonly MainWindow ownerMain;
        private readonly HashSet<string> displayedMessageKeys = new HashSet<string>(StringComparer.Ordinal);

        public PrivateChatWindow(MainWindow ownerMain, string myUser, string otherUser)
        {
            InitializeComponent();
            this.ownerMain = ownerMain;
            this.myUser = myUser;
            this.otherUser = otherUser;
            Title = $"Private Chat with {otherUser}";
            Loaded += (_, __) => LoadHistorySafe();
        }

        private void LoadHistorySafe()
        {
            try
            {
                var history = ownerMain.ExecuteLobby(p => p.GetPrivateHistory(myUser, otherUser)).OrderBy(m => m.Timestamp);
                LoadHistory(history);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load private history: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadHistory(IEnumerable<PrivateMessage> msgs)
        {
            PrivateChatBox.Clear();
            displayedMessageKeys.Clear();
            foreach (var m in msgs)
            {
                if (m == null || !displayedMessageKeys.Add(MessageKey(m))) continue;
                PrivateChatBox.AppendText(FormatMessage(m));
            }
            PrivateChatBox.ScrollToEnd();
        }

        private string FormatMessage(PrivateMessage msg)
        {
            string displayName = string.Equals(msg.Sender, myUser, StringComparison.OrdinalIgnoreCase)
                ? "You" : msg.Sender;
            return $"[{msg.Timestamp:t}] {displayName}: {msg.Content}\n";
        }

        public void AppendPrivateMessage(PrivateMessage msg)
        {
            if (msg == null || !displayedMessageKeys.Add(MessageKey(msg))) return;
            PrivateChatBox.AppendText(FormatMessage(msg));
            PrivateChatBox.ScrollToEnd();
        }

        private static string MessageKey(PrivateMessage msg)
        {
            return msg.Sender + "|" + msg.Receiver + "|" +
                   msg.Timestamp.Ticks + "|" + (msg.Content ?? string.Empty);
        }

        private void SendPrivateButton_Click(object sender, RoutedEventArgs e)
        {
            var content = PrivateMessageTextBox.Text;
            if (string.IsNullOrWhiteSpace(content)) 
                return;

            var msg = new PrivateMessage
            {
                Sender = myUser,
                Receiver = otherUser,
                Content = content.Trim(),
                Timestamp = DateTime.Now
            };

            try
            {
                ownerMain.ExecuteLobby(p => p.SendPrivateMessage(msg));
                AppendPrivateMessage(msg);   
                PrivateMessageTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to send private message: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
