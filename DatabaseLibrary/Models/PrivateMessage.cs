using System;
using System.Runtime.Serialization;

namespace DatabaseLibrary.Models
{
    [DataContract]
    public class PrivateMessage
    {
        [DataMember]
        public string Sender
        {
            get;
            set;
        }
        [DataMember]
        public string Receiver
        {
            get;
            set;
        }
        [DataMember]
        public string Content
        {
            get;
            set;
        }
        [DataMember]
        public DateTime Timestamp
        {
            get;
            set;
        }

        public PrivateMessage() { }

        public PrivateMessage(string sender, string receiver, string content, DateTime? timestamp = null)
        {
            Sender = sender;
            Receiver = receiver;
            Content = content;
            Timestamp = timestamp ?? DateTime.Now;
        }

        public override string ToString() => $"[{Timestamp:t}] {Sender} → {Receiver}: {Content}";
    }
}
