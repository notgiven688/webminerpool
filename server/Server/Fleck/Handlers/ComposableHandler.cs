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

namespace Fleck.Handlers
{
    public class ComposableHandler : IHandler
    {
        public Func<string, byte[]> Handshake = s => new byte[0];
        public Func<string, byte[]> TextFrame = x => new byte[0];
        public Func<byte[], byte[]> BinaryFrame = x => new byte[0];
        public Action<List<byte>> ReceiveData = delegate { };
        public Func<byte[], byte[]> PingFrame = i => new byte[0];
        public Func<byte[], byte[]> PongFrame = i => new byte[0];
        public Func<int, byte[]> CloseFrame = i => new byte[0];
        
        private readonly List<byte> _data = new List<byte>();

        public byte[] CreateHandshake(string subProtocol = null)
        {
            return Handshake(subProtocol);
        }

        public void Receive(IEnumerable<byte> data)
        {
            _data.AddRange(data);
            
            ReceiveData(_data);
        }
        
        public byte[] FrameText(string text)
        {
            return TextFrame(text);
        }
        
        public byte[] FrameBinary(byte[] bytes)
        {
            return BinaryFrame(bytes);
        }
        
        public byte[] FramePing(byte[] bytes)
        {
            return PingFrame(bytes);
        }
        
        public byte[] FramePong(byte[] bytes)
        {
            return PongFrame(bytes);
        }
        
        public byte[] FrameClose(int code)
        {
            return CloseFrame(code);
        }
    }
}

