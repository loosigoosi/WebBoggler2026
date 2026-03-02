
//using System.Windows.Foundation;
using System.Windows;
//using System.Windows.Xaml;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WebBogglerCommonTypes;

namespace WebBoggler
{

    public sealed partial class BoardGrid : UserControl
    {
        private Rectangle _rectSelector;

        // Costruttore
        public BoardGrid()
        {
            this.InitializeComponent();

            for (int i = 1; i <= 25; i++)
			{
				Rectangle rectButton =  (Rectangle)base.FindName("recButton" + i.ToString());
                rectButton.Tapped += lblLetter_Tapped;
                rectButton.MouseEnter += rectButton_PointerEntered;
                rectButton.MouseLeave += rectButton_PointerExited; 
			}

		}

        internal void HideCover()
        {
            rectCover.Visibility = Visibility.Collapsed;
        }

        internal void ShowCover()
        {
            rectCover.Visibility = Visibility.Visible;

        }
        //internal void StartShakeBoardAnimation()
        //{
        //    stbShakeBoardAnimation.Begin();
        //    stbBeginShake.Begin();

        //}
        internal Visibility CoverVisibility
        {
            get { return rectCover.Visibility; }
            //set { rectCover.Visibility = value; }
        }


        #region "Subs & Functions"


        internal void SetBoard(WebBogglerCommonTypes.Dices dices)
        {
            this.cnvWordPaths.Children.Clear();
            this.rectCover.Visibility = Visibility.Visible;
            for (int i = 1; i <= 25; i++)
            {
				WebBogglerCommonTypes.Dice dice = dices[i - 1];
                TextBlock block = null;
                string str = dice.Letter;
                block = (TextBlock)base.FindName("lblLetter" + i.ToString());
                block.Text = str;
                if ((((str == "Z") | (str == "M")) | (str == "N")) | (str == "W")) {
                    block.TextDecorations = System.Windows.TextDecorations.Underline ;
                } else {
                    block.TextDecorations = null;
                }
                RotateTransform transform = new RotateTransform();
                transform.Angle = dice.Rotation;
                block.RenderTransformOrigin = new Point(0.5, 0.5);
                block.RenderTransform = transform;
            }
        }
  
        internal void ClearPaths()
        {
            cnvWordPaths.Children.Clear();
        }


        internal void DrawWordPaths(Word word)
        {
            SolidColorBrush brush = new SolidColorBrush();
            SolidColorBrush brush2 = new SolidColorBrush();
            Color color = new Color
			{ 
                R = 255,
                G = 230,
                B = 70,
                A = 255
            };
            brush.Color = color;
            brush.Opacity = 0.65;
            double scaleX = this.cnvWordPaths.ActualWidth / 5.0;
            double scaleY = this.cnvWordPaths.ActualHeight / 5.0;
            string[] item = word.GetGridPathPolyline(scaleX, scaleY, new Point(scaleX / 2.0, scaleY / 2.0));
            this.ClearPaths();
            Path path = new Path
            {
                Stroke = brush,
                StrokeThickness = 8,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                Fill = null
            };
            PathFigure figure = new PathFigure();
			////figure.Segments = new PathSegmentCollection();
			////figure.Segments.Add(item);
			////PathGeometry geometry = new PathGeometry();
			////geometry.Figures = new PathFigureCollection();
			////geometry.Figures.Add(figure);
			////path.Data = geometry;
			
			path.Data = (Geometry)DotNetForHtml5.Core.TypeFromStringConverters.ConvertFromInvariantString(typeof(Geometry), item[0]);
            this.cnvWordPaths.Children.Add(path);

            //Disegna cerchietto di inizio parola
            color = new Color
            {
                R = 0255,
                G =230,
                B = 70,
                A = 255
            };

            brush2.Color = color;
            brush2.Opacity = 0.75;
            var ellipse = new Ellipse();
            ellipse.StrokeThickness = 3.0;
            ellipse.Stroke = brush2;
            ellipse.Fill = brush2;
            ellipse.Height = 35.0;
            ellipse.Width = 35.0;
            ellipse.Visibility = Visibility.Collapsed;

            Point p = new Point(double.Parse(item[1].Split(",".ToCharArray())[0]), double.Parse(item[1].Split(",".ToCharArray())[1]));
            var t= new TranslateTransform();
            t.X = p.X - (ellipse.Width / 2.0);
            t.Y = p.Y - (ellipse.Height / 2.0);

            this.cnvWordPaths.Children.Add(ellipse);

            ellipse.RenderTransform = t;
            ellipse.Visibility = Visibility.Visible;
            //
        }

        #endregion


        #region "Controls Event Handlers"

        public event LetterTappedEventHandler LetterTapped;
        public delegate void LetterTappedEventHandler(object sender, int index);
        private void lblLetter_Tapped(object sender, TappedRoutedEventArgs e)
        {
            int index = int.Parse(((Rectangle)sender).Name.Substring(9));
            if (LetterTapped != null) {
                LetterTapped(sender, index);
            }
        }

        public event LetterDoubleTappedEventHandler LetterDoubleTapped;
        public delegate void LetterDoubleTappedEventHandler(object sender, int index);
        private void lblLetter_DoubleTapped(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(((Rectangle)sender).Name.Substring(9));
            if (LetterDoubleTapped != null) {
                LetterDoubleTapped(sender, index);
            }
        }

        public event CoverTappedEventHandler CoverTapped;
        public delegate void CoverTappedEventHandler();
        private void rectCover_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (CoverTapped != null) {
                CoverTapped();
            }
        }

        private void rectButton_PointerExited(object sender, RoutedEventArgs e)
        {
            SolidColorBrush brush = new SolidColorBrush();
            var color = new Color
            {
                R = 200,
                G = 0,
                B = 0,
                A = 255
            };

            brush.Color = color;
            brush.Opacity = 0;

            ((Rectangle)sender).Stroke = brush;

        }

        private void rectButton_PointerEntered(object sender, RoutedEventArgs e)
        {


            SolidColorBrush brush = new SolidColorBrush();
            var color = new Color
            {
                R = 200,
                G = 0,
                B = 0,
                A = 255
            };

            brush.Color = color;
            brush.Opacity = 0.75;

            ((Rectangle)sender).Stroke = brush;
        }


        #endregion





    }
}

