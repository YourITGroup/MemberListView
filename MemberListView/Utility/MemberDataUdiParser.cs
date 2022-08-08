using System.Collections.Generic;
using System.Text.RegularExpressions;
#if NET5_0_OR_GREATER
using Umbraco.Cms.Core;
#else
using Umbraco.Core;
#endif
namespace MemberListView.Utility
{
    internal sealed class MemberDataUdiParser
    {
        public MemberDataUdiParser()
        {
        }

        private static readonly Regex UdiRegex = new Regex(@"(?<udi>umb://[A-z0-9\-]+/[A-z0-9]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>
        /// Parses out media UDIs from an html string based on 'data-udi' html attributes
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static IEnumerable<Udi> FindUdis(string text)
        {
            var matches = UdiRegex.Matches(text);
            if (matches.Count == 0)
                yield break;

            foreach (Match match in matches)
            {
#if NET5_0_OR_GREATER
                if (match.Groups.Count == 2 && UdiParser.TryParse(match.Groups[1].Value, out var udi))
#else
                if (match.Groups.Count == 2 && Udi.TryParse(match.Groups[1].Value, out var udi))
#endif
                    yield return udi;
            }
        }

    }

}
