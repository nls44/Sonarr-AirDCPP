using System.IO;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaPathResolver
    {
        string Resolve(string path);
        string ResolveEpisodeFilePath(string seriesPath, string relativePath);
        string ResolveEpisodeFolderPath(string seriesPath, string relativePath);
        string ResolveMappedPath(string path, string mapFrom, string mapTo);
    }

    public class MediaPathResolver : IMediaPathResolver
    {
        private readonly IDiskProvider _diskProvider;

        public MediaPathResolver(IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
        }

        public string Resolve(string path)
        {
            if (path.IsNullOrWhiteSpace())
            {
                return path;
            }

            return _diskProvider.GetRealPath(path);
        }

        public string ResolveEpisodeFilePath(string seriesPath, string relativePath)
        {
            if (seriesPath.IsNullOrWhiteSpace() || relativePath.IsNullOrWhiteSpace())
            {
                return Path.Combine(seriesPath ?? string.Empty, relativePath ?? string.Empty);
            }

            return Resolve(Path.Combine(seriesPath, relativePath));
        }

        public string ResolveEpisodeFolderPath(string seriesPath, string relativePath)
        {
            if (relativePath.IsNullOrWhiteSpace())
            {
                return Resolve(seriesPath);
            }

            return ResolveEpisodeFilePath(seriesPath, relativePath).GetParentPath();
        }

        public string ResolveMappedPath(string path, string mapFrom, string mapTo)
        {
            var resolvedPath = Resolve(path);

            if (mapTo.IsNullOrWhiteSpace())
            {
                return resolvedPath;
            }

            var pathToMap = IsWithin(mapFrom, resolvedPath) ? resolvedPath : path;

            return (new OsPath(mapTo) + (new OsPath(pathToMap) - new OsPath(mapFrom))).ToString();
        }

        private static bool IsWithin(string parentPath, string childPath)
        {
            return parentPath.IsNotNullOrWhiteSpace() &&
                   (parentPath.PathEquals(childPath) || parentPath.IsParentPath(childPath));
        }
    }
}
