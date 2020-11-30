using Discord;
using Microsoft.Extensions.Logging;

namespace HunterPilferBot.Core
{
	public static class Extensions
	{

        /// <summary>
        /// Transforms a discord <see cref="LogSeverity"/> to a microsoft <see cref="LogLevel"/>
        /// </summary>
        /// <param name="severity">The discord severity</param>
        /// <returns>The microsoft log level</returns>
        public static LogLevel TransformLogLevel(this LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Critical: return LogLevel.Critical;
                case LogSeverity.Debug: return LogLevel.Debug;
                case LogSeverity.Error: return LogLevel.Error;
                case LogSeverity.Info: return LogLevel.Information;
                case LogSeverity.Verbose: return LogLevel.Trace;
                case LogSeverity.Warning: return LogLevel.Warning;
            }

            return (LogLevel)(int)severity;
        }
    }
}
