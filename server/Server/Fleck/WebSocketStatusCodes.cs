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

namespace Fleck
{
    public static class WebSocketStatusCodes
    {
        public const ushort NormalClosure = 1000;
        public const ushort GoingAway = 1001;
        public const ushort ProtocolError = 1002;
        public const ushort UnsupportedDataType = 1003;
        public const ushort NoStatusReceived = 1005;
        public const ushort AbnormalClosure = 1006;
        public const ushort InvalidFramePayloadData = 1007;
        public const ushort PolicyViolation = 1008;
        public const ushort MessageTooBig = 1009;
        public const ushort MandatoryExt = 1010;
        public const ushort InternalServerError = 1011;
        public const ushort TLSHandshake = 1015;
        
        public const ushort ApplicationError = 3000;
        
        public static ushort[] ValidCloseCodes = new []{
            NormalClosure, GoingAway, ProtocolError, UnsupportedDataType,
            InvalidFramePayloadData, PolicyViolation, MessageTooBig,
            MandatoryExt, InternalServerError
        };
    }
}

