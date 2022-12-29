using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;

namespace MemberListView.Composing
{
    public class PackageManifestComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.ManifestFilters().Append<MemberListViewManifestFilter>();
        }
    }

    public class MemberListViewManifestFilter : IManifestFilter
    {
        public void Filter(List<PackageManifest> manifests)
        {
            var version = typeof(MemberListViewManifestFilter).Assembly.GetName().Version?.ToString();

            if (version is not null)
            {
                manifests.Add(new PackageManifest
                {
                    PackageName = "MemberListView",
                    AllowPackageTelemetry = true,
                    Version = version
                });
            }
        }
    }
}