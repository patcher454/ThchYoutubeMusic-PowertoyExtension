using Microsoft.CommandPalette.Extensions.Toolkit;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

using ThchYoutubeMusicExtension.Commands;
using ThchYoutubeMusicExtension.Properties;

namespace ThchYoutubeMusicExtension.Util
{
    public class SettingsManager : JsonSettingsManager
    {
        private readonly string _historyPath;

        private static readonly string _namespace = "youtube-music";

        private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

        private static readonly List<ChoiceSetSetting.Choice> _choices =
        [
            new ChoiceSetSetting.Choice(Properties.Resource.history_none, Properties.Resource.history_none),
            new ChoiceSetSetting.Choice(Properties.Resource.history_1, Properties.Resource.history_1),
            new ChoiceSetSetting.Choice(Properties.Resource.history_5, Properties.Resource.history_5),
            new ChoiceSetSetting.Choice(Properties.Resource.history_10, Properties.Resource.history_10),
            new ChoiceSetSetting.Choice(Properties.Resource.history_20, Properties.Resource.history_20),
        ];

        private readonly ChoiceSetSetting _showHistory = new(
            Namespaced(nameof(ShowHistory)),
            Properties.Resource.plugin_show_history,
            Properties.Resource.plugin_show_history,
            _choices);

        private readonly TextSetting _apiServer = new(
            Namespaced(nameof(ApiServerAddress)),
            Properties.Resource.api_server_address,
            Properties.Resource.api_server_address,
            "http://127.0.0.1:26538/");

        public string ShowHistory => _showHistory.Value ?? string.Empty;

        public string ApiServerAddress => _apiServer.Value ?? "http://127.0.0.1:26538/";


        internal static string SettingsJsonPath()
        {
            var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");
            Directory.CreateDirectory(directory);

            // now, the state is just next to the exe
            return Path.Combine(directory, "settings.json");
        }

        internal static string HistoryStateJsonPath()
        {
            var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");
            Directory.CreateDirectory(directory);

            // now, the state is just next to the exe
            return Path.Combine(directory, "youtube_music_history.json");
        }

        public void SaveHistory(SearchHistory historyItem)
        {
            if (historyItem == null)
            {
                return;
            }

            try
            {
                List<SearchHistory> historyItems;

                // Check if the file exists and load existing history
                if (File.Exists(_historyPath))
                {
                    var existingContent = File.ReadAllText(_historyPath);
                    historyItems = JsonConvert.DeserializeObject<List<SearchHistory>>(existingContent) ?? [];
                }
                else
                {
                    historyItems = [];
                }

                // Add the new history item
                historyItems.Add(historyItem);

                historyItems = historyItems.DistinctBy(x => x.VideoId).ToList();

                // Determine the maximum number of items to keep based on ShowHistory
                if (int.TryParse(ShowHistory, out var maxHistoryItems) && maxHistoryItems > 0)
                {
                    // Keep only the most recent `maxHistoryItems` items
                    while (historyItems.Count > maxHistoryItems)
                    {
                        historyItems.RemoveAt(0); // Remove the oldest item
                    }
                }

                // Serialize the updated list back to JSON and save it
                var serializerSettings = new JsonSerializerSettings 
                { 
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor 
                };
                var historyJson = JsonConvert.SerializeObject(historyItems, serializerSettings);
                File.WriteAllText(_historyPath, historyJson);
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage(new LogMessage() { Message = ex.ToString() });
            }
        }

        public List<ListItem> LoadHistory()
        {
            try
            {
                if (!File.Exists(_historyPath))
                {
                    return [];
                }

                // Read and deserialize JSON into a list of HistoryItem objects
                var fileContent = File.ReadAllText(_historyPath);
                var settings = new JsonSerializerSettings 
                { 
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor 
                };
                var historyItems = JsonConvert.DeserializeObject<List<SearchHistory>>(fileContent, settings) ?? [];

                // Convert each HistoryItem to a ListItem
                var listItems = new List<ListItem>();
                foreach (var historyItem in historyItems)
                {
                    try 
                    {
                        // Check if historyItem is null
                        if (historyItem == null)
                        {
                            ExtensionHost.LogMessage(new LogMessage() { Message = "Null history item found, skipping." });
                            continue;
                        }
                        
                        // Check if required fields are null
                        if (historyItem.AccessibilityData == null || 
                            historyItem.Title == null ||
                            historyItem.ThumbnailUrl == null || 
                            historyItem.VideoId == null)
                        {
                            ExtensionHost.LogMessage(new LogMessage() { Message = "History item contains null fields, skipping." });
                            continue;
                        }

                        var searchResult = new SearchResult
                        {
                            ThumbnailUrl = historyItem.ThumbnailUrl,
                            Title = historyItem.Title,
                            VideoId = historyItem.VideoId,
                            AccessibilityData = historyItem.AccessibilityData
                        };
                        
                        listItems.Add(new ListItem(new InsertCommand(searchResult, this, QueueInsertPosition.INSERT_AFTER_CURRENT_VIDEO))
                        {
                            Icon = new IconInfo(historyItem.ThumbnailUrl),
                            Title = historyItem.Title,
                            Tags = historyItem.AccessibilityData.Split("•").Select(s => new Tag(s.Trim())).ToArray(),
                            MoreCommands = 
                            [
                                new CommandContextItem(new InsertCommand(searchResult, this, QueueInsertPosition.INSERT_AT_END))
                            ]
                            
                        });
                    }
                    catch (Exception ex)
                    {
                        ExtensionHost.LogMessage(new LogMessage() { Message = $"Error processing history item: {ex}" });
                    }
                }

                listItems.Reverse();
                return listItems;
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage(new LogMessage() { Message = ex.ToString() });
                return [];
            }
        }

        public SettingsManager()
        {
            FilePath = SettingsJsonPath();
            _historyPath = HistoryStateJsonPath();

            Settings.Add(_showHistory);
            Settings.Add(_apiServer);

            // Load settings from file upon initialization
            LoadSettings();

            Settings.SettingsChanged += (s, a) => this.SaveSettings();
        }

        private void ClearHistory()
        {
            try
            {
                if (File.Exists(_historyPath))
                {
                    // Delete the history file
                    File.Delete(_historyPath);

                    // Log that the history was successfully cleared
                    ExtensionHost.LogMessage(new LogMessage() { Message = "History cleared successfully." });
                }
                else
                {
                    // Log that there was no history file to delete
                    ExtensionHost.LogMessage(new LogMessage() { Message = "No history file found to clear." });
                }
            }
            catch (Exception ex)
            {
                // Log any exception that occurs
                ExtensionHost.LogMessage(new LogMessage() { Message = $"Failed to clear history: {ex}" });
            }
        }

        public override void SaveSettings()
        {
            base.SaveSettings();
            try
            {
                if (ShowHistory == Properties.Resource.history_none)
                {
                    ClearHistory();
                }
                else if (int.TryParse(ShowHistory, out var maxHistoryItems) && maxHistoryItems > 0)
                {
                    // Trim the history file if there are more items than the new limit
                    if (File.Exists(_historyPath))
                    {
                        var existingContent = File.ReadAllText(_historyPath);
                        var settings = new JsonSerializerSettings 
                        { 
                            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor 
                        };
                        var historyItems = JsonConvert.DeserializeObject<List<SearchHistory>>(existingContent, settings) ?? [];

                        historyItems = historyItems.DistinctBy(x => x.VideoId).ToList();

                        // Check if trimming is needed
                        if (historyItems.Count > maxHistoryItems)
                        {
                            // Trim the list to keep only the most recent `maxHistoryItems` items
                            historyItems = historyItems.Skip(historyItems.Count - maxHistoryItems).ToList();

                            // Save the trimmed history back to the file
                            var serializerSettings = new JsonSerializerSettings 
                            { 
                                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor 
                            };
                            var trimmedHistoryJson = JsonConvert.SerializeObject(historyItems, serializerSettings);
                            File.WriteAllText(_historyPath, trimmedHistoryJson);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage(new LogMessage() { Message = ex.ToString() });
            }
        }
    }
}
