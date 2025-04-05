using Microsoft.CommandPalette.Extensions.Toolkit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ThchYoutubeMusicExtension.Util;

namespace ThchYoutubeMusicExtension.Commands
{
    public partial class SearchCommand : InvokableCommand
    {
        private readonly SettingsManager _settingsManager;

        private readonly YoutubeMusicApiClient _apiClient;

        public SearchResult Arguments { get; set; }

        public SearchCommand(SearchResult arguments, SettingsManager settingsManager)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments), "SearchResult cannot be null");
            }

            if (settingsManager == null)
            {
                throw new ArgumentNullException(nameof(settingsManager), "SettingsManager cannot be null");
            }

            _apiClient = YoutubeMusicApiClient.Initialize("http://127.0.0.1:26538/");
            Arguments = arguments;
            
            // null 체크 추가
            Icon = new IconInfo(arguments.ThumbnailUrl ?? string.Empty);
            Name = arguments.Title ?? string.Empty;
            Id = arguments.VideoId ?? string.Empty;
            _settingsManager = settingsManager;
        }

        public override CommandResult Invoke()
        {
            // 비동기 작업을 동기적으로 실행
            var task = ExecuteAsync();
            task.Wait();
            
            return CommandResult.Dismiss();
        }

        private async Task ExecuteAsync()
        {
            // 큐에 추가
            await _apiClient.AddToQueueAsync(Arguments.VideoId, QueueInsertPosition.INSERT_AFTER_CURRENT_VIDEO);
            
            // 서버 처리를 위한 지연
            await Task.Delay(3000);
            
            // 다음 트랙으로 이동
            await _apiClient.NextAsync();

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
