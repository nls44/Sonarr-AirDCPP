using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Download
{
    public abstract class DirectConnectClientBase<TSettings> : DownloadClientBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        protected readonly IHttpClient _httpClient;

        protected DirectConnectClientBase(IHttpClient httpClient,
                                   IConfigService configService,
                                   IDiskProvider diskProvider,
                                   IRemotePathMappingService remotePathMappingService,
                                   Logger logger,
                                   ILocalizationService localizationService)
            : base(configService, diskProvider, remotePathMappingService, logger, localizationService)
        {
            _httpClient = httpClient;
        }

        public override DownloadProtocol Protocol => DownloadProtocol.DirectConnect;

        protected abstract string AddFromId(string id, RemoteEpisode remoteEpisode);

        public override Task<string> Download(RemoteEpisode remoteEpisode, IIndexer indexer)
        {
            var id = remoteEpisode.Release.DownloadUrl;
            _logger.Info("Adding report [{0}] to the queue.", remoteEpisode.Release.Title);
            var downloadResult = AddFromId(id, remoteEpisode);
            return Task.FromResult(downloadResult);
        }
    }
}
