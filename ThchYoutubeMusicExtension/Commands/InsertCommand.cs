using Microsoft.CommandPalette.Extensions.Toolkit;

using System;
using System.Threading.Tasks;

using ThchYoutubeMusicExtension.Util;

namespace ThchYoutubeMusicExtension.Commands
{

    public partial class InsertCommand : InvokableCommand
    {
        private readonly SettingsManager _settingsManager;
        private readonly QueueInsertPosition _insertType;

        public SearchResult Arguments { get; set; }

        public InsertCommand(SearchResult arguments, SettingsManager settingsManager, QueueInsertPosition insertType)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments), "SearchResult cannot be null");
            }

            if (settingsManager == null)
            {
                throw new ArgumentNullException(nameof(settingsManager), "SettingsManager cannot be null");
            }

            Arguments = arguments;
            _insertType = insertType;

            Name = _insertType == QueueInsertPosition.INSERT_AFTER_CURRENT_VIDEO
                ? Properties.Resource.Insert_After_Play
                : Properties.Resource.Insert_End;

            _settingsManager = settingsManager;
        }

        public override CommandResult Invoke()
        {
            var task = ExecuteAsync();
            task.Wait();

            return CommandResult.KeepOpen();
        }

        private async Task ExecuteAsync()
        {
            var client = YoutubeMusicApiClient.Initialize(_settingsManager.ApiServerAddress);
            await client.AddToQueueAsync(Arguments.VideoId, _insertType);

            if (_insertType == QueueInsertPosition.INSERT_AFTER_CURRENT_VIDEO)
            {
                await Task.Delay(1500);
                await client.NextAsync();
            }

            if (_settingsManager.ShowHistory != Properties.Resource.history_none)
            {
                var history = new SearchHistory()
                {
                    ThumbnailUrl = Arguments.ThumbnailUrl,
                    Title = Arguments.Title,
                    VideoId = Arguments.VideoId,
                    AccessibilityData = Arguments.AccessibilityData,
                };
                _settingsManager.SaveHistory(history);
            }
        }
    }
}
