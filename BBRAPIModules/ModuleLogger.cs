using log4net;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBRAPIModules
{
    internal class ModuleLogger : ILog
    {
        private readonly ILog baseLogger;

        public ModuleLogger(ILog baseLogger)
        {
            this.baseLogger = baseLogger;
        }

        public bool IsDebugEnabled => this.baseLogger.IsDebugEnabled;

        public bool IsInfoEnabled => this.baseLogger.IsInfoEnabled;

        public bool IsWarnEnabled => this.baseLogger.IsWarnEnabled;

        public bool IsErrorEnabled => this.baseLogger.IsErrorEnabled;

        public bool IsFatalEnabled => this.baseLogger.IsFatalEnabled;

        public ILogger Logger => this.baseLogger.Logger;

        public void Debug(object message) => this.baseLogger.Debug($"[] {message}");
        public void Debug(object message, Exception exception) => throw new NotImplementedException();
        public void DebugFormat(string format, params object[] args) => throw new NotImplementedException();
        public void DebugFormat(string format, object arg0) => throw new NotImplementedException();
        public void DebugFormat(string format, object arg0, object arg1) => throw new NotImplementedException();
        public void DebugFormat(string format, object arg0, object arg1, object arg2) => throw new NotImplementedException();
        public void DebugFormat(IFormatProvider provider, string format, params object[] args) => throw new NotImplementedException();
        public void Error(object message) => throw new NotImplementedException();
        public void Error(object message, Exception exception) => throw new NotImplementedException();
        public void ErrorFormat(string format, params object[] args) => throw new NotImplementedException();
        public void ErrorFormat(string format, object arg0) => throw new NotImplementedException();
        public void ErrorFormat(string format, object arg0, object arg1) => throw new NotImplementedException();
        public void ErrorFormat(string format, object arg0, object arg1, object arg2) => throw new NotImplementedException();
        public void ErrorFormat(IFormatProvider provider, string format, params object[] args) => throw new NotImplementedException();
        public void Fatal(object message) => throw new NotImplementedException();
        public void Fatal(object message, Exception exception) => throw new NotImplementedException();
        public void FatalFormat(string format, params object[] args) => throw new NotImplementedException();
        public void FatalFormat(string format, object arg0) => throw new NotImplementedException();
        public void FatalFormat(string format, object arg0, object arg1) => throw new NotImplementedException();
        public void FatalFormat(string format, object arg0, object arg1, object arg2) => throw new NotImplementedException();
        public void FatalFormat(IFormatProvider provider, string format, params object[] args) => throw new NotImplementedException();
        public void Info(object message) => throw new NotImplementedException();
        public void Info(object message, Exception exception) => throw new NotImplementedException();
        public void InfoFormat(string format, params object[] args) => throw new NotImplementedException();
        public void InfoFormat(string format, object arg0) => throw new NotImplementedException();
        public void InfoFormat(string format, object arg0, object arg1) => throw new NotImplementedException();
        public void InfoFormat(string format, object arg0, object arg1, object arg2) => throw new NotImplementedException();
        public void InfoFormat(IFormatProvider provider, string format, params object[] args) => throw new NotImplementedException();
        public void Warn(object message) => throw new NotImplementedException();
        public void Warn(object message, Exception exception) => throw new NotImplementedException();
        public void WarnFormat(string format, params object[] args) => throw new NotImplementedException();
        public void WarnFormat(string format, object arg0) => throw new NotImplementedException();
        public void WarnFormat(string format, object arg0, object arg1) => throw new NotImplementedException();
        public void WarnFormat(string format, object arg0, object arg1, object arg2) => throw new NotImplementedException();
        public void WarnFormat(IFormatProvider provider, string format, params object[] args) => throw new NotImplementedException();
    }
}
