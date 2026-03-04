
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
			this.Loaded += BoardGrid_Loaded;
		}

		private void BoardGrid_Loaded(object sender, RoutedEventArgs e)
		{
			for (int i = 1; i <= 25; i++)
			{
				Rectangle rectButton = (Rectangle)base.FindName("recButton" + i.ToString());
				if (rectButton != null)
				{
					rectButton.MouseEnter += rectButton_PointerEntered;
					rectButton.MouseLeave += rectButton_PointerExited;
					rectButton.MouseLeftButtonDown += rectButton_MouseLeftButtonDown;
					rectButton_PointerExited(rectButton, null);
				}
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
			SolidColorBrush brush1 = new SolidColorBrush();
			SolidColorBrush brush2 = new SolidColorBrush();

			// Verdino come nel VB
			Color color1 = new Color
			{ 
				R = 187,
				G = 214,
				B = 10,
				A = 255
			};
			brush1.Color = color1;
			brush1.Opacity = 0.85;

			double rectWidth = this.cnvWordPaths.ActualWidth / 5.0;
			double rectHeight = this.cnvWordPaths.ActualHeight / 5.0;

			Polyline ply = word.GetPolyline(rectWidth, rectHeight, new Point(rectWidth / 2.0, rectHeight / 2.0));

			this.ClearPaths();

			if (ply != null)
			{
				// Linea continua come nel VB
				ply.Stroke = brush1;
				ply.StrokeThickness = 19;
				ply.StrokeEndLineCap = PenLineCap.Round;
				ply.StrokeLineJoin = PenLineJoin.Round;
				ply.StrokeStartLineCap = PenLineCap.Round;
				ply.StrokeDashCap = PenLineCap.Round;
				ply.Fill = null;

				this.cnvWordPaths.Children.Add(ply);

				// Cerchietto iniziale
				Color color2 = new Color
				{
					R = 187,
					G = 214,
					B = 10,
					A = 255
				};
				brush2.Color = color2;
				brush2.Opacity = 0.95;

				Ellipse circ = new Ellipse();
				circ.StrokeThickness = 3.0;
				circ.Stroke = brush2;
				circ.Fill = brush2;
				circ.Height = 50.0;
				circ.Width = 50.0;
				circ.Margin = new Thickness(
					ply.Points[0].X - circ.Width / 2.0, 
					ply.Points[0].Y - circ.Height / 2.0, 
					0, 
					0
				);

				this.cnvWordPaths.Children.Add(circ);
			}
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

        private void rectButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int index = int.Parse(((Rectangle)sender).Name.Substring(9));
            TextBlock letter = (TextBlock)base.FindName("lblLetter" + index.ToString());

            if (LetterTapped != null)
            {
                LetterTapped(sender, index);
            }
        }


        #endregion





    }
}

