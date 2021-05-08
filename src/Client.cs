using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace OthelloSharp
{
	public class Client
	{
		public class AsyncObject
		{
			public byte[] Buffer;
			public Socket WorkingSocket;
			public AsyncObject(int bufferSize)
			{
				Buffer = new byte[bufferSize];
			}
		}

		private MainWindow mainWindow;
		private Socket ClientSocket = null;
		private AsyncCallback ReceiveHandler;
		private AsyncCallback SendHandler;

		public Client(MainWindow mainWindow)
		{
			this.mainWindow = mainWindow;

			ReceiveHandler = new AsyncCallback(HandleDataReceive);
			SendHandler = new AsyncCallback(HandleDataSend);
		}

		public bool IsServerConnected()
		{
			return ClientSocket.Poll(1000, SelectMode.SelectRead) && (ClientSocket.Available == 0);
		}

		public void Connect(string serverAddress, ushort port)
		{
			ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			ClientSocket.Connect(serverAddress, port);

			AsyncObject ao = new AsyncObject(4096);
			ao.WorkingSocket = ClientSocket;

			ClientSocket.BeginReceive(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, ReceiveHandler, ao);
		}

		public void Close()
		{
			ClientSocket.Close();
		}

		public void SendMessage(string message)
		{
			AsyncObject ao = new AsyncObject(1);
			ao.Buffer = Encoding.Unicode.GetBytes(message);
			ao.WorkingSocket = ClientSocket;

			ClientSocket.BeginSend(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, SendHandler, ao);
		}

		private void HandleDataReceive(IAsyncResult ar)
		{
			AsyncObject ao = (AsyncObject)ar.AsyncState;

			int recvBytes;

			try
			{
				recvBytes = ao.WorkingSocket.EndReceive(ar);
			}
			catch
            {
				mainWindow.Disconnected();
				return;
            }

			if (recvBytes > 0)
			{
				byte[] msgByte = new byte[recvBytes];
				Array.Copy(ao.Buffer, msgByte, recvBytes);
				MessageBox.Show(Encoding.Unicode.GetString(msgByte));
			}
			ao.WorkingSocket.BeginReceive(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, ReceiveHandler, ao);
		}

		private void HandleDataSend(IAsyncResult ar)
		{
			AsyncObject ao = (AsyncObject)ar.AsyncState;
			int sentBytes = ao.WorkingSocket.EndSend(ar);

			if (sentBytes > 0)
			{
				byte[] msgByte = new byte[sentBytes];
				Array.Copy(ao.Buffer, msgByte, sentBytes);
				MessageBox.Show(Encoding.Unicode.GetString(msgByte));
			}
		}
	}
}