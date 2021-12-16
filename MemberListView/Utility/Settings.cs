using System;
#if NET5_0_OR_GREATER
using Microsoft.Extensions.Configuration;
#else
using System.Configuration;
#endif

namespace MemberListView.Utility
{
    internal class Settings
    {
        internal string[] ExcludedColumns { get; private set; }
#if NET5_0_OR_GREATER
        internal Settings(IConfiguration configuration)
        {
            ExcludedColumns = configuration[Constants.Configuration.ExportExcludedColumns]
                                    ?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                ?? Array.Empty<string>();
        }
#else
        internal Settings()
        {
            ExcludedColumns = ConfigurationManager.AppSettings[Constants.Configuration.ExportExcludedColumns]
                                    ?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                ?? Array.Empty<string>();
        }
#endif

    }
}
