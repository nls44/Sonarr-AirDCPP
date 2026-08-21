using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Download.Clients.AirDCPP;
using NzbDrone.Core.Indexers.AirDCPP;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.IndexerTests.AirDCPPTests
{
    [TestFixture]
    public class AirDCPPProxyFixture : CoreTest<AirDCPPProxy>
    {
        [Test]
        public void should_use_season_folder_by_default()
        {
            new AirDCPPClientSettings().UseSeasonFolder.Should().BeTrue();
        }

        [Test]
        public void should_use_download_directory_when_season_folders_are_disabled()
        {
            var settings = new AirDCPPClientSettings
            {
                DownloadDirectory = "downloads",
                UseSeasonFolder = false
            };

            AirDCPPProxy.GetTargetDirectory(settings, CreateRemoteEpisode()).Should().Be(settings.DownloadDirectory);
        }

        [Test]
        public void should_send_configured_download_directory_and_match_queue_title_when_season_folders_are_disabled()
        {
            var settings = new AirDCPPClientSettings
            {
                DownloadDirectory = "downloads",
                UseSeasonFolder = false
            };

            var remoteEpisode = CreateRemoteEpisode();
            var requests = new List<HttpRequest>();

            Mocker.GetMock<IHttpClient>()
                .Setup(client => client.Execute(It.IsAny<HttpRequest>()))
                .Returns<HttpRequest>(request =>
                {
                    requests.Add(request);

                    var content = request.Url.FullUri.Contains("/download")
                        ? "{}"
                        : $"[{{\"id\":123,\"name\":\"{remoteEpisode.Release.Title}\"}}]";

                    return new HttpResponse(request, new HttpHeader(), Encoding.UTF8.GetBytes(content));
                });

            Subject.DownloadBySearchInstanceAndResultId(settings, "1:2", remoteEpisode).Should().Be("123");

            var downloadRequest = requests.Single(request => request.Url.FullUri.Contains("/download"));
            var query = Json.Deserialize<AirDCPPProxy.HubDownloadQuery>(Encoding.UTF8.GetString(downloadRequest.ContentData));

            query.target_directory.Should().Be(settings.DownloadDirectory);
        }

        [Test]
        public void should_use_series_and_season_folder_when_enabled()
        {
            var settings = new AirDCPPClientSettings
            {
                DownloadDirectory = "downloads",
                UseSeasonFolder = true
            };

            var remoteEpisode = CreateRemoteEpisode();
            var expected = Path.Combine(settings.DownloadDirectory, remoteEpisode.Series.Title, "Season 2") + Path.DirectorySeparatorChar;

            AirDCPPProxy.GetTargetDirectory(settings, remoteEpisode).Should().Be(expected);
        }

        [Test]
        public void should_use_series_folder_without_season_folder_when_series_season_folders_are_disabled()
        {
            var settings = new AirDCPPClientSettings
            {
                DownloadDirectory = "downloads",
                UseSeasonFolder = true
            };

            var remoteEpisode = CreateRemoteEpisode();
            remoteEpisode.Series.SeasonFolder = false;
            var expected = Path.Combine(settings.DownloadDirectory, remoteEpisode.Series.Title) + Path.DirectorySeparatorChar;

            AirDCPPProxy.GetTargetDirectory(settings, remoteEpisode).Should().Be(expected);
        }

        private static RemoteEpisode CreateRemoteEpisode()
        {
            return new RemoteEpisode
            {
                Release = new ReleaseInfo { Title = "The Series S02E02 Release" },
                ParsedEpisodeInfo = new ParsedEpisodeInfo { SeasonNumber = 2 },
                Series = new Series { Title = "The Series", SeasonFolder = true }
            };
        }
    }
}
