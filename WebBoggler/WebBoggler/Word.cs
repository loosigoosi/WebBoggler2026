
using System.Windows;

namespace WebBogglerCommonTypes
{
    public partial class Word : WordBase
    {
        public Word() : base()
        {
            DicePath = new System.Collections.Generic.List<Dice>(); 
        }
        
        public string[] GetGridPathPolyline(double scaleX, double scaleY, Point origin)
        {
            if (base.DicePath.Count > 0) {
                string[] pathDef = new string[2];
                                 
                int i = 0;
                foreach (Dice dice in DicePath)
                {
                    Point p = new Point(origin.X + dice.Column * scaleX, origin.Y + dice.Row * scaleY);
                    string coords = p.X.ToString().Trim() + "," + p.Y.ToString().Trim();
                    if (i == 0) {
                        pathDef[1] = coords;
						pathDef[0] = "M ";
						pathDef[0] += coords;
                   } else { 

                        pathDef[0] += " L " + coords;
                   }
                    i++;
                }

                return pathDef; //_gridPathPolyline;
                
            }
            else
            {
                return null;
            }

        }

        public new Word Clone()
        {
            var word = new Word();
            foreach (var dice in base.DicePath)
            {
                word.AppendDiceLast(dice);
            }
            return word;
        }

     }
}
