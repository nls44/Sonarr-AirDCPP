using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class MediaPathResolverFixture : CoreTest<NzbDrone.Core.MediaFiles.MediaPathResolver>
    {
        [Test]
        public void should_resolve_paths_through_the_disk_provider()
        {
            var path = @"/mnt/ext_3/sonarr/Series";
            var resolvedPath = @"/mnt/plex/TV/Series";

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetRealPath(path))
                  .Returns(resolvedPath);

            Subject.Resolve(path).Should().Be(resolvedPath);
        }

        [Test]
        public void should_resolve_episode_file_paths_through_the_disk_provider()
        {
            var seriesPath = @"/mnt/ext_3/sonarr/Series";
            var relativePath = @"Season 1/Episode.mkv";
            var fullPath = System.IO.Path.Combine(seriesPath, relativePath);
            var resolvedPath = @"/mnt/plex/TV/Series/Season 1/Episode.mkv";

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetRealPath(fullPath))
                  .Returns(resolvedPath);

            Subject.ResolveEpisodeFilePath(seriesPath, relativePath).Should().Be(resolvedPath);
        }

        [Test]
        public void should_apply_mapping_to_the_resolved_path()
        {
            var path = @"/mnt/ext_3/sonarr/Series";
            var resolvedPath = @"/mnt/plex/TV/Series";

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetRealPath(path))
                  .Returns(resolvedPath);

            Subject.ResolveMappedPath(path, @"/mnt/plex", @"/media").Should().Be(@"/media/TV/Series");
        }
    }
}
