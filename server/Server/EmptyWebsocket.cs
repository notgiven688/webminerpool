using System;
using System.Collections.Generic;
using Fleck;


namespace Server
{

    public class EmptyConnectionInfo : IWebSocketConnectionInfo
	{
		#region IWebSocketConnectionInfo implementation
		public string SubProtocol {
			get {
				throw new NotImplementedException ();
			}
		}
		public string Origin {
			get {
				throw new NotImplementedException ();
			}
		}
		public string Host {
			get {
				throw new NotImplementedException ();
			}
		}
		public string Path {
			get {
				throw new NotImplementedException ();
			}
		}
		public string ClientIpAddress {
			get {
				return "127.0.0.1";
			}
		}
		public int ClientPort {
			get {
				throw new NotImplementedException ();
			}
		}
		public IDictionary<string, string> Cookies {
			get {
				throw new NotImplementedException ();
			}
		}
		public IDictionary<string, string> Headers {
			get {
				throw new NotImplementedException ();
			}
		}
		public Guid Id {
			get {
				return Guid.Empty;
			}
		}
		public string NegotiatedSubProtocol {
			get {
				throw new NotImplementedException ();
			}
		}
		#endregion
	}

	public class EmptyWebsocket : IWebSocketConnection
	{
		private static EmptyConnectionInfo eci =
			new EmptyConnectionInfo();

		#region IWebSocketConnection implementation
		public System.Threading.Tasks.Task Send (string message)
		{
			//throw new NotImplementedException ();
			return null;
		}
		public System.Threading.Tasks.Task Send (byte[] message)
		{
			throw new NotImplementedException ();
		}
		public System.Threading.Tasks.Task SendPing (byte[] message)
		{
			throw new NotImplementedException ();
		}
		public System.Threading.Tasks.Task SendPong (byte[] message)
		{
			throw new NotImplementedException ();
		}
		public void Close ()
		{
			
		}
		public Action OnOpen {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}
		public Action OnClose {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}
		public Action<string> OnMessage {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}
		public Action<byte[]> OnBinary {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}
		public Action<byte[]> OnPing {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}
		public Action<byte[]> OnPong {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}
		public Action<Exception> OnError {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}
		public IWebSocketConnectionInfo ConnectionInfo {
			get {
				return EmptyWebsocket.eci;
			}
		}
		public bool IsAvailable {
			get {
				return false;
			}
		}
		#endregion
	}


}
