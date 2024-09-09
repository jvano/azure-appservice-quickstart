using Microsoft.ServiceHosting.Tools.DevelopmentFabric;
using System.Diagnostics;

namespace CloudServiceBootstrapper
{
    internal class ConsoleRoleInstanceLogger : IRoleInstanceLogger
    {
        public ConsoleRoleInstanceLogger(string id)
        {
            this.Id = id;
        }

        public string Id { get; private set; }


        public LoggingLevel GetLoggingLevel()
        {
            return LoggingLevel.Info;
        }

        public void Log(RoleInstanceLogEntry log)
        {
            if (string.IsNullOrEmpty(log.Name))
            {
                Log($"{log.TimeStamp} [{this.Id}] {log.Level.ToString()}:{log.EventParameters[string.Empty]}", log.Level);
            }
        }

        public void Log(string message, LoggingLevel level)
        {
            switch (level)
            {
                case LoggingLevel.Must:
                case LoggingLevel.Critical:
                    Trace.TraceError(message);
                    break;
                case LoggingLevel.Warning:
                    Trace.TraceWarning(message);
                    break;
                case LoggingLevel.Info:
                    Trace.TraceInformation(message);
                    break;
                case LoggingLevel.Debugging:
                default:
                    Trace.WriteLine(message);
                    break;
            }
        }
    }
}
