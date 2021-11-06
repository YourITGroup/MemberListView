using System;
#if NET5_0_OR_GREATER
using Microsoft.Extensions.Logging;
#else
using Umbraco.Core.Logging;
#endif

namespace MemberListView.Utility
{
    internal class Logging<TType>
    {
#if NET5_0_OR_GREATER
        internal ILogger<TType> logger;
        internal Logging(ILogger<TType> logger)
#else
        internal ILogger logger;
        internal Logging(ILogger logger)
#endif
        {
            this.logger = logger;
        }

        internal void LogWarning(string message, params object[] args)
        {
#if NET5_0_OR_GREATER
            logger.LogWarning(message, args);
#else
            logger.Warn<TType>(message, args);
#endif
        }

        internal void LogWarning(Exception ex, string message, params object[] args)
        {
#if NET5_0_OR_GREATER
            logger.LogWarning(ex, message, args);
#else
            logger.Warn<TType>(ex, message, args);
#endif
        }
    }
}
