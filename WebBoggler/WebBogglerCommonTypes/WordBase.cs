using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace WebBogglerCommonTypes
{

	[DataContract(IsReference = true, Namespace = "WebBogglerCommonTypes")]
	public class WordBase
    {
        protected List<Dice> _dicePathList;
        public event ChangeEventHandler Change;
        public delegate void ChangeEventHandler(object sender, WordChangeEventArgs e);
        public bool Duplicated;
        public enum WordStatus
        {
            Null,
            NotValid,
            Valid
        }

		public class WordChangeEventArgs : EventArgs
        {
            public WordBase.WordStatus WordStatus;
        }


        public WordBase()
        {
            _dicePathList = new List<Dice>();
        }

        public static bool operator ==(WordBase w1, WordBase w2)
        {
            if ((w1.DicePath.Count() == w2.DicePath.Count()) & (w1.Text.Length == w2.Text.Length))
            {
                for (int i = 0; i <= w1.DicePath.Count() - 1; i++)
                {
                    if (!object.ReferenceEquals(w1.DicePath[i], w2.DicePath[i]))
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            return true;
        }
        public static bool operator !=(WordBase w1, WordBase w2)
        {
            if ((w1.DicePath.Count == w2.DicePath.Count()) & (w1.Text.Length == w2.Text.Length))
            {
                for (int i = 0; i <= w1.DicePath.Count() - 1; i++)
                {
                    var dicePath1 = w1.DicePath[i];
                    var dicePath2 = w2.DicePath[i];
                    if (!object.ReferenceEquals(dicePath1, dicePath2))
                    {
                        return true;
                    }
                }
            }
            else
            {
                return true;
            }
            return false;
        }

		public WordStatus Status
        {
            get
            {
                WordStatus functionReturnValue = default(WordStatus);
                switch (Text.Length)
                {
                    case 0:
                        functionReturnValue = WordStatus.Null;
                        break;
                    case 4:

                        functionReturnValue = WordStatus.Valid;
                        break;
                    default:
                        functionReturnValue = WordStatus.NotValid;
                        break;
                }
                return functionReturnValue;
            }
        }

		public int Score
        {
            get
            {
                switch (Text.Length)
                {
                    case 8:
                        return 11;
                    case 7:
                        return 5;
                    case 6:
                        return 3;
                    case 5:
                        return 2;
                    case 4:
                        return 1;
                    default:
                        return 0;
                }
            }
        }


		string Text
        {
            get
            {
                if (_dicePathList.Count() > 0)
                {
                    string strText = "";

                    foreach (var node in _dicePathList)
                    {
                        strText += node.Letter;
                    }
                    return strText;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

		[DataMember]
		public List<Dice> DicePath
        {
            get { return _dicePathList; }
            set { _dicePathList = value; }
        }

		public new string ToString
        {
            get { return this.Text; }
        }


        public void AppendDiceFirst(Dice dice)
        {
            _dicePathList.Insert(0,(dice));
            WordChangeEventArgs e = new WordChangeEventArgs();
            e.WordStatus = Status;
            if (Change != null)
            {
                Change(this, e);
            }
        }

        public void AppendDiceLast(Dice dice)
        {
            _dicePathList.Add(dice);
            WordChangeEventArgs e = new WordChangeEventArgs();
            e.WordStatus = Status;
            if (Change != null)
            {
                Change(this, e);
            }
        }

        public void RemoveDice(Dice dice)
        {
            _dicePathList.Remove(dice);
            if (Change != null)
            {
                Change(this, new WordChangeEventArgs());
            }
        }

        public bool Contains(Dice dice)
        {
            return _dicePathList.Contains(dice);
        }

        public bool IsFirst(Dice dice)
        {
            return object.ReferenceEquals(dice, _dicePathList[0]);
        }

        public bool IsLast(Dice dice)
        {
            return object.ReferenceEquals(dice, _dicePathList[_dicePathList.Count -1]);
        }

        public bool IsEmpty()
        {
            return _dicePathList.Count == 0;
        }

        public void Clear()
        {
            _dicePathList.Clear();

            WordChangeEventArgs e = new WordChangeEventArgs();
            e.WordStatus = Status;
            if (Change != null)
            {
                Change(this, e);
            }
        }

        public WordBase Clone()
        {
            WordBase word = new WordBase();
            foreach (var dice in _dicePathList)
            {
                word.AppendDiceLast(dice);
            }
            return word;
        }
    }

}
