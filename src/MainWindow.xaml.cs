using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace OthelloSharp
{
    public partial class MainWindow : Window
    {
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
            string portInput = Interaction.InputBox("포트 번호를 입력하세요.\n(0 ~ 65535)");

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
                    portInput = Interaction.InputBox("잘못된 포트 번호입니다.\n포트 번호를 다시 입력하세요.\n(0 ~ 65535)");
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
            string serverAddress = Interaction.InputBox("서버 주소를 입력하세요.");

            if (serverAddress.Length == 0)
            {
                return;
            }

            string portInput = Interaction.InputBox("포트 번호를 입력하세요.\n(0 ~ 65535)");

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
                    portInput = Interaction.InputBox("잘못된 포트 번호입니다.\n포트 번호를 다시 입력하세요.\n(0 ~ 65535)");
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
                "9x9 보드판에 마우스 클릭으로 돌을 두는 방식으로 진행됩니다.\n" +
                "각자 5분의 시간이 주어지며 시간을 다 쓰는 상대는 패배합니다.\n" +
                "만약 돌을 놓을 수 있는 곳이 없다면 자동으로 턴이 넘겨집니다.\n" +
                "흑이 선공, 백이 후공이며 흑백 지정은 랜덤입니다.\n" +
                "이 외의 룰은 보드게임 오델로와 같습니다.\n",
                "게임 설명"
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
            catch
            {
                TextBoxWriteLine(ConsoleTextBox, "게임 생성 실패");
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
                MessageBox.Show(e.Message);

                try
                {
                    Client.IsServerConnected();
                    TextBoxWriteLine(ConsoleTextBox, "연결 실패.");
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
                CancelButton.IsEnabled = true;
                CancelIcon.Opacity = 1;
                CancelLabel.Opacity = 1;
            }));
        }

        private void DisableCancelButton()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                CancelButton.IsEnabled = false;
                CancelIcon.Opacity = .5;
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
            MessageBox.Show("연결이 끊겼습니다", "알림");
        }
    }
}
