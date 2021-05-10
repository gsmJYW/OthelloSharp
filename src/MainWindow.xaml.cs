using Microsoft.VisualBasic;
using OthelloSharp.src;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static OthelloSharp.src.Game;

namespace OthelloSharp
{
    public partial class MainWindow : Window
    {
        Game game;

        Thread CreateGameThread, JoinGameThread;
        Server Server;
        Client Client;

        bool IsPiecePlaced, MyTimeout, OpponentTimeout, MyResign, OpponentResign, GameOver;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ControlBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MinimizeButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void MenuButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Rectangle)sender).Opacity = 1;
        }

        private void MenuButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Rectangle)sender).Opacity = 0;
        }

        private void ServerButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ushort port = 727;

            CreateGameThread = new Thread(() => CreateGame(port))
            {
                IsBackground = true
            };

            CreateGameThread.Start();
        }

        private void ClientButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ushort port = 727;
            string serverAddress = Interaction.InputBox("서버 주소를 입력하세요.", "Othello#");

            if (serverAddress.Length == 0)
            {
                return;
            }

            JoinGameThread = new Thread(() => JoinGame(serverAddress, port))
            {
                IsBackground = true
            };

            JoinGameThread.Start();
        }

        private void HelpButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(
                "8x8 보드판에 마우스 좌클릭으로 돌을 둘 수 있습니다.\n" +
                "각자 5분의 시간이 주어지며 시간을 다 쓰면 패배합니다.\n" +
                "만약 돌을 놓을 수 있는 곳이 없다면 자동으로 턴이 넘겨집니다.\n" +
                "흑이 선공, 백이 후공이며 흑백 지정은 랜덤입니다.\n" +
                "이 외의 룰은 보드게임 오델로와 같습니다.\n",
                "Othello#"
                );
        }

        private void CancelButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBoxWriteLine(ConsoleTextBox, "연결 취소.");
            DisableCancelButton();
            Disconnect();
        }

        private void CreateGame(ushort port)
        {
            Server = new Server(this);

            TextBoxWriteLine(ConsoleTextBox, "게임 생성 중...");
            EnableCancelButton();

            try
            {
                Server.Open(port);
            }
            catch (Exception e)
            {
                TextBoxWriteLine(ConsoleTextBox, "{0}.", e.Message);
                DisableCancelButton();
                Disconnect();
                return;
            }

            TextBoxWriteLine(ConsoleTextBox, "게임 생성 성공, 연결을 기다리는 중...");

            while (true)
            {
                try
                {
                    if (Server.IsClientConnected())
                    {
                        Connected();
                        break;
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        private void JoinGame(string serverAddress, ushort port)
        {
            Client = new Client(this);

            TextBoxWriteLine(ConsoleTextBox, "{0}(으)로 연결을 시도하는 중...", serverAddress);
            EnableCancelButton();

            try
            {
                Client.Connect(serverAddress, port);
            }
            catch (Exception e)
            {
                if (Client != null)
                {
                    TextBoxWriteLine(ConsoleTextBox, "{0}.", e.Message);
                    DisableCancelButton();
                    Disconnect();
                }

                return;
            }

            Connected();
        }

        private void EnableCancelButton()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                ServerButton.IsEnabled = false;
                ClientButton.IsEnabled = false;
                CancelButton.IsEnabled = true;

                ServerIcon.Opacity = .5;
                ClientIcon.Opacity = .5;
                CancelIcon.Opacity = 1;
                
                ServerLabel.Opacity = .5;
                ClientLabel.Opacity = .5;
                CancelLabel.Opacity = 1;
            }));
        }

        private void DisableCancelButton()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                ServerButton.IsEnabled = true;
                ClientButton.IsEnabled = true;
                CancelButton.IsEnabled = false;

                ServerIcon.Opacity = 1;
                ClientIcon.Opacity = 1;
                CancelIcon.Opacity = .5;

                ServerLabel.Opacity = 1;
                ClientLabel.Opacity = 1;
                CancelLabel.Opacity = .5;
            }));
        }

        public void TextBoxWriteLine(RichTextBox richTextbox, string format, params object[] args)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                richTextbox.AppendText(string.Format(format, args));
                richTextbox.AppendText(Environment.NewLine);
                richTextbox.ScrollToEnd();
            }));
        }

        public void Send(string format, params object[] args)
        {
            if (Server != null)
            {
                Server.Send(format, args);
            }
            else if (Client != null)
            {
                Client.Send(format, args);
            }
        }

        public void Sent(string msg)
        {
            string[] msgSplit = msg.Split();
            string request = msgSplit[0];

            switch (request)
            {
                case "chat":
                    string content = string.Join(" ", msgSplit.Skip(1).ToArray());
                    TextBoxWriteLine(ChatTextBox, "<나> {0}", content);
                    break;

                case "piece":
                    game.SetPiece(Convert.ToInt32(msgSplit[1]));
                    break;

                case "timeout":
                    MyTimeout = true;
                    break;

                case "resign":
                    MyResign = true;
                    break;
            }
        }

        public void Recieved(string msg)
        {
            string[] msgSplit = msg.Split();
            string request = msgSplit[0];

            switch (request)
            {
                case "chat":
                    string content = string.Join(" ", msgSplit.Skip(1).ToArray());
                    TextBoxWriteLine(ChatTextBox, "<상대> {0}", content);
                    break;

                case "piece":
                    game.SetPiece(Convert.ToInt32(msgSplit[2]));
                    break;

                case "place":
                    game.PlacePiece(game.opponentPiece, Convert.ToInt32(msgSplit[1]), Convert.ToInt32(msgSplit[2]));
                    game.opponentTime = Convert.ToDouble(msgSplit[3]);
                    IsPiecePlaced = true;
                    break;

                case "timeout":
                    OpponentTimeout = true;
                    break;

                case "resign":
                    OpponentResign = true;
                    break;
            }
        }

        private void ChatInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (ChatInputTextBox.Text.Length > 0 && e.Key == Key.Return)
            {
                Send("chat {0}", ChatInputTextBox.Text);
                ChatInputTextBox.Clear();
            }
        }

        private void BoardGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (game.turn != game.myPiece)
            {
                return;
            }

            var point = Mouse.GetPosition(BoardGrid);

            int row = 0;
            int col = 0;

            double accumulatedHeight = 0.0;
            double accumulatedWidth = 0.0;

            foreach (var rowDefinition in BoardGrid.RowDefinitions)
            {
                accumulatedHeight += rowDefinition.ActualHeight;

                if (accumulatedHeight >= point.Y)
                {
                    break;
                }

                row++;
            }

            foreach (var columnDefinition in BoardGrid.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;

                if (accumulatedWidth >= point.X)
                {
                    break;
                }

                col++;
            }

            if (game.IsAvailable(game.myPiece, row, col))
            {
                game.PlacePiece(game.myPiece, row, col);
                Send("place {0} {1} {2}", row, col, game.myTime);

                IsPiecePlaced = true;
            }
        }

        public void DrawPiece(int[,] board)
        {
            for (int row = 0; row < board.GetLength(0); row++)
            {
                for (int col = 0; col < board.GetLength(1); col++)
                {
                    if (board[row, col] != Piece.Empty)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            var pieceEllipse = new Ellipse()
                            {
                                Height = 43,
                                Width = 43,
                                Margin = new Thickness() { Top = 7 + 52 * row, Left = 7 + 52 * col }
                            };

                            if (board[row, col] == Piece.Black)
                            {
                                pieceEllipse.Fill = Brushes.Black;
                            }
                            else
                            {
                                pieceEllipse.Fill = Brushes.White;
                            }

                            BoardCanvas.Children.Add(pieceEllipse);
                        }));
                    }
                }
            }
        }

        public void UpdateGroupBoxBorder()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (game.turn == game.myPiece)
                {
                    MyGroupBox.BorderBrush = Brushes.Red;
                    OpponentGroupBox.BorderBrush = Brushes.LightGray;
                }
                else if (game.turn == game.opponentPiece)
                {
                    OpponentGroupBox.BorderBrush = Brushes.Red;
                    MyGroupBox.BorderBrush = Brushes.LightGray;
                }
                else
                {
                    MyGroupBox.BorderBrush = Brushes.LightGray;
                    OpponentGroupBox.BorderBrush = Brushes.LightGray;
                }
            }));
        }

        public void UpdateTimeLabel()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                MyTimeSecondLabel.Content = string.Format("{0:000.0}", game.myTime);
                OpponentTimeSecondLabel.Content = string.Format("{0:000.0}", game.opponentTime);
            }));
        }

        public void UpdateLogLabel(string log)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                LogLabel.Content = log;
            }));
        }

        public void UpdatePieceCountLabel()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                MyPieceCountLabel.Content = string.Format("{0:00}", game.CountPiece(game.myPiece));
                OpponentPieceCountLabel.Content = string.Format("{0:00}", game.CountPiece(game.opponentPiece));
            }));
        }

        private void ResignButton_Click(object sender, RoutedEventArgs e)
        {
            Send("resign");
        }

        public void Connected()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                ServerButton.IsEnabled = true;
                ClientButton.IsEnabled = true;

                ServerIcon.Opacity = 1;
                ClientIcon.Opacity = 1;

                ServerLabel.Opacity = 1;
                ClientLabel.Opacity = 1;

                MenuButtonPanel.IsEnabled = false;
                MenuLabelPanel.Opacity = .5;
                MenuIcon.Opacity = .5;

                ChatTextBox.IsEnabled = true;
                ChatInputTextBox.IsEnabled = true;
            }));

            game = new Game();

            if (Server != null)
            {
                if (new Random().Next(0, 2) == 0)
                {
                    Send("piece {0} {1}", Piece.Black, Piece.White);
                }
                else
                {
                    Send("piece {0} {1}", Piece.White, Piece.Black);
                }
            }

            while (game.myPiece == 0 || game.opponentPiece == 0)
            {
                if (Server == null && Client == null)
                {
                    return;
                }
            }

            Dispatcher.Invoke(new Action(() =>
            {
                ConsoleTextBox.Document.Blocks.Clear();
            }));

            TextBoxWriteLine(ConsoleTextBox, "당신의 색은 {0}입니다.", Piece.Black == game.myPiece ? "흑" : "백");
            TextBoxWriteLine(ConsoleTextBox, "{0} 차례로 시작합니다.", Piece.Black == game.myPiece ? "당신" : "상대");

            for (int sec = 4; sec > 0; sec--)
            {
                TextBoxWriteLine(ConsoleTextBox, sec.ToString());
                Thread.Sleep(1000);

                if (Server == null && Client == null)
                {
                    return;
                }
            }

            Dispatcher.Invoke(new Action(() =>
            {
                ConsoleTextBox.Document.Blocks.Clear();

                Menu.Visibility = Visibility.Hidden;
                Result.Visibility = Visibility.Visible;
                BoardCanvas.Visibility = Visibility.Visible;
                
                MenuButtonPanel.IsEnabled = true;
                MenuLabelPanel.Opacity = 1;
                MenuIcon.Opacity = 1;

                if (game.myPiece == Piece.Black)
                {
                    OpponentColorRectangle.Fill = Brushes.White;
                    OpponentColorRectangle.Stroke = Brushes.Black;

                    MyColorRectangle.Fill = Brushes.Black;
                }
                else
                {
                    MyColorRectangle.Fill = Brushes.White;
                    MyColorRectangle.Stroke = Brushes.Black;

                    OpponentColorRectangle.Fill = Brushes.Black;
                }
            }));

            game.InitBoard();
            DrawPiece(game.board);

            game.myTime = 300;
            game.opponentTime = 300;

            game.turn = Piece.Black;

            while (true)
            {
                UpdateGroupBoxBorder();

                DateTime startTime = DateTime.Now;

                double myStartTime = game.myTime;
                double opponentStartTime = game.opponentTime;

                while (!IsPiecePlaced)
                {
                    if (game.myTime <= 0)
                    {
                        Send("timeout");
                    }

                    if (MyTimeout || OpponentTimeout || MyResign || OpponentResign)
                    {
                        break;
                    }

                    double ellapsedSecs = DateTime.Now.Subtract(startTime).TotalSeconds;

                    if (game.turn == game.myPiece)
                    {
                        game.myTime = myStartTime - ellapsedSecs;
                    }
                    else
                    {
                        game.opponentTime = opponentStartTime - ellapsedSecs;
                    }

                    UpdateTimeLabel();
                    Thread.Sleep(100);
                }

                DrawPiece(game.board);
                UpdatePieceCountLabel();
                IsPiecePlaced = false;

                if (MyTimeout || OpponentTimeout || MyResign || OpponentResign)
                {
                    break;
                }
                else if (!game.HasAvailablePlace(game.myPiece) && !game.HasAvailablePlace(game.opponentPiece))
                {
                    break;
                }
                else if (game.turn == game.opponentPiece && !game.HasAvailablePlace(game.myPiece))
                {
                    UpdateLogLabel("당신이 돌을 놓을 수 없어\n상대 차례입니다.");
                    continue;
                }
                else if (game.turn == game.myPiece && !game.HasAvailablePlace(game.opponentPiece))
                {
                    UpdateLogLabel("상대가 돌을 놓을 수 없어\n당신 차례입니다.");
                    continue;
                }
                else if (game.turn == game.myPiece)
                {
                    game.turn = game.opponentPiece;
                }
                else
                {
                    game.turn = game.myPiece;
                }
                UpdateLogLabel("");
            }

            game.turn = 0;
            GameOver = true;
            UpdateGroupBoxBorder();
            UpdateLogLabel("");

            if (MyTimeout)
            {
                MessageBox.Show("시간을 전부 사용하셨습니다.\n상대의 승리입니다.", "Othello#");
                MyTimeout = false;
            }
            else if (OpponentTimeout)
            {
                MessageBox.Show("상대가 시간을 전부 사용했습니다.\n당신의 승리입니다.", "Othello#");
                OpponentTimeout = false;
            }
            else if (MyResign)
            {
                MessageBox.Show("물러나셨습니다.\n상대의 승리입니다.", "Othello#");
                MyResign = false;
            }
            else if (OpponentResign)
            {
                MessageBox.Show("상대가 물러났습니다.\n당신의 승리입니다.", "Othello#");
                OpponentResign = false;
            }
            else
            {
                int myPieceCount = game.CountPiece(game.myPiece);
                int opponentPieceCount = game.CountPiece(game.opponentPiece);

                if (myPieceCount == opponentPieceCount)
                {
                    MessageBox.Show(string.Format("돌 개수가 같습니다.\n무승부입니다.", myPieceCount - opponentPieceCount), "Othello#");
                }
                else
                {
                    MessageBox.Show(string.Format("{0}의 돌이 {1}개 더 많습니다.\n{0}의 승리입니다.", myPieceCount > opponentPieceCount ? "당신" : "상대", Math.Abs(myPieceCount - opponentPieceCount)), "Othello#");
                }
            }

            Disconnect();
        }

        public void Disconnected()
        {
            DisableCancelButton();

            Dispatcher.Invoke(new Action(() =>
            {
                MenuButtonPanel.IsEnabled = true;
                MenuLabelPanel.Opacity = 1;
                MenuIcon.Opacity = 1;

                ChatTextBox.IsEnabled = false;
                ChatInputTextBox.IsEnabled = false;

                ConsoleTextBox.Document.Blocks.Clear();
                ChatTextBox.Document.Blocks.Clear();
                ChatInputTextBox.Clear();

                Menu.Visibility = Visibility.Visible;
                Result.Visibility = Visibility.Hidden;
                BoardCanvas.Visibility = Visibility.Hidden;
            }));

            if (CreateGameThread != null && CreateGameThread.IsAlive)
            {
                CreateGameThread.Interrupt();
            }
            else if (JoinGameThread != null && JoinGameThread.IsAlive)
            {
                JoinGameThread.Interrupt();
            }

            if (GameOver)
            {
                GameOver = false;
            }
            else
            {
                MessageBox.Show("상대와의 연결이 끊겼습니다", "Othello#");
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (Server != null)
            {
                Server.Close();
                Server = null;
            }

            if (Client != null)
            {
                Client.Close();
                Client = null;
            }
        }

        public string PieceName(int piece)
        {
            if (piece == Piece.Black)
            {
                return "흑";
            }
            else
            {
                return "백";
            }
        }
    }
}
