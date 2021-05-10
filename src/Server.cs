using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace OthelloSharp
{
	public class Server
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
		private Socket ConnectedClient = null;
		private Socket ServerSocket = null;
		private AsyncCallback ReceiveHandler;
		private AsyncCallback SendHandler;
		private AsyncCallback AcceptHandler;

		public Server(MainWindow mainWindow)
		{
			this.mainWindow = mainWindow;

			ReceiveHandler = new AsyncCallback(HandleDataReceive);
			SendHandler = new AsyncCallback(HandleDataSend);
			AcceptHandler = new AsyncCallback(HandleClientConnectionRequest);
		}

		public bool IsClientConnected()
		{
			return ServerSocket.Poll(1000, SelectMode.SelectRead) && (ServerSocket.Available == 0);
		}

		public void Open(ushort port)
		{
			ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			ServerSocket.Bind(new IPEndPoint(IPAddress.Any, port));
			ServerSocket.Listen(5);
			ServerSocket.BeginAccept(AcceptHandler, null);
		}

		public void Close()
		{
			ServerSocket.Close();
		}

		public void Send(string format, params object[] args)
		{
			AsyncObject ao = new AsyncObject(1);
			ao.Buffer = Encoding.Unicode.GetBytes(string.Format(format, args));
			ao.WorkingSocket = ConnectedClient;

			try
			{
				ConnectedClient.BeginSend(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, SendHandler, ao);
			}
			catch
            {
				mainWindow.Disconnected();
            }
		}

		private void HandleClientConnectionRequest(IAsyncResult ar)
		{
			Socket sockClient;

			try
			{
				sockClient = ServerSocket.EndAccept(ar);
			}
			catch
            {
				return;
            }

			AsyncObject ao = new AsyncObject(4096);
			ao.WorkingSocket = sockClient;
			ConnectedClient = sockClient;

			sockClient.BeginReceive(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, ReceiveHandler, ao);
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
				mainWindow.Recieved(Encoding.Unicode.GetString(msgByte));
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
				mainWindow.Sent(Encoding.Unicode.GetString(msgByte));
			}
		}
	}
}