namespace System.Net.Sockets
{

	public static class ObjectExtensionClass
	{
		public static string GetString(this object input)
		{
			return input == null ? string.Empty : input.ToString ();
		}
	}

}


