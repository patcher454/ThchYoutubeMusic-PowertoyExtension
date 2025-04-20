// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ThchYoutubeMusicExtension.Util;
using ThchYoutubeMusicExtension.Commands;

namespace ThchYoutubeMusicExtension;

internal sealed partial class ThchYoutubeMusicExtensionPage : DynamicListPage, IDisposable
{
    private List<ListItem> _allItems;
    private List<ListItem> _historyItems;
    private readonly YoutubeMusicApiClient _apiClient;
    private readonly SettingsManager _settingsManager;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task<List<ListItem>>? _currentSearchTask;
    private readonly object _itemsLock = new();
    private CancellationTokenSource? _debounceCts;

    public ThchYoutubeMusicExtensionPage(SettingsManager settingsManager)
    {
        Icon = IconHelpers.FromRelativePath("Assets\\youtube-music.png");
        Title = "th-ch/youtube-music";
        Name = "Open";
        _settingsManager = settingsManager;
        _apiClient = YoutubeMusicApiClient.Initialize(_settingsManager.ApiServerAddress);
        _historyItems = _settingsManager.LoadHistory();
        _allItems = _settingsManager.LoadHistory();
    }

    private async Task<List<ListItem>> DoSearchAsync(string query, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IEnumerable<ListItem>? filteredHistoryItems = null;

        _historyItems = _settingsManager.LoadHistory();

        if (_historyItems != null)
        {
            if (string.IsNullOrEmpty(query))
            {
                filteredHistoryItems = _historyItems;
            }
            else
            {
                filteredHistoryItems = _settingsManager.ShowHistory != Properties.Resource.history_none ? ListHelpers.FilterList(_historyItems, query).OfType<ListItem>() : null;
            }
        }

        var results = new List<ListItem>();

        if (!string.IsNullOrEmpty(query))
        {
            ct.ThrowIfCancellationRequested();
            var searchResult = await _apiClient.Search(query);

            if (searchResult != null)
            {
                ct.ThrowIfCancellationRequested();
                var result = new ListItem(new InsertCommand(searchResult, _settingsManager, QueueInsertPosition.INSERT_AFTER_CURRENT_VIDEO))
                {
                    Icon = new IconInfo(searchResult.ThumbnailUrl),
                    Title = searchResult.Title,
                    Tags = searchResult.AccessibilityData.Split("•").Select(s => new Tag(s.Trim())).ToArray(),
                    MoreCommands =
                    [
                        new CommandContextItem(new InsertCommand(searchResult, _settingsManager, QueueInsertPosition.INSERT_AT_END))
                    ]
                };
                results.Add(result);
            }
        }

        if (filteredHistoryItems != null)
        {
            ct.ThrowIfCancellationRequested();
            results.AddRange(filteredHistoryItems);
        }
        return results;
    }

    public override async void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (newSearch == oldSearch)
        {
            return;
        }

        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var currentCts = _debounceCts;

        try
        {
            await Task.Delay(300, currentCts.Token);

            if (currentCts.IsCancellationRequested)
            {
                return;
            }

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken searchCancellationToken = _cancellationTokenSource.Token;

            _currentSearchTask = DoSearchAsync(newSearch, searchCancellationToken);

            _ = ProcessSearchResultsAsync(_currentSearchTask, newSearch);

        }
        catch (TaskCanceledException)
        {
        }
        finally
        {
        }
    }

    private async Task ProcessSearchResultsAsync(Task<List<ListItem>> searchTask, string newSearch)
    {
        try
        {
            List<ListItem> results = await searchTask;

            if (_currentSearchTask == searchTask && searchTask.IsCompletedSuccessfully)
            {
                lock (_itemsLock)
                {
                    _allItems = results;
                }
                RaiseItemsChanged(results.Count);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
        }
        finally
        {
        }
    }

    public override IListItem[] GetItems()
    {
        lock (_itemsLock)
        {
            return [.. _allItems];
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        GC.SuppressFinalize(this);
    }
}
