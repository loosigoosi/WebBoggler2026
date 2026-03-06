using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Linq;
using BigBoggler.Models;

namespace WebBoggler
{
    // Estende WordBase da BigBoggler.Shared con funzionalità UI WPF
    public class Word : WordBase
    {
        public Word() : base()
        {
        }

        public string[] GetGridPathPolyline(double scaleX, double scaleY, Point origin)
        {
            if (DicePath == null || DicePath.Count == 0)
                return null;

            string[] pathDef = new string[2];
            int i = 0;
            
            foreach (var dice in DicePath)
            {
                Point p = new Point(origin.X + dice.Column * scaleX, origin.Y + dice.Row * scaleY);
                string coords = p.X.ToString().Trim() + "," + p.Y.ToString().Trim();
                
                if (i == 0)
                {
                    pathDef[1] = coords;
                    pathDef[0] = "M " + coords;
                }
                else
                {
                    pathDef[0] += " L " + coords;
                }
                i++;
            }

            return pathDef;
        }

        public Polyline GetPolyline(double rectWidth, double rectHeight, Point offset)
        {
            if (DicePath == null || DicePath.Count == 0)
                return null;

            Polyline polyline = new Polyline
            {
                Stroke = Brushes.Yellow,
                StrokeThickness = 4
            };

            foreach (var dice in DicePath)
            {
                Point p = new Point(
                    offset.X + dice.Column * rectWidth + rectWidth / 2,
                    offset.Y + dice.Row * rectHeight + rectHeight / 2
                );
                polyline.Points.Add(p);
            }

            return polyline;
        }
    }
}
