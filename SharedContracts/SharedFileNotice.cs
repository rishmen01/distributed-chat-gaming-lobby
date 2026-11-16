using System;
using System.Runtime.Serialization;

namespace SharedContracts
{
    [DataContract]
    public class SharedFileNotice
    {
        [DataMember] public string FileId { 
            get; 
            set; 
        }
        [DataMember] public string Room { 
            get; 
            set; 
        }
        [DataMember] public string Sender { 
            get; 
            set; 
        }
        [DataMember] public string FileName { 
            get; 
            set; 
        }
        [DataMember] public long Length { 
            get; 
            set; 
        }
        [DataMember] public DateTime Timestamp { 
            get; 
            set; 
        }
    }
}
