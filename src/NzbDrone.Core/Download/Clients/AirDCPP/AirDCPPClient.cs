using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.AirDCPP;
using NzbDrone.Core.RemotePathMappings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Download.Clients.AirDCPP
{
    public class AirDCPPClient : DirectConnectClientBase<AirDCPPClientSettings>
    {
        public override string Name => "AirDCPP";

        protected readonly IAirDCPPProxy _airDCPPProxy;

        public AirDCPPClient(
                      IHttpClient httpClient,
                      IConfigService configService,
                      IDiskProvider diskProvider,
                      IRemotePathMappingService remotePathMappingService,
                      Logger logger)
            : base(httpClient, configService, diskProvider, remotePathMappingService, logger)
        {
            _airDCPPProxy = new AirDCPPProxy(httpClient, logger);
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            var results = _airDCPPProxy.GetQueueHistory(Settings);
            return results.Select(r => new DownloadClientItem
            {
                DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this),
                DownloadId = r.id.ToString(),
                RemainingTime = TimeSpan.FromSeconds((long)r.seconds_left),
                RemainingSize = (long)(r.size - r.downloaded_bytes),
                TotalSize = (long)r.size,
                Title = r.name,
                OutputPath = _remotePathMappingService.RemapRemoteToLocal(Settings.Host, new OsPath(r.target)),
                Status = r.seconds_left > 0 ? DownloadItemStatus.Downloading : DownloadItemStatus.Completed,
            });
        }

        public override DownloadClientInfo GetStatus()
        {
            return new DownloadClientInfo
            {
                IsLocalhost = false,
                OutputRootFolders = new List<OsPath> { new OsPath(Settings.DownloadDirectory) }
            };
        }

        public override void RemoveItem(string downloadId, bool deleteData)
        {
            throw new NotImplementedException();
        }

        protected override void Test(List<ValidationFailure> failures)
        {
        }

        protected override string AddFromId(string searchInstanceAndResultIds, string title)
        {
            return _airDCPPProxy.DownloadBySearchInstanceAndResultId(Settings, searchInstanceAndResultIds, title);
        }
    }
}