using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications.Synology
{
    public class SynologyIndexer : NotificationBase<SynologyIndexerSettings>
    {
        private readonly ISynologyIndexerProxy _indexerProxy;
        private readonly IMediaPathResolver _pathResolver;
        private readonly ILocalizationService _localizationService;

        public SynologyIndexer(ISynologyIndexerProxy indexerProxy, IMediaPathResolver pathResolver, ILocalizationService localizationService)
        {
            _indexerProxy = indexerProxy;
            _pathResolver = pathResolver;
            _localizationService = localizationService;
        }

        public override string Link => "https://www.synology.com";
        public override string Name => "Synology Indexer";

        public override void OnDownload(DownloadMessage message)
        {
            if (Settings.UpdateLibrary)
            {
                foreach (var oldFile in message.OldFiles)
                {
                    var fullPath = _pathResolver.ResolveEpisodeFilePath(message.Series.Path, oldFile.EpisodeFile.RelativePath);

                    _indexerProxy.DeleteFile(fullPath);
                }

                {
                    var fullPath = _pathResolver.ResolveEpisodeFilePath(message.Series.Path, message.EpisodeFile.RelativePath);

                    _indexerProxy.AddFile(fullPath);
                }
            }
        }

        public override void OnImportComplete(ImportCompleteMessage message)
        {
            if (Settings.UpdateLibrary)
            {
                UpdateFolders(message.Series, message.EpisodeFiles);
            }
        }

        public override void OnRename(Series series, List<RenamedEpisodeFile> renamedFiles)
        {
            if (Settings.UpdateLibrary)
            {
                UpdateFolders(series, renamedFiles.Select(x => x.EpisodeFile));
            }
        }

        public override void OnEpisodeFileDelete(EpisodeDeleteMessage deleteMessage)
        {
            if (Settings.UpdateLibrary)
            {
                var fullPath = _pathResolver.ResolveEpisodeFilePath(deleteMessage.Series.Path, deleteMessage.EpisodeFile.RelativePath);
                _indexerProxy.DeleteFile(fullPath);
            }
        }

        public override void OnSeriesAdd(SeriesAddMessage message)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.UpdateFolder(_pathResolver.Resolve(message.Series.Path));
            }
        }

        public override void OnSeriesDelete(SeriesDeleteMessage deleteMessage)
        {
            if (deleteMessage.DeletedFiles)
            {
                if (Settings.UpdateLibrary)
                {
                    _indexerProxy.DeleteFolder(_pathResolver.Resolve(deleteMessage.Series.Path));
                }
            }
        }

        private void UpdateFolders(Series series, IEnumerable<EpisodeFile> episodeFiles)
        {
            var folders = episodeFiles?.Where(e => e?.RelativePath.IsNotNullOrWhiteSpace() == true)
                                    .Select(e => _pathResolver.ResolveEpisodeFolderPath(series.Path, e.RelativePath))
                                    .Distinct()
                                    .ToList();

            if (folders.Empty())
            {
                folders = new List<string> { _pathResolver.Resolve(series.Path) };
            }

            foreach (var folder in folders)
            {
                _indexerProxy.UpdateFolder(folder);
            }
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(TestConnection());

            return new ValidationResult(failures);
        }

        protected virtual ValidationFailure TestConnection()
        {
            if (!OsInfo.IsLinux)
            {
                return new ValidationFailure(string.Empty, _localizationService.GetLocalizedString("NotificationsSynologyValidationInvalidOs"));
            }

            if (!_indexerProxy.Test())
            {
                return new ValidationFailure(string.Empty, _localizationService.GetLocalizedString("NotificationsSynologyValidationTestFailed"));
            }

            return null;
        }
    }
}
