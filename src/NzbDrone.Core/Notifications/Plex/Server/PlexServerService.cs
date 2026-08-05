using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Plex.Server
{
    public interface IPlexServerService
    {
        void UpdateLibrary(Series series, PlexServerSettings settings);
        void UpdateLibrary(IEnumerable<Series> series, PlexServerSettings settings);
        void UpdateLibrary(IEnumerable<EpisodeFile> episodes, Series series, PlexServerSettings settings);
        ValidationFailure Test(PlexServerSettings settings);
    }

    public class PlexServerService : IPlexServerService
    {
        private readonly ICached<Version> _versionCache;
        private readonly IPlexServerProxy _plexServerProxy;
        private readonly IRootFolderService _rootFolderService;
        private readonly ILocalizationService _localizationService;
        private readonly IConfigService _configService;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public PlexServerService(
            ICacheManager cacheManager,
            IPlexServerProxy plexServerProxy,
            IRootFolderService rootFolderService,
            ILocalizationService localizationService,
            IConfigService configService,
            IDiskProvider diskProvider,
            Logger logger)
        {
            _versionCache = cacheManager.GetCache<Version>(GetType(), "versionCache");
            _plexServerProxy = plexServerProxy;
            _rootFolderService = rootFolderService;
            _localizationService = localizationService;
            _configService = configService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public void UpdateLibrary(Series series, PlexServerSettings settings)
        {
            UpdateLibrary([series], settings);
        }

        public void UpdateLibrary(IEnumerable<Series> multipleSeries, PlexServerSettings settings)
        {
            try
            {
                _logger.Debug("Sending Update Request to Plex Server");
                var watch = Stopwatch.StartNew();

                var version = _versionCache.Get(settings.Host, () => GetVersion(settings), TimeSpan.FromHours(2));
                ValidateVersion(version);

                var sections = GetSections(settings);

                foreach (var series in multipleSeries)
                {
                    UpdateSections(series, sections, settings);
                }

                _logger.Debug("Finished sending Update Request to Plex Server (took {0} ms)", watch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to Update Plex host: " + settings.Host);
                throw;
            }
        }

        public void UpdateLibrary(IEnumerable<EpisodeFile> episodes, Series series, PlexServerSettings settings)
        {
            var sections = GetSections(settings);

            if (_configService.CopyUsingSymlinks)
            {
                foreach (var episode in episodes)
                {
                    var episodeLocation = _diskProvider.GetParentFolder(episode.Path).TrimEnd(Path.DirectorySeparatorChar);

                    _logger.Debug("Searching matching section for {0}", episodeLocation);
                    var matchingSections = sections.Where(section => section.Locations.Any(location =>
                            IsSubDirectory(episodeLocation, location.Path)))
                        .ToList();

                    if (matchingSections.Any())
                    {
                        foreach (var matchingSection in matchingSections)
                        {
                            _plexServerProxy.Update(matchingSection.Id, episodeLocation, settings);
                        }
                    }
                    else
                    {
                        _logger.Warn("Failed to find matching section for {0}", episodeLocation);
                    }
                }
            }
            else
            {
                UpdateLibrary([series], settings);
            }
        }

        // Checks if the given directory is a subdirectory of the parent directory
        private bool IsSubDirectory(string directory, string parentDirectory)
        {
            var isSubDirectory = false;

            var parentDir = new DirectoryInfo(parentDirectory.TrimEnd(Path.DirectorySeparatorChar));
            var subDir = new DirectoryInfo(directory.TrimEnd(Path.DirectorySeparatorChar));

            while (subDir.Parent != null)
            {
                if (subDir.Parent.FullName == parentDir.FullName)
                {
                    isSubDirectory = true;
                    break;
                }
                else
                {
                    subDir = subDir.Parent;
                }
            }

            return isSubDirectory;
        }

        private List<PlexSection> GetSections(PlexServerSettings settings)
        {
            _logger.Debug("Getting sections from Plex host: {0}", settings.Host);

            return _plexServerProxy.GetTvSections(settings).ToList();
        }

        private void ValidateVersion(Version version)
        {
            if (version >= new Version(1, 3, 0) && version < new Version(1, 3, 1))
            {
                throw new PlexVersionException("Found version {0}, upgrade to PMS 1.3.1 to fix library updating and then restart Sonarr", version);
            }
        }

        private Version GetVersion(PlexServerSettings settings)
        {
            _logger.Debug("Getting version from Plex host: {0}", settings.Host);

            var rawVersion = _plexServerProxy.Version(settings);
            var version = new Version(Regex.Match(rawVersion, @"^(\d+[.-]){4}").Value.Trim('.', '-'));

            return version;
        }

        private void UpdateSections(Series series, List<PlexSection> sections, PlexServerSettings settings)
        {
            var rootFolderPath = _rootFolderService.GetBestRootFolderPath(series.Path);
            var seriesRelativePath = rootFolderPath.GetRelativePath(series.Path);

            // Try to update a matching section location before falling back to updating all section locations.
            foreach (var section in sections)
            {
                foreach (var location in section.Locations)
                {
                    var rootFolder = new OsPath(rootFolderPath);
                    var mappedPath = rootFolder;

                    if (settings.MapTo.IsNotNullOrWhiteSpace())
                    {
                        mappedPath = new OsPath(settings.MapTo) + (rootFolder - new OsPath(settings.MapFrom));

                        _logger.Trace("Mapping Path from {0} to {1} for partial scan", rootFolder, mappedPath);
                    }

                    if (location.Path.PathEquals(mappedPath.FullPath))
                    {
                        _logger.Debug("Updating matching section location, {0}", location.Path);
                        UpdateSectionPath(seriesRelativePath, section, location, settings);

                        return;
                    }
                }
            }

            _logger.Debug("Unable to find matching section location, updating all TV sections");

            foreach (var section in sections)
            {
                foreach (var location in section.Locations)
                {
                    UpdateSectionPath(seriesRelativePath, section, location, settings);
                }
            }
        }

        private void UpdateSectionPath(string seriesRelativePath, PlexSection section, PlexSectionLocation location, PlexServerSettings settings)
        {
            var separator = location.Path.Contains('\\') ? "\\" : "/";
            var locationRelativePath = seriesRelativePath.Replace("\\", separator).Replace("/", separator);

            // Plex location paths trim trailing extraneous separator characters,
            // unless it's a Windows drive letter (S:\) that needs to be trimmed.
            var pathToUpdate = $"{location.Path.TrimEnd(separator)}{separator}{locationRelativePath}";

            _logger.Debug("Updating section location, {0}", location.Path);
            _plexServerProxy.Update(section.Id, pathToUpdate, settings);
        }

        public ValidationFailure Test(PlexServerSettings settings)
        {
            try
            {
                _versionCache.Remove(settings.Host);
                var sections = GetSections(settings);

                if (sections.Empty())
                {
                    return new ValidationFailure("Host", _localizationService.GetLocalizedString("NotificationsPlexValidationNoTvLibraryFound"));
                }
            }
            catch (PlexAuthenticationException ex)
            {
                _logger.Error(ex, "Unable to connect to Plex Media Server");
                return new ValidationFailure("AuthToken", _localizationService.GetLocalizedString("NotificationsValidationInvalidAuthenticationToken"));
            }
            catch (PlexException ex)
            {
                return new NzbDroneValidationFailure("Host", _localizationService.GetLocalizedString("NotificationsValidationUnableToConnect", new Dictionary<string, object> { { "exceptionMessage", ex.Message } }));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to connect to Plex Media Server");

                return new NzbDroneValidationFailure("Host", _localizationService.GetLocalizedString("NotificationsValidationUnableToConnectToService", new Dictionary<string, object> { { "serviceName", "Plex Media Server" } }))
                       {
                           DetailedDescription = ex.Message
                       };
            }

            return null;
        }
    }
}
