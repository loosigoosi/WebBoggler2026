using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WebBoggler.SignalRServer.Models
{
    [DataContract]
    public class WordList
    {
        [DataMember(EmitDefaultValue = false)]
        public Word[]? Items { get; set; }
    }
}
