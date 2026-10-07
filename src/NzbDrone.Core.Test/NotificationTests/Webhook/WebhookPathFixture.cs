using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Notifications.Webhook;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tags;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using CoreWebhook = NzbDrone.Core.Notifications.Webhook.Webhook;

namespace NzbDrone.Core.Test.NotificationTests.Webhook
{
    [TestFixture]
    public class WebhookPathFixture : CoreTest<CoreWebhook>
    {
        [Test]
        public void should_send_resolved_series_and_episode_paths()
        {
            var seriesPath = @"/mnt/ext_3/sonarr/Nicely Formatted Series";
            var resolvedEpisodeFilePath = @"/mnt/plex/X264/Full.Series.Folder/Season 01/Episode.mkv";
            var resolvedEpisodeFolderPath = Path.GetDirectoryName(resolvedEpisodeFilePath);
            var series = new Series
            {
                Id = 1,
                Path = seriesPath,
                Title = "Series"
            };
            var episodeFile = new EpisodeFile
            {
                Series = series,
                RelativePath = Path.Combine("Season 01", "Episode.mkv"),
                Episodes = new List<Episode>(),
                Quality = new QualityModel()
            };
            WebhookImportPayload payload = null;

            Subject.Definition = new NotificationDefinition
            {
                Settings = new WebhookSettings()
            };

            Mocker.GetMock<ITagRepository>()
                  .Setup(v => v.GetTags(It.IsAny<HashSet<int>>()))
                  .Returns(new List<Tag>());
            Mocker.GetMock<IMediaPathResolver>()
                  .Setup(v => v.ResolveEpisodeFolderPath(seriesPath, episodeFile.RelativePath))
                  .Returns(resolvedEpisodeFolderPath);
            Mocker.GetMock<IMediaPathResolver>()
                  .Setup(v => v.ResolveEpisodeFilePath(seriesPath, episodeFile.RelativePath))
                  .Returns(resolvedEpisodeFilePath);
            Mocker.GetMock<IWebhookProxy>()
                  .Setup(v => v.SendWebhook(It.IsAny<WebhookPayload>(), It.IsAny<WebhookSettings>()))
                  .Callback<WebhookPayload, WebhookSettings>((p, _) => payload = (WebhookImportPayload)p);

            Subject.OnDownload(new DownloadMessage
            {
                Series = series,
                EpisodeInfo = new LocalEpisode
                {
                    Series = series
                },
                EpisodeFile = episodeFile,
                OldFiles = new List<DeletedEpisodeFile>()
            });

            payload.Should().NotBeNull();
            payload.Series.Path.Should().Be(resolvedEpisodeFolderPath);
            payload.EpisodeFile.Path.Should().Be(resolvedEpisodeFilePath);
        }

        [Test]
        public void should_send_resolved_import_complete_paths()
        {
            var seriesPath = @"/mnt/ext_3/sonarr/Nicely Formatted Series";
            var resolvedEpisodeFilePath = @"/mnt/plex/X264/Full.Series.Folder/Season 01/Episode.mkv";
            var resolvedEpisodeFolderPath = Path.GetDirectoryName(resolvedEpisodeFilePath);
            var series = new Series
            {
                Id = 1,
                Path = seriesPath,
                Title = "Series"
            };
            var episodeFile = new EpisodeFile
            {
                Series = series,
                RelativePath = Path.Combine("Season 01", "Episode.mkv"),
                Episodes = new List<Episode>(),
                Quality = new QualityModel()
            };
            WebhookImportCompletePayload payload = null;

            Subject.Definition = new NotificationDefinition
            {
                Settings = new WebhookSettings()
            };

            Mocker.GetMock<ITagRepository>()
                  .Setup(v => v.GetTags(It.IsAny<HashSet<int>>()))
                  .Returns(new List<Tag>());
            Mocker.GetMock<IMediaPathResolver>()
                  .Setup(v => v.ResolveEpisodeFolderPath(seriesPath, episodeFile.RelativePath))
                  .Returns(resolvedEpisodeFolderPath);
            Mocker.GetMock<IMediaPathResolver>()
                  .Setup(v => v.ResolveEpisodeFilePath(seriesPath, episodeFile.RelativePath))
                  .Returns(resolvedEpisodeFilePath);
            Mocker.GetMock<IWebhookProxy>()
                  .Setup(v => v.SendWebhook(It.IsAny<WebhookPayload>(), It.IsAny<WebhookSettings>()))
                  .Callback<WebhookPayload, WebhookSettings>((p, _) => payload = (WebhookImportCompletePayload)p);

            Subject.OnImportComplete(new ImportCompleteMessage
            {
                Series = series,
                Episodes = new List<Episode>(),
                EpisodeFiles = new List<EpisodeFile> { episodeFile },
                DestinationPath = resolvedEpisodeFolderPath
            });

            payload.Should().NotBeNull();
            payload.Series.Path.Should().Be(resolvedEpisodeFolderPath);
            payload.EpisodeFiles[0].Path.Should().Be(resolvedEpisodeFilePath);
            payload.DestinationPath.Should().Be(resolvedEpisodeFolderPath);
        }
    }
}
