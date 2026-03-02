using System;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Net.WebSockets;

using WebBogglerCommonTypes;

namespace WebBoggler

{
    class Desk
    {

        private WebBogglerServer.ServiceWebBogglerClient _serviceClient;
		public CSHTML5.Extensions.WebSockets.ClientWebSocket _WebSocket;

        private DispatcherTimer _oneSecondTicker = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 1) };

		private Board _board;
		private BoardGrid _boardGrid;
        private HourglassControl _Hourglass;
        private StackPanel _gameControlsPanel;
        private StackPanel _gameStatusPanel;
        private TextBlock _gameStatusText;
        private TextBlock _gameSubStatusText;
        private TextBlock _inputWordTextBlock;
        private StackPanel _loginPanel;
        private TextBox _userNameTextBox;
        private TextBlock _wordCountTextBlock;
        private TextBlock _wordFoundCountTextBlock;
        private ListBox _wordListControl;
        private ListBox _playersWordListControl;
        private StackPanel _playersWordListPanel;
        private StackPanel _localWordListPanel;
        private ListBox _solutionControl;
        private ListBox _playersListControl;
        private Button _cmdAddWord;
        private Button _cmdJoin;
        private SoundFX _soundFX;
        private Brush _cmdAddBrush;
        private Word _wordEntry = new Word();
        private WordList _wordList = new WordList();
        private Players _Players;
        private WordList _Solution;

        private WebBogglerServer.GameInfo _gameInfo;
        private DeskMode _Mode = DeskMode.Busy;

        private Player _localPlayer = new Player() {ID = "-1" };

        private const int ROUND_DURATION_MIN = 0;

        private int _roundDurationMS = /*default*/ 180000;

        private enum DeskMode
        {
            Busy,
            Playing,
            Observing
        }


        public Desk( BoardGrid boardGrid, HourglassControl hourglass,
                    StackPanel gameStatusPanel, StackPanel gameControlsPanel,
                    TextBlock gameStatusText, TextBlock gameSubStatusText,
                    TextBlock wordCountTextBlock, TextBlock wordFoundCountTextBlock, TextBlock inputWordTextBlock,
                    Button addWordButton, ListBox wordListControl,
                    StackPanel playersWordListPanel, StackPanel localWordListPanel, ListBox playersWordListControl, ListBox solutionControl,
                    ListBox playersListControl, StackPanel loginPanel, TextBox userNameTextBox,
                    Button cmdJoin, SoundFX soundFXControl)
        {
            _gameControlsPanel = gameControlsPanel;
            _gameStatusPanel = gameStatusPanel;
            _gameStatusText = gameStatusText;
            _gameSubStatusText = gameSubStatusText;
            _wordCountTextBlock = wordCountTextBlock;
            _inputWordTextBlock = inputWordTextBlock;
            _wordFoundCountTextBlock = wordFoundCountTextBlock;
            _wordListControl = wordListControl;
            _playersListControl = playersListControl;
            _localWordListPanel = localWordListPanel;
            _playersWordListPanel = playersWordListPanel;
            _playersWordListControl = playersWordListControl;
            _solutionControl = solutionControl;
            _cmdAddWord = addWordButton;
            _cmdAddBrush = addWordButton.Foreground;
            _gameControlsPanel.Visibility = Visibility.Collapsed;
            _gameStatusPanel.Visibility = Visibility.Visible;
            _cmdJoin = cmdJoin;
            _soundFX = soundFXControl; 
            _loginPanel = loginPanel;
            _userNameTextBox = userNameTextBox;
            _boardGrid = boardGrid;
            _boardGrid.IsEnabled = false;
            _Hourglass = hourglass;

            ServiceConnector service = new ServiceConnector();
            _serviceClient = service.ConnectService ();

			// Initialize websocket asynchronously and register handlers after connection
			_ = InitializeWebSocketAsync(service);

            _Hourglass.Duration = new TimeSpan(0,0,0,0, _roundDurationMS);
            _Hourglass.Reset();
            _oneSecondTicker.Tick += _OneSecondTicker_Tick;
        
        
        
        }

        #region "Properties"

        internal Word WordEntry
		{ get { return _wordEntry; } }

		internal Board GameBoard
        {
            get { return _board; }
        }

        internal Player LocalPlayer
        {
            get { return _localPlayer; }
            set { _localPlayer = value; }
        }

        internal Players GamePlayers
        {
            get { return _Players; }
            set { _Players = value; }
        }

#endregion

        #region "Methods"

        internal void ConnectWebSocket()
        {
            if (_WebSocket != null)
            {
                _WebSocket.OnOpen += _WebSocket_OnOpen;
                _WebSocket.OnMessage += _WebSocket_OnMessage;
                _WebSocket.OnClose += _WebSocket_OnClose;
                _WebSocket.OnError += _WebSocket_OnError;


                _wordEntry.Change += _inputWord_Change;
            }
        }

        private async Task InitializeWebSocketAsync(ServiceConnector service)
        {
            try
            {
                var ws = await service.ConnectWebSocketAsync();
                _WebSocket = ws;
                if (_WebSocket != null)
                {
                    ConnectWebSocket();
                    // ensure server registration message is sent
                    Register();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("WebSocket connect error: " + ex.Message);
            }
        }

        internal void Echo(string msg)
        { if (_WebSocket != null) _WebSocket.Send(msg);}

        internal void Register()
		{
            try
            {
                if (_WebSocket == null)
                {
                    ServiceConnector service = new ServiceConnector();
                    _WebSocket = service.ConnectWebSocket();
                    ConnectWebSocket();
                }
                if (_WebSocket != null) _WebSocket.Send("REGISTER"); //Il server assegna l'ID, lo reinvia con tag #
            }
            finally { }
		}

        internal void Join(string userName)
        {
            _localPlayer.NickName = userName;

            var result = _serviceClient.Join(_localPlayer.ID, "*" + _localPlayer.NickName +"."+ _localPlayer.ID.Substring(0, 3));
            if (result)
            {
                _gameInfo = _serviceClient.Observe();

                if (_gameInfo.RoomState == "RunningRound")
                {
                    _gameControlsPanel.Visibility = Visibility.Visible;
                    _gameStatusPanel.Visibility = Visibility.Collapsed;
                    _boardGrid.HideCover();
                    _boardGrid.IsEnabled = true;
                    _Hourglass.Visibility = Visibility.Visible;
                    _Hourglass.HideCover();
                    _Hourglass.Reset();
                    _Hourglass.Run();

                    var startTimeOffset = TimeSpan.FromMilliseconds(_gameInfo.RoundElapsedTimeMS);
                    _Hourglass.StartTimeUTC = DateTime.Now.ToUniversalTime() - startTimeOffset;

                    _wordFoundCountTextBlock.Visibility = Visibility.Visible;
                    _wordFoundCountTextBlock.Text = "Parole trovate: 0";
                }
                _Mode = DeskMode.Playing;
                _loginPanel.Visibility = Visibility.Collapsed;
                _wordListControl.SelectionChanged += _wordListControl_SelectionChanged;
                _cmdJoin.Content = "Abbandona";
                _cmdJoin.Tag = "Leave";
            }
        }

        internal void Leave()
		{
            var result = false;
            try
            {
                result = _serviceClient.Leave(_localPlayer.ID);
            }
            finally
            {
                if (result)
                {
                    _wordEntry.Clear();
                    _gameControlsPanel.Visibility = Visibility.Collapsed;
                    _gameStatusPanel.Visibility = Visibility.Visible;
                    _wordFoundCountTextBlock.Visibility = Visibility.Collapsed;
                    _playersWordListPanel.Visibility = Visibility.Collapsed;
                    _localWordListPanel.Visibility = Visibility.Visible;
                     _boardGrid.ShowCover();
                    _Hourglass.ShowCover();
                    _Hourglass.Visibility = Visibility.Visible;
                    _cmdJoin.Content = "Partecipa";
                    _cmdJoin.IsEnabled = false;
                    _cmdJoin.Tag = "Join";
                    _userNameTextBox.Text = "";
                    _loginPanel.Visibility = Visibility.Visible;
                    _wordListControl.SelectionChanged -= _wordListControl_SelectionChanged;
                    _Mode = DeskMode.Observing;
               }
            }
		}

		internal void GetBoardFromServer(string localeID)
        {
           var board = new WebBogglerServer.Board();
           try
            {
                board = _serviceClient.GetBoard(localeID);
                _board = (Board)board;
            }
            catch( Exception ex) { System.Windows.MessageBox.Show(ex.Message); }

            finally
            {
                if (_board == null) _board = new Board();

                _boardGrid.SetBoard(_board.DicesVector);
                _wordCountTextBlock.Text = "Parole possibili: " + _board.WordCount;
            }

        }

        internal bool ValidateWord(string text)
        {
            try
            {
                return _serviceClient.CheckWord(text);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error checking word! \n"+ ex.Message);
                return false;
            }
            finally { }
         }

        internal void AddEntryToWordList()
        {
            _soundFX.PlaySound(Sound.AddWord);
            _wordList.Add(_wordEntry.Clone());

           _wordListControl.ItemsSource = _wordList;
            _wordFoundCountTextBlock.Text = "Parole trovate: " + _wordList.Count;
        }

        internal void ClearWordEntry()
        {
            _soundFX.PlaySound(Sound.Erase);
            _wordEntry.Clear();

            _wordListControl.SelectedIndex = -1;
            _wordListControl.SelectedItem = null;

        }

        #endregion

        #region "Subs/Funcs"

        private async void ObserveGame()
        {
            _Mode = DeskMode.Observing; 

            GetBoardFromServer("it-IT");

            _gameInfo = _serviceClient.Observe();

            _gameStatusText.Text = "Turno di gioco #" + (_board.GameSerial).ToString().Trim();

            _Players = await UpdatePlayers();
            _playersListControl.ItemsSource = _Players;

            _Hourglass.Visibility = Visibility.Visible;
            _Hourglass.ShowCover();
            _boardGrid.ShowCover();

            //var startTimeOffset = TimeSpan.FromMilliseconds(_gameInfo.RoundElapsedTimeMS);
            _Hourglass.StartTimeUTC = DateTime.Now.ToUniversalTime().AddMilliseconds( (double) (- _gameInfo.RoundElapsedTimeMS));

            _oneSecondTicker.Start(); //va posto qui altrimenti legge gameInfo ancora nullo

            // _Hourglass.StartTimeUTC = _gameInfo.RoundStartTime;
            if (_gameInfo.RoomState == "RunningRound")
            {
                _Hourglass.Reset();
                _Hourglass.Run();
                _Hourglass.StartTimeUTC = DateTime.Now.ToUniversalTime().AddMilliseconds( (double) (- _gameInfo.RoundElapsedTimeMS));
            }
        }

        private void SendWordList()
        {
            // Rely on the implicit conversion operator between local WordList and proxy WordList
            _serviceClient.SendWordList(_wordList, _localPlayer.ID);
        }

        private async Task<Players> UpdatePlayers()
        {
            Players plys;
            try
            {
                plys = (Players)(_serviceClient.GetPlayers(_localPlayer.ID));
            }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message);  return null;}


            return plys;
        }

        private async Task<WordList> GetSolution()
        {
            WordList sol;
            try
            {
                sol = (WordList)_serviceClient.GetSolution();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message); return null; }

            return sol;

        }

        private void subAddCharacterDice(Dice dice, bool appendLastPriority)
		{
			if (_wordEntry.DicePath.Contains(dice)) {
				if ((_wordEntry.IsFirst(dice) || _wordEntry.IsLast(dice))) {
					_wordEntry.RemoveDice(dice);
                    _soundFX.PlaySound(Sound.Erase);
                    return;
				} else {
                    _soundFX.PlaySound(Sound.Failure);
                    return;
				}
			}

			if ((WordEntry.DicePath.Count >= 1)) {
				List<Dice> validDicesLast;
				List<Dice> validDicesFirst;
				validDicesLast = _board.GetValidEntries(WordEntry.DicePath.Last(), _wordEntry);
				validDicesFirst = _board.GetValidEntries(_wordEntry.DicePath.First(), _wordEntry);
				if (validDicesFirst.Contains(dice)) {
					if (validDicesLast.Contains(dice)) {
						if (appendLastPriority)	{
                            _soundFX.PlaySound(Sound.LetterTap);
                            _wordEntry.AppendDiceLast(dice);
							return;
						} else {
                            _soundFX.PlaySound(Sound.LetterTap);
                            _wordEntry.AppendDiceFirst(dice);
							return;
						}
					}
                    _soundFX.PlaySound(Sound.LetterTap);
                    _wordEntry.AppendDiceFirst(dice);
					return;
				} else if (validDicesLast.Contains(dice)) {
                    _soundFX.PlaySound(Sound.LetterTap );
                    _wordEntry.AppendDiceLast(dice);
					return;
				} else {
                    _soundFX.PlaySound(Sound.Failure);
                    return;
				}
			} else {
				_soundFX.PlaySound(Sound.LetterTap);
				_wordEntry.AppendDiceLast(dice);
				// se la parola è vuota aggiungo senza tante storie
			}
		}

        #endregion

        #region "Event Handlers"

        private void _OneSecondTicker_Tick(object sender, object e)
        {
            var delayMS = _gameInfo.DeadTimeAmountMS + _gameInfo.RoundDurationMS - (int)((DateTime.Now.ToUniversalTime() - _Hourglass.StartTimeUTC).TotalMilliseconds);
            if (delayMS < 0) delayMS = 0;
            var delay = TimeSpan.FromMilliseconds(delayMS);             
            _gameSubStatusText.Text = "Il prossimo turno inizierà tra " + String.Format("{0:0}:{1:00}", (delay.Minutes), delay.Seconds);
        }

        internal void LetterTappedHandler(object sender, int index)
		{
            var dice = (Dice)_board.DicesVector[index - 1];
			if (_wordEntry.DicePath.Count() > 0 && (_wordEntry.IsLast(dice) || _wordEntry.IsFirst(dice)) )
            {
                _wordEntry.RemoveDice((Dice)_board.DicesVector[index - 1]);
			}
            else
            {
				subAddCharacterDice(_board.DicesVector[index - 1], true);
			}
		}

        private void _wordListControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_wordEntry.DicePath.Count == 0) // solo se non sto inserendo lettere
            {
                var lb = (ListBox)sender;
                if (lb.SelectedIndex >= 0)
                {
                    var w = _wordList.ElementAt(lb.SelectedIndex);
                    _boardGrid.DrawWordPaths(w);
                }
            }
        }

        private void _playersWordListControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_wordEntry.DicePath.Count == 0) // solo se non sto inserendo lettere
            {
                var lb = (ListBox)sender;
                if (lb.SelectedIndex >= 0)
                {
                    var w = ((WordList)_playersWordListControl.ItemsSource).ElementAt(lb.SelectedIndex);
                    _boardGrid.DrawWordPaths(w);
                }
            }
        }

        private void _solutionControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_wordEntry.DicePath.Count == 0) // solo se non sto inserendo lettere
            {
                var lb = (ListBox)sender;
                if (lb.SelectedIndex >= 0)
                {
                    var w = _Solution.ElementAt(lb.SelectedIndex);
                    _boardGrid.DrawWordPaths(w);
                }
            }
        }

        private void _playersListControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
                var lb = (ListBox)sender;
                if (lb.SelectedIndex >= 0)
                {
                    var w = (_Players.ElementAt(_playersWordListControl.SelectedIndex)).WordList;
                    _playersWordListControl.ItemsSource = w;
                }
        }

		private void _inputWord_Change(object sender, WordBase.WordChangeEventArgs e)
		{
            _inputWordTextBlock.Text = ((Word)sender).ToString.ToUpper();
			if (!((WordBase)sender).IsEmpty()) {
				_boardGrid.DrawWordPaths((Word)sender);
                _cmdAddWord.IsEnabled = true;
			}
			else
            {
				_boardGrid.ClearPaths();
                _cmdAddWord.IsEnabled = false;
            }

            _cmdAddWord.Foreground = _cmdAddBrush; //green
            _cmdAddWord.Content = _cmdAddWord.Tag.ToString();


        }

        private void _WebSocket_OnOpen(object sender, EventArgs e)
		{
            _boardGrid.IsEnabled = false;

            Register(); 
		}

		private async void _WebSocket_OnMessage(object sender, CSHTML5.Extensions.WebSockets.OnMessageEventArgs e)
		{
            var msg = (e.Data.ToString().ToUpper());
            if(msg.Substring(0,1) == "#") //Alla richiesta di registrazione il server invia il client ID assegnato preceduto da #
            {
                //Lo memorizzo e in seguito lo uso per il Join
                _localPlayer.ID = msg.Remove(0,1);
                _Mode = DeskMode.Busy;
                ObserveGame();

            }
            else
            {
                switch (msg)
			    {
				    case "GET_BOARD":
                        _gameStatusText.Text = "Sorteggio nuovo turno...";
                        _boardGrid.IsEnabled = false;
					    _boardGrid.ShowCover();
                        _Hourglass.ShowCover();

                        if (_Mode == DeskMode.Playing || _Mode == DeskMode.Observing )
                        {
                            _playersWordListPanel.Visibility = Visibility.Collapsed;
                            _localWordListPanel.Visibility = Visibility.Visible;
                            _solutionControl.SelectionChanged -= _solutionControl_SelectionChanged;
                            _playersWordListControl.SelectionChanged -= _playersWordListControl_SelectionChanged;
                            _Hourglass.Visibility = Visibility.Visible;
                            _wordList = new WordList();
                            _wordListControl.ItemsSource = _wordList;
					    }

                        _soundFX.PlaySound(Sound.ShakeBoard);
                        this.GetBoardFromServer("it-IT");
                        _gameStatusText.Text = "Turno di gioco #" + _board.GameSerial.ToString().Trim() ;

                        break;

				    case "START_ROUND":

                        if (_Mode == DeskMode.Playing)
                        {
                            _soundFX.PlaySound(Sound.StartGame);
                            _gameControlsPanel.Visibility = Visibility.Visible;
                            _gameStatusPanel.Visibility = Visibility.Collapsed;
                            _playersWordListPanel.Visibility = Visibility.Collapsed;
                            _localWordListPanel.Visibility = Visibility.Visible;
                            _boardGrid.HideCover();
                            _boardGrid.IsEnabled = true;
                            _Hourglass.HideCover();
                            _Hourglass.Visibility = Visibility.Visible;
                            _wordFoundCountTextBlock.Text = "Parole trovate: 0";
                        }
                        else if (_Mode == DeskMode.Observing)
                        {
                            _boardGrid.ShowCover();
                            _boardGrid.IsEnabled = false;
                            _Hourglass.ShowCover();
                            _Hourglass.Visibility = Visibility.Visible;
                            _wordListControl.SelectionChanged -= _wordListControl_SelectionChanged;
                        }
                        _Hourglass.Reset();
                        _Hourglass.Run();

                        break;

				    case "END_ROUND":

                        _gameStatusText.Text = "Tempo scaduto.";
                        if (_Mode == DeskMode.Playing)
                        {
                            _soundFX.PlaySound(Sound.EndGame);  
                            _boardGrid.IsEnabled = false;
                            _wordEntry.Clear();
                            _gameControlsPanel.Visibility = Visibility.Collapsed;
                            _gameStatusPanel.Visibility = Visibility.Visible;
                                                          
                            SendWordList();
                        }
                        else if (_Mode == DeskMode.Observing)
                        {
                            _boardGrid.ShowCover();
                            _boardGrid.IsEnabled = false;
                        }

                        break;

				    case "SHOW_TIME":

                        _gameStatusText.Text = "Controllo parole e punteggio...";

                        _Players = await UpdatePlayers();
                        _playersListControl.ItemsSource = _Players;
                        _playersWordListControl.SelectionChanged += _playersWordListControl_SelectionChanged;
                        _Solution = await GetSolution();
                        _solutionControl.ItemsSource = _Solution;
                        _solutionControl.SelectionChanged += _solutionControl_SelectionChanged;

                        if (_Mode == DeskMode.Playing)
                        {
					        _boardGrid.IsEnabled = false;
					        _boardGrid.HideCover();
                            _Hourglass.ShowCover();
                            _playersWordListPanel.Visibility = Visibility.Visible;
                            _localWordListPanel.Visibility = Visibility.Collapsed;
                            _Hourglass.Visibility = Visibility.Collapsed;
                            _playersWordListControl.SelectionChanged += _playersWordListControl_SelectionChanged;
                         }

                        break;

				    case "REGISTERED":

					    _boardGrid.IsEnabled = false;
					    _boardGrid.ShowCover();
                        _Hourglass.ShowCover();

					    break;

				    case "UPDATE_PLAYERS":

                        _Players = await UpdatePlayers();
                        _playersListControl.ItemsSource = _Players;

                        break;

                    //default:
                    //  System.Windows.MessageBox.Show(e.Data.ToString());
                    //
                    //  break;
                }
            }
		}

		private void _WebSocket_OnError(object sender, CSHTML5.Extensions.WebSockets.OnErrorEventArgs e)
		{
			System.Windows.MessageBox.Show("WebSocket Error!");
		}

		private void _WebSocket_OnClose(object sender, EventArgs e)
		{
            _WebSocket = null;
            Leave();  
            _localPlayer.ID = "-1";
            _boardGrid.IsEnabled = false;

        }

#endregion

	}

}
