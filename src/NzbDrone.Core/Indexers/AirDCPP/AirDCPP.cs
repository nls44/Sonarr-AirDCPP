using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.AirDCPP
{
    public class AirDCPP : HttpIndexerBase<AirDCPPSettings>
    {
        public override string Name => "airdcpp";
        public override bool SupportsRss => false;

        public override DownloadProtocol Protocol => DownloadProtocol.DirectConnect;

        public AirDCPP(IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, Logger logger, ILocalizationService localizationService)
            : base(httpClient, indexerStatusService, configService, parsingService, logger, localizationService)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AirDCPPRequestGenerator(_httpClient, _logger) { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AirDCPPParser(Settings);
        }
    }
}
