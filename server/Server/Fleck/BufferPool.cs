using System;
using System.Collections.Generic;

namespace Fleck
{
	public static class BufferPool
	{

		public static int WebsocketClosed = 0;
		public static int WebsocketCreated = 0;


		private static Queue<byte[]> pooledBuffers = new Queue<byte[]>();
		private static HashSet<byte[]> hs = new HashSet<byte[]>();

		const int BufferSize = 1024 * 4;

		public static int Created = 0;
		public static int Returned = 0;


		public static void InitBufferPool (int size = 10000)
		{
			while (size-- > 0) {
				pooledBuffers.Enqueue(new byte[BufferSize]);
				hs.Add (new byte[BufferSize]);
			}
		}

		public static void ReturnBuffer(byte[] buffer)
		{
			
			lock (pooledBuffers) 
			{
				if (hs.Contains (buffer))
				return;

				Returned++;

				hs.Add (buffer);


				System.Array.Clear (buffer, 0, BufferSize);

				pooledBuffers.Enqueue (buffer); 

			}
		}

		public static byte[] RequestBuffer( byte[] data)
		{
			var retval = RequestBuffer ();
			System.Array.Copy (data, retval, data.Length);
			return retval;
		}

		public static byte[] RequestBuffer()
		{
			lock (pooledBuffers) {

				Created++;

							
				if (pooledBuffers.Count == 0) return new byte[BufferSize];
				
				byte[] retval =  pooledBuffers.Dequeue ();

		
				hs.Remove (retval);

				return retval;

			}

		}
	}
}

