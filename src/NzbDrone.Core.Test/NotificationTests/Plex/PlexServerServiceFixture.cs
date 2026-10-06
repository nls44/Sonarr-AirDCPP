using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Notifications.Plex.Server;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.NotificationTests.Plex
{
    [TestFixture]
    public class PlexServerServiceFixture : CoreTest<PlexServerService>
    {
        [SetUp]
        public void SetUp()
        {
            Mocker.SetConstant<ICacheManager>(Mocker.Resolve<CacheManager>());
        }

        [Test]
        public void should_update_the_tv_section_containing_the_resolved_episode_path()
        {
            var settings = new PlexServerSettings
            {
                Host = "plex"
            };
            var episodePath = @"/mnt/ext_3/sonarr/Nicely Formatted Series/Season 01/Episode.mkv";
            var resolvedEpisodePath = @"/mnt/plex/X264/Full.Series.Folder/Season 01/Episode.mkv";
            var series = new Series
            {
                Path = @"/mnt/ext_3/sonarr/Nicely Formatted Series"
            };
            var sections = new List<PlexSection>
            {
                new PlexSection
                {
                    Id = 1,
                    Type = "show",
                    Locations = new List<PlexSectionLocation>
                    {
                        new PlexSectionLocation { Path = "/mnt/plex/X264" }
                    }
                }
            };

            Mocker.GetMock<IConfigService>()
                  .SetupGet(v => v.CopyUsingSymlinks)
                  .Returns(true);
            Mocker.GetMock<IPlexServerProxy>()
                  .Setup(v => v.GetTvSections(settings))
                  .Returns(sections);
            Mocker.GetMock<IMediaPathResolver>()
                  .Setup(v => v.Resolve(episodePath))
                  .Returns(resolvedEpisodePath);

            Subject.UpdateLibrary(new List<EpisodeFile>
            {
                new EpisodeFile { Path = episodePath }
            }, series, settings);

            Mocker.GetMock<IPlexServerProxy>()
                  .Verify(v => v.Update(1, @"/mnt/plex/X264/Full.Series.Folder/Season 01", settings), Times.Once());
        }
    }
}
