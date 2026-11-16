using System;
using System.Runtime.Serialization;

namespace DatabaseLibrary.Models
{
    [DataContract]
    public class RoomMessage
    {
        [DataMember]
        public string Room
        {
            get;
            set;
        }
        [DataMember]
        public string Sender
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

        public RoomMessage() { }

        public RoomMessage(string room, string sender, string content, DateTime? timestamp = null)
        {
            Room = room;
            Sender = sender;
            Content = content;
            Timestamp = timestamp ?? DateTime.Now;
        }

        public override string ToString() => $"[{Timestamp:t}] {Sender}: {Content}";
    }
}
