using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

using System.Windows.Media;
using System.Windows.Controls;
using WebBogglerCommonTypes;

namespace WebBoggler
{
    public sealed partial class MainPage : Page
    {
        private Desk _Desk;
        private Brush _cmdAddBrush;


        public MainPage()
        {
           this.InitializeComponent();

            _Desk = new Desk(bgBoard, hcHourglass,
                            panGameStatus, panGameControls, txtGameStatus, txtGameSubStatus, 
                            txtWordCount, txtFoundWordCount, txtInputWord, 
                            cmdAddWord, lbWordList, panPlayersWordList, panLocalWordList, lbPlayersWordList, lbSolution,   
                            lbPlayersList,spLogin, txtUserName,  
                            cmdJoin, sfxSoundPlayer);

            _cmdAddBrush = cmdAddWord.Foreground;

            bgBoard.LetterTapped += _Desk.LetterTappedHandler;

            base.Loaded += MainPage_Loaded; ; 
            //this.cmdTest.Click += new RoutedEventHandler(this.CmdTest_Click);
			this.cmdJoin.Click +=  new RoutedEventHandler(this.CmdJoin_Click);
            this.cmdAddWord.Click += new RoutedEventHandler(this.CmdAddWord_Click);
            this.cmdClearWord.Click += new RoutedEventHandler(this.CmdClearWord_Click);
            this.txtUserName.TextChanged += TxtUserName_TextChanged;
            this.chkSounds.Unchecked += ChkSounds_Unchecked;
            this.chkSounds.Checked += ChkSounds_Checked;
		}

        #region "Event Handlers"


        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            _Desk.Register();
        }


        private async void CmdJoin_Click(object sender, RoutedEventArgs e)
        {
            if ((cmdJoin.Tag).ToString() == "Join")
            {
                await _Desk.Join(txtUserName.Text);
            }
            else
            {
                _Desk.LeaveAsync();
            }
        }


        private void ChkSounds_Checked(object sender, RoutedEventArgs e)
        {
            sfxSoundPlayer.Mute = false;
        }

        private void ChkSounds_Unchecked(object sender, RoutedEventArgs e)
        {
            sfxSoundPlayer.Mute = true;
        }

        private void CmdClearWord_Click(object sender, RoutedEventArgs e)
        {
            _Desk.ClearWordEntry();
        }

        private async void CmdAddWord_Click(object sender, RoutedEventArgs e)
        {
            var result = await _Desk.ValidateWordAsync(_Desk.WordEntry.WordText);
            if (result)
            {
                cmdAddWord.Foreground = _cmdAddBrush; //green
                cmdAddWord.Content = cmdAddWord.Tag.ToString();
                _Desk.AddEntryToWordList();
                _Desk.WordEntry.Clear();
            }
            else
            {
                cmdAddWord.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 200, 0, 0)); //red
                cmdAddWord.Content = "Non valida";
            }

        }

        private void TxtUserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (((TextBox)sender).Text.Length >= 3)
            {
                cmdJoin.IsEnabled = true;
            }
            else
            {
                cmdJoin.IsEnabled = false;
            }

        }


        private async void CmdTest_Click(object sender, RoutedEventArgs e)
        {
            _Desk.WordEntry.Clear();
            await _Desk.GetBoardFromServerAsync("it-IT");
            _Desk.Echo(DateTime.Now.ToString());
            try
            {
                if (_Desk._WebSocket != null)
                {
                    await _Desk._WebSocket.DisconnectAsync();
                }
            }
            catch { }
        }

		#endregion
	}

}



