using System;
using System.Runtime.Serialization;

namespace DatabaseLibrary.Models
{
    [DataContract]
    public class SharedFile
    {
        [DataMember]
        public string FileId { 
            get; 
            set; }

        [DataMember]
        public string Room { 
            get; 
            set; }

        [DataMember]
        public string Sender { 
            get; 
            set; }

        [DataMember]
        public string FileName { 
            get; 
            set; }

        [DataMember]
        public byte[] FileData { 
            get; 
            set; }

        [DataMember]
        public DateTime Timestamp { 
            get; 
            set; }

        public SharedFile() { }

        public SharedFile(string room, string sender, string fileName, byte[] fileData, DateTime? timestamp = null, string fileId = null)
        {
            FileId = fileId;
            Room = room;
            Sender = sender;
            FileName = fileName;
            FileData = fileData;
            Timestamp = timestamp ?? DateTime.Now;
        }

        public override string ToString() => $"{FileName} (by {Sender} at {Timestamp:t})";
    }
}
