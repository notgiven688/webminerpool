using System;

namespace Server
{
	public static class Execute
	{
		public static void IgnoreExceptions(Action a) 
		{
			try
			{
				a();
			}
			catch
			{
			}
		}
	}
}

