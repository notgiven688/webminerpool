// The MIT License (MIT)

// Copyright (c) 2018-2019 - the webminerpool developer

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
using Fleck;

namespace Server
{

    public class EmptyConnectionInfo : IWebSocketConnectionInfo
    {
        #region IWebSocketConnectionInfo implementation
        public string SubProtocol
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public string Origin
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public string Host
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public string Path
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public string ClientIpAddress
        {
            get
            {
                return "127.0.0.1";
            }
        }
        public int ClientPort
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public IDictionary<string, string> Cookies
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public IDictionary<string, string> Headers
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public Guid Id
        {
            get
            {
                return Guid.Empty;
            }
        }
        public string NegotiatedSubProtocol
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }

    public class EmptyWebsocket : IWebSocketConnection
    {
        private static EmptyConnectionInfo eci =
            new EmptyConnectionInfo();

        #region IWebSocketConnection implementation
        public System.Threading.Tasks.Task Send(string message)
        {
            //throw new NotImplementedException ();
            return null;
        }
        public System.Threading.Tasks.Task Send(byte[] message)
        {
            throw new NotImplementedException();
        }
        public System.Threading.Tasks.Task SendPing(byte[] message)
        {
            throw new NotImplementedException();
        }
        public System.Threading.Tasks.Task SendPong(byte[] message)
        {
            throw new NotImplementedException();
        }
        public void Close()
        {

        }
        public Action OnOpen
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public Action OnClose
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public Action<string> OnMessage
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public Action<byte[]> OnBinary
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public Action<byte[]> OnPing
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public Action<byte[]> OnPong
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public Action<Exception> OnError
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public IWebSocketConnectionInfo ConnectionInfo
        {
            get
            {
                return EmptyWebsocket.eci;
            }
        }
        public bool IsAvailable
        {
            get
            {
                return false;
            }
        }
        #endregion
    }

}