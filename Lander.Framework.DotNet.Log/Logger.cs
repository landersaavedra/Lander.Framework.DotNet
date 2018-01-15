using log4net;
using log4net.Config;
using System;
using System.IO;

namespace Lander.Framework.DotNet.Log
{
    public class Logger : IDisposable
    {
        private ILog log;
        private Stream ConfigStream;

        private void Ini(Type ClassType)
        {
            this.log = LogManager.GetLogger(ClassType);
        }

        public Logger(Type ClassType)
        {
            this.Ini(ClassType);
            XmlConfigurator.Configure();
        }

        public Logger(Type ClassType, string LoggerConfigFilePath)
        {
            this.Ini(ClassType);
            try
            {
                this.ConfigStream = new StreamReader(LoggerConfigFilePath).BaseStream;
            }
            catch (FileNotFoundException ex)
            {
                throw new FileNotFoundException("Logger Config file was not found. The logger config file path is wrong!!");
            }
            XmlConfigurator.Configure(this.ConfigStream);
            this.ConfigStream.Close();
            this.ConfigStream = (Stream)null;
        }

        public Logger(Type ClassType, Stream LoggerConfigFile)
        {
            this.Ini(ClassType);
            XmlConfigurator.Configure(LoggerConfigFile);
        }

        private bool IsErrorEnabled
        {
            get
            {
                return this.log.IsErrorEnabled;
            }
        }

        private bool IsFatalEnabled
        {
            get
            {
                return this.log.IsFatalEnabled;
            }
        }

        private bool IsWarnEnabled
        {
            get
            {
                return this.log.IsWarnEnabled;
            }
        }

        private bool IsInfoEnabled
        {
            get
            {
                return this.log.IsInfoEnabled;
            }
        }

        private bool IsDebugEnabled
        {
            get
            {
                return this.log.IsDebugEnabled;
            }
        }

        private void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.log.ErrorFormat(provider, format, args);
        }

        private void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            this.log.ErrorFormat(format, arg0, arg1, arg2);
        }

        private void ErrorFormat(string format, object arg0, object arg1)
        {
            this.log.ErrorFormat(format, arg0, arg1);
        }

        private void ErrorFormat(string format, object arg0)
        {
            this.log.ErrorFormat(format, arg0);
        }

        private void ErrorFormat(string format, params object[] args)
        {
            this.log.ErrorFormat(format, args);
        }

        public void Info(object message, Exception exception)
        {
            if (!this.IsInfoEnabled)
                return;
            this.log.Info(message, exception);
        }

        public void Info(object message)
        {
            if (!this.IsInfoEnabled)
                return;
            this.log.Info(message);
        }

        public void Debug(object message, Exception exception)
        {
            if (!this.IsDebugEnabled)
                return;
            this.log.Debug(message, exception);
        }

        public void Debug(object message)
        {
            if (!this.IsDebugEnabled)
                return;
            this.log.Debug(message);
        }

        public void Warn(object message, Exception exception)
        {
            if (!this.IsWarnEnabled)
                return;
            this.log.Warn(message, exception);
        }

        public void Warn(object message)
        {
            if (!this.IsWarnEnabled)
                return;
            this.log.Warn(message);
        }

        private void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.log.WarnFormat(provider, format, args);
        }

        private void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            this.log.WarnFormat(format, arg0, arg1, arg2);
        }

        private void WarnFormat(string format, object arg0, object arg1)
        {
            this.log.WarnFormat(format, arg0, arg1);
        }

        private void WarnFormat(string format, object arg0)
        {
            this.log.WarnFormat(format, arg0);
        }

        private void WarnFormat(string format, params object[] args)
        {
            this.log.WarnFormat(format, args);
        }

        public void Fatal(object message, Exception exception)
        {
            if (!this.IsFatalEnabled)
                return;
            this.log.Fatal(message, exception);
        }

        public void Fatal(object message)
        {
            if (!this.IsFatalEnabled)
                return;
            this.log.Fatal(message);
        }

        public void Error(object message, Exception exception)
        {
            if (!this.IsErrorEnabled)
                return;
            this.log.Error(message, exception);
        }

        public void Error(object message)
        {
            if (!this.IsErrorEnabled)
                return;
            this.log.Error(message);
        }

        private void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.log.InfoFormat(provider, format, args);
        }

        private void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            this.log.InfoFormat(format, arg0, arg1, arg2);
        }

        private void InfoFormat(string format, object arg0, object arg1)
        {
            this.log.InfoFormat(format, arg0, arg1);
        }

        private void InfoFormat(string format, object arg0)
        {
            this.log.InfoFormat(format, arg0);
        }

        private void InfoFormat(string format, params object[] args)
        {
            this.log.InfoFormat(format, args);
        }

        private void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.log.FatalFormat(provider, format, args);
        }

        private void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            this.log.FatalFormat(format, arg0, arg1, arg2);
        }

        private void FatalFormat(string format, object arg0, object arg1)
        {
            this.log.FatalFormat(format, arg0, arg1);
        }

        private void FatalFormat(string format, object arg0)
        {
            this.log.FatalFormat(format, arg0);
        }

        private void FatalFormat(string format, params object[] args)
        {
            this.log.FatalFormat(format, args);
        }

        private void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.log.DebugFormat(provider, format, args);
        }

        private void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            this.log.DebugFormat(format, arg0, arg1, arg2);
        }

        private void DebugFormat(string format, object arg0, object arg1)
        {
            this.log.DebugFormat(format, arg0, arg1);
        }

        private void DebugFormat(string format, object arg0)
        {
            this.log.DebugFormat(format, arg0);
        }

        private void DebugFormat(string format, params object[] args)
        {
            this.log.DebugFormat(format, args);
        }

        public void Dispose()
        {
            if (this.ConfigStream != null)
            {
                this.ConfigStream.Close();
                this.ConfigStream = (Stream)null;
            }
            if (this.log == null)
                return;
            this.log = (ILog)null;
        }
    }
}
