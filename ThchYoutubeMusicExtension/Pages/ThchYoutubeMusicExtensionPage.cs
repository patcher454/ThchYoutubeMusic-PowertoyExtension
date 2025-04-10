﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using ThchYoutubeMusicExtension.Commands;
using ThchYoutubeMusicExtension.Util;

namespace ThchYoutubeMusicExtension;

internal sealed partial class ThchYoutubeMusicExtensionPage : DynamicListPage
{

    private List<ListItem> _allItems;
    private List<ListItem> _historyItems;
    private readonly YoutubeMusicApiClient _apiClient;
    private readonly SettingsManager _settingsManager;

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

    public List<ListItem> Query(string query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.Run(() => QueryAsync(query).ConfigureAwait(false).GetAwaiter().GetResult()).Result;
    }

    private async Task<List<ListItem>> QueryAsync(string query)
    {
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
            var searchResult = await _apiClient.Search(query);

            if (searchResult != null)
            {
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
            results.AddRange(filteredHistoryItems);
        }
        return results;
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _allItems = [.. Query(newSearch)];
        RaiseItemsChanged(0);
    }

    public override IListItem[] GetItems() => [.. _allItems];
} 
