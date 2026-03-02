using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebBogglerCommonTypes
{

	public class Dices : List<Dice>
	{
		public Dice GetDiceAt(int row, int col)
		{
			return this[row * 5 + col];
		}
	}
}
