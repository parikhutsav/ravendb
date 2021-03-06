using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Raven.Server.Discovery
{
	///<summary>
	/// Listen to presence notifications
	///</summary>
	public class ClusterDiscoveryHost : IDisposable
	{
		private Socket socket;
		private readonly byte[] buffer = new byte[1024 * 4];

		///<summary>
		/// Starts listening to network notifications
		///</summary>
		public void Start()
		{
			IsStop = false;
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
			socket.Bind(new IPEndPoint(IPAddress.Any, 12392));
			NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface adapter in adapters.Where(card => card.OperationalStatus == OperationalStatus.Up))
			{
				IPInterfaceProperties properties = adapter.GetIPProperties();
				foreach (var ipaddress in properties.UnicastAddresses.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork))
				{
					socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, (object)new MulticastOption(IPAddress.Parse("224.0.0.1"), ipaddress.Address));
				}
			}

			StartListening();
		}

		private void StartListening()
		{
			var socketAsyncEventArgs = new SocketAsyncEventArgs
			{
				RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0),
			};
			socketAsyncEventArgs.Completed += Completed;
			socketAsyncEventArgs.SetBuffer(buffer, 0, buffer.Length);

			bool startedAsync;
			try
			{
				startedAsync = socket.ReceiveFromAsync(socketAsyncEventArgs);
			}
			catch (Exception)
			{
				return;
			}
			if (startedAsync == false)
				Completed(this, socketAsyncEventArgs);
		}

		private void Completed(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
		{
			if (IsStop)
				return;

			using (socketAsyncEventArgs)
			{
				try
				{
					using (var stream = new MemoryStream(socketAsyncEventArgs.Buffer, 0, socketAsyncEventArgs.BytesTransferred))
					using (var streamReader = new StreamReader(stream))
					{
						var senderId = streamReader.ReadLine();
						var clusterManagerUrl = streamReader.ReadLine();
						var clientDiscovered = new ClientDiscoveredEventArgs
						{
							ClusterManagerUrl = clusterManagerUrl,
						};

						Guid result;
						if (Guid.TryParse(senderId, out result))
						{
							clientDiscovered.SenderId = result;
							InvokeClientDiscovered(clientDiscovered);
						}
					}
				}
				catch
				{
				}
				StartListening();
			}
		}

		///<summary>
		/// Notify when a client is discovered
		///</summary>
		public event EventHandler<ClientDiscoveredEventArgs> ClientDiscovered;

		private void InvokeClientDiscovered(ClientDiscoveredEventArgs e)
		{
			EventHandler<ClientDiscoveredEventArgs> handler = ClientDiscovered;
			if (handler != null) handler(this, e);
		}

		/// <summary>
		/// Notification raised when a client is discovered
		/// </summary>
		public class ClientDiscoveredEventArgs : EventArgs
		{
			/// <summary>
			/// The cluster manager URL
			/// </summary>
			public string ClusterManagerUrl { get; set; }

			/// <summary>
			/// The id of the sender
			/// </summary>
			public Guid SenderId { get; set; }
		}

		public void Dispose()
		{
			if (socket != null)
				socket.Dispose();
		}

		public void Stop()
		{
			IsStop = true;
		}

		protected bool IsStop { get; set; }
	}
}