using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebBogglerCommonTypes
{
	[DataContract(IsReference = true, Namespace = "WebBogglerCommonTypes")]
	public class Players : List<Player>
    {
    }
}
