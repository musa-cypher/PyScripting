
using System;
using System.Collections.Generic;
using System.Text;

namespace PyScripting.ScriptingEngine
{
    public class PyStream
    {
        public event EventHandler<WriteEventArgs> WriteEvent;

        public void write(string data)
        {
            var args = new WriteEventArgs { Data = data };
            WriteEvent?.Invoke(this, args);
        }

    }

    public class WriteEventArgs : EventArgs
    {
        public string Data { get; set; }
    }
}
