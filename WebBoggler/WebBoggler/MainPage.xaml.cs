using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WebBogglerCommonTypes;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                            cmdJoin, cmdLeave, chkReady, spReadyPanel, sfxSoundPlayer);

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
            this.chkReady.Checked += chkReady_Checked;
            this.chkReady.Unchecked += chkReady_Unchecked;
            this.lbSolution.MouseDoubleClick  += LbSolution_MouseDoubleClick;
            this.cmdLeave.Click  += CmdLeave_MouseDoubleClick;

        }



        #region "Event Handlers"


        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            _Desk.Register();
            
        }


        private async void CmdJoin_Click(object sender, RoutedEventArgs e)
        {
                await _Desk.Join(txtUserName.Text);             
        }


/* Private Sub txtPlayerName_KeyUp(sender As Object, e As KeyEventArgs) Handles txtPlayerName.KeyUp

        If e.Key = Key.Enter And CType(sender, TextBox).Text.Length >= 3 Then cmdLogIn_Click(sender, e)
End Sub


Private Sub txtPlayerName_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtPlayerName.TextChanged

    cmdLogIn.IsEnabled = CType(sender, TextBox).Text.Length >= 3

End Sub

Private Sub cmdLogIn_Click(sender As Object, e As RoutedEventArgs) Handles cmdLogIn.Click

    App.GameEngine = New GameEngine(5, App.Language)
        App.GameEngine.PhotonClient.NickName = String.Format("{0,2:G2}.", (SupportClass.ThreadSafeRandom.Next() Mod 99)) & txtPlayerName.Text.Trim
*/

        private async void CmdLeave_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            await _Desk.LeaveAsync();
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
                sfxSoundPlayer.PlaySound(Sound.AddWord);
            }
            else
            {
                cmdAddWord.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 200, 0, 0)); //red
                cmdAddWord.Content = "Non valida";
                sfxSoundPlayer.PlaySound(Sound.Failure);
            }

        }

        private void txtUserName_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && cmdJoin.IsEnabled)
            {
                // Simula il click sul pulsante Join
                CmdJoin_Click(cmdJoin, new RoutedEventArgs());
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

		private void chkReady_Checked(object sender, RoutedEventArgs e)
		{
			// Invia "READY" al server
			_Desk._WebSocket?.Send("READY");
		}

		private void chkReady_Unchecked(object sender, RoutedEventArgs e)
			{
				// Invia "NOTREADY" al server
				_Desk._WebSocket?.Send("NOTREADY");
			}

        private void LbSolution_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Richiede al server la lista delle parole soluzioni e le visualizza in lbSolution. 
            _Desk.GetSolution();
        }


        #endregion
    }

}



