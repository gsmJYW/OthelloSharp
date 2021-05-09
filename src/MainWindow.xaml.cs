using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OthelloSharp
{
    public partial class MainWindow : Window
    {
        static class Constants
        {
            public const int Empty = 0;
            public const int White = 1;
            public const int Black = 2;
        }

        Thread CreateGameThread, JoinGameThread;
        Server Server;
        Client Client;

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
            ushort port;
            string portInput = Interaction.InputBox("포트 번호를 입력하세요.\n(0 ~ 65535)", "Othello#");

            while (true)
            {
                try
                {
                    if (portInput.Length == 0)
                    {
                        return;
                    }

                    port = Convert.ToUInt16(portInput);
                }
                catch
                {
                    portInput = Interaction.InputBox("잘못된 포트 번호입니다.\n포트 번호를 다시 입력하세요.\n(0 ~ 65535)", "Othello#");
                    continue;
                }

                break;
            }

            CreateGameThread = new Thread(() => CreateGame(port))
            {
                IsBackground = true
            };

            CreateGameThread.Start();
        }

        private void ClientButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ushort port;
            string serverAddress = Interaction.InputBox("서버 주소를 입력하세요.", "Othello#");

            if (serverAddress.Length == 0)
            {
                return;
            }

            string portInput = Interaction.InputBox("포트 번호를 입력하세요.\n(0 ~ 65535)", "Othello#");

            while (true)
            {
                try
                {
                    if (portInput.Length == 0)
                    {
                        return;
                    }

                    port = Convert.ToUInt16(portInput);
                }
                catch
                {
                    portInput = Interaction.InputBox("잘못된 포트 번호입니다.\n포트 번호를 다시 입력하세요.\n(0 ~ 65535)", "Othello#");
                    continue;
                }

                break;
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

            if (CreateGameThread != null && CreateGameThread.IsAlive)
            {
                Server.Close();
                Server = null;
            }

            if (JoinGameThread != null && JoinGameThread.IsAlive)
            {
                Client.Close();
                Client = null;
            }

            DisableCancelButton();
        }

        private void CreateGame(ushort port)
        {
            Server = new Server(this);

            TextBoxWriteLine(ConsoleTextBox, "{0} 포트에서 게임 생성 중...", port);
            EnableCancelButton();

            try
            {
                Server.Open(port);
            }
            catch (Exception e)
            {
                TextBoxWriteLine(ConsoleTextBox, "{0}.", e.Message);
                DisableCancelButton();
                return;
            }

            TextBoxWriteLine(ConsoleTextBox, "게임 생성 성공, 연결을 기다리는 중...");

            while (true)
            {
                try
                {
                    if (Server.IsClientConnected())
                    {
                        DisableCancelButton();
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

            TextBoxWriteLine(ConsoleTextBox, "{0}:{1}(으)로 연결을 시도하는 중...", serverAddress, port);
            EnableCancelButton();

            try
            {
                Client.Connect(serverAddress, port);
                Connected();
            }
            catch (Exception e)
            {
                try
                {
                    Client.IsServerConnected();
                    TextBoxWriteLine(ConsoleTextBox, "{0}.", e.Message);
                }
                catch
                {

                }
            }
            DisableCancelButton();
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

        private void TextBoxWriteLine(RichTextBox richTextbox, string format, params object[] args)
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

        public void Recieved(string msg)
        {
            string[] msgSplit = msg.Split();
            string request = msgSplit[0];

            switch (request)
            {
                case "chat":
                    string content = string.Join("", msgSplit.Skip(1).ToArray());
                    TextBoxWriteLine(ChatTextBox, "<상대> {0}", content);
                    break;
            }
        }

        public void Sent(string msg)
        {
            string[] msgSplit = msg.Split();
            string request = msgSplit[0];


            switch (request)
            {
                case "chat":
                    string content = string.Join("", msgSplit.Skip(1).ToArray());
                    TextBoxWriteLine(ChatTextBox, "<나> {0}", content);
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

        private void Connected()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                ConsoleTextBox.Document.Blocks.Clear();
                ChatTextBox.IsEnabled = true;
                ChatInputTextBox.IsEnabled = true;
            }));
        }

        private void BoardGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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
        }

        public void DrawPiece(int row, int col, int color)
        {
            var piece = new Ellipse()
            {
                Height = 43,
                Width = 43,
                StrokeThickness = 3,
                Margin = new Thickness() { Top = 7 + 52 * row, Left = 7 + 52 * col }
            };

            if (color == Constants.White)
            {
                piece.Fill = Brushes.White;
                piece.Stroke = Brushes.Black;
            }
            else
            {
                piece.Fill = Brushes.Black;
                piece.Stroke = Brushes.White;
            }

            BoardCanvas.Children.Add(piece);
        }

        public void Disconnected()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                ChatTextBox.IsEnabled = false;
                ChatInputTextBox.IsEnabled = false;

                ChatTextBox.Document.Blocks.Clear();
                ChatInputTextBox.Clear();
            }));

            if (Server != null)
            {
                Server.Close();
                Server = null;
            }
            else if (Client != null)
            {
                Client.Close();
                Client = null;
            }
            MessageBox.Show("연결이 끊겼습니다", "Othello#");
        }
    }
}
