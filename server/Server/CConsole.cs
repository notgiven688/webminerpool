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
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Server
{

    public static class CConsole
    {

        // Info, Alert, Warning
        private static readonly object locker = new object();
        private static readonly bool enabled = true;

        static CConsole()
        {
            try
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.ResetColor();
            }
            catch
            {
                // ArgumentException, SecurityException, IOException.
                enabled = false;
            }
        }

        private static void ColorConsole(Action consoleAction, ConsoleColor foreground)
        {

            if (enabled)
            {
                lock (locker)
                {
                    Console.ForegroundColor = foreground;
                    consoleAction();
                    Console.ResetColor();
                }
            }
            else
            {
                consoleAction();
            }
        }

        private static void ColorConsole(Action consoleAction, ConsoleColor foreground, ConsoleColor background)
        {

            if (enabled)
            {
                lock (locker)
                {
                    Console.ForegroundColor = foreground;
                    Console.BackgroundColor = background;
                    consoleAction();
                    Console.ResetColor();
                }
            }
            else
            {
                consoleAction();
            }
        }

        public static void ColorInfo(Action consoleAction)
        {
            ColorConsole(consoleAction, ConsoleColor.Cyan);
        }

        public static void ColorWarning(Action consoleAction)
        {
            ColorConsole(consoleAction, ConsoleColor.Yellow);
        }

        public static void ColorAlert(Action consoleAction)
        {
            ColorConsole(consoleAction, ConsoleColor.Red);
        }

    }

}