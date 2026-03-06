using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WebBoggler.SignalRServer.Models
{
    [DataContract]
    public class Players
    {
        [DataMember(EmitDefaultValue = false)]
        public Player[]? Items { get; set; }
    }
}
