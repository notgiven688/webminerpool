namespace System.Net.Sockets
{
    public static class SocketExtensions
	{
		private const int BytesPerLong = 4;
		private const int BitsPerByte = 8;

		public static void SetKeepAlive(this Socket socket, UInt32 keepAliveInterval, UInt32 retryInterval)
		{
			int size = sizeof(UInt32);
			UInt32 on = 1;

			byte[] inArray = new byte[size * 3];
			Array.Copy(BitConverter.GetBytes(on), 0, inArray, 0, size);
			Array.Copy(BitConverter.GetBytes(keepAliveInterval), 0, inArray, size, size);
			Array.Copy(BitConverter.GetBytes(retryInterval), 0, inArray, size * 2, size);
			socket.IOControl(IOControlCode.KeepAliveValues, inArray, null);
		}
	}
		
	public static class ObjectExtensionClass
	{
		public static string GetString(this object input)
		{
			return input == null ? string.Empty : input.ToString ();
		}
	}

}


