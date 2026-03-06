using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace WebBogglerCommonTypes
{
	[DataContract(IsReference = true, Namespace = "WebBogglerCommonTypes")]
	public class Dice
	{

		private int _index;
		//in numero di rotazioni di 90°
		private int _faceRotation;
		private string _letter;

		[DataMember]
		public int Index
		{
			get { return _index; }
			set { _index = value; }
		}

		[DataMember]
		public int Rotation
		{
			get { return _faceRotation; }
			set { _faceRotation = value; }
		}

		[DataMember]
		public string Letter
		{
			get { return _letter; }
			set { _letter = value; }
		}

		[DataMember]
		public int Row
		{
			get { return (_index / 5); }
		}

		[DataMember]
		public int Column
		{
			get { return _index % 5; }
		}
	}
}
