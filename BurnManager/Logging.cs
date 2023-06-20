using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    
    public class Logging
    {
        private object _lockObj = new object();

        public Action<Log>? OnAddLogCallback = null;
        public Action? OnClearCallback = null;

        public bool Enabled = true;
        public bool CallbacksOnly = false;

        private List<Log> _logs = new List<Log>();
        public IReadOnlyList<Log> Logs
        {
            get
            {
                lock (_lockObj)
                {
                    return _logs.AsReadOnly();
                }
            }
        }

        public Logging()
        {
        }

        public void Add(string? callingFunction, string log)
        {
            lock (_lockObj)
            {
                if (!Enabled)
                {
                    return;
                }

                Log newlog = new Log(callingFunction, log);

                if (!CallbacksOnly)
                {
                    _logs.Add(newlog);
                }

                if (!(OnAddLogCallback is null))
                {
                    OnAddLogCallback(newlog);
                }
            }
        }

        public void Add(Log log)
        {
            lock (_lockObj)
            {
                this.Add(log.CallingFunction, log.LogText);
            }
        }

        public void Clear()
        {
            lock (_lockObj)
            {
                if (!Enabled)
                {
                    return;
                }

                if (!CallbacksOnly)
                {
                    _logs.Clear();
                }

                if (!(OnClearCallback is null))
                {
                    OnClearCallback();
                }
            }
        }
    }

    public class Log
    {
        public string? CallingFunction = null;
        public string LogText = "";

        public Log(string? CallingFunction, string LogText)
        {
            this.CallingFunction = CallingFunction;
            this.LogText = LogText;
        }
    }
}
