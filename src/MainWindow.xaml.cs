using Microsoft.VisualBasic;
using System;
using System.Threading;
using System.Windows;
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
        
        private void MenuButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Rectangle)sender).Opacity = 1;
        }

        private void MenuButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Rectangle)sender).Opacity = 0;
        }

        private void CloseButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
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
                "키보드 방향키로 보드판의 커서를 이동시킬 수 있습니다.\n" +
                "엔터를 입력하면 커서가 있는 위치에 돌을 놓습니다.\n" +
                "만약 돌을 놓을 수 있는 곳이 없다면 자동으로 턴이 넘겨집니다.\n" +
                "흑이 선공, 백이 후공이며 흑백 지정은 랜덤입니다.\n" +
                "이 외의 룰은 보드게임 오델로와 같습니다.\n",
                "게임 설명"
                );
        }

        private void CancelButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ConsoleWriteLine("연결 취소.");
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

            ConsoleWriteLine(port + " 포트에서 게임 생성 중...");
            EnableCancelButton();

            try
            {
                Server.Open(port);
            }
            catch
            {
                ConsoleWriteLine("게임 생성 실패");
                DisableCancelButton();
                return;
            }

            ConsoleWriteLine("게임 생성 성공, 연결을 기다리는 중...");

            while (true)
            {
                try
                {
                    if (Server.IsClientConnected())
                    {
                        DisableCancelButton();
                        ConsoleWriteLine("연결 완료.");
                        break;
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        private void JoinGame(string server, ushort port)
        {
            Client = new Client(this);

            ConsoleWriteLine("연결을 시도하는 중...");
            EnableCancelButton();

            try
            {
                Client.Connect(server, port);
                ConsoleWriteLine("연결 완료.");
            }
            catch
            {
                try
                {
                    Client.IsServerConnected();
                    ConsoleWriteLine("연결 실패.");
                }
                catch
                {

                }
            }
            DisableCancelButton();
        }

        private void EnableCancelButton()
        {
            Dispatcher.Invoke(new Action(() => {
                CancelButton.IsEnabled = true;
                CancelIcon.Opacity = 1;
                CancelLabel.Opacity = 1;
                })
            );
        }

        private void DisableCancelButton()
        {
            Dispatcher.Invoke(new Action(() => {
                CancelButton.IsEnabled = false;
                CancelIcon.Opacity = .5;
                CancelLabel.Opacity = .5;
                })
            );
        }

        private void ConsoleWriteLine(string text)
        {
            Dispatcher.Invoke(new Action(() => {
                ConsoleTextBox.AppendText(text);
                ConsoleTextBox.AppendText(Environment.NewLine);
                ConsoleTextBox.ScrollToEnd();
                })
            );
        }

        public void Disconnected()
        {
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
