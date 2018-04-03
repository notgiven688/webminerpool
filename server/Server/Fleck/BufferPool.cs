// https://github.com/statianzo/Fleck

// The MIT License

// Copyright (c) 2010-2016 Jason Staten

// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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

