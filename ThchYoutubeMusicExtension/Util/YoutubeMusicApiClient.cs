using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.NewtonsoftJson;
using System.Text.Json.Nodes;

namespace ThchYoutubeMusicExtension
{
    /// <summary>
    /// YouTube Music API Client
    /// </summary>
    public class YoutubeMusicApiClient
    {
        private readonly string APP_NAME = "Powertoys-Extension";

        private readonly RestClient _restClient;
        private string? _accessToken;

        private static YoutubeMusicApiClient? _instance;
        private static string _baseUrl;
        private static readonly object _lock = new object();

        /// <summary>
        /// Singleton instance access property
        /// </summary>
        public static YoutubeMusicApiClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("YoutubeMusicApiClient is not initialized, call the Initialize method first.");
                }
                return _instance;
            }
        }

        /// <summary>
        /// Initialize YoutubeMusicApiClient
        /// </summary>
        /// <param name="baseUrl">API server base URL</param>
        /// <param name="accessToken">Authentication token (optional)</param>
        /// <returns>Initialized instance</returns>
        public static YoutubeMusicApiClient Initialize(string baseUrl, string? accessToken = null)
        {
            lock (_lock)
            {
                if (_instance == null || _baseUrl != baseUrl)
                {
                    _instance = new YoutubeMusicApiClient(baseUrl, accessToken);
                }
                return _instance;
            }
        }

        /// <summary>
        /// Reset instance for testing purposes
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// YouTube Music API Client constructor
        /// </summary>
        /// <param name="baseUrl">API server base URL</param>
        /// <param name="accessToken">Authentication token (optional)</param>
        private YoutubeMusicApiClient(string baseUrl, string? accessToken = null)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _restClient = new RestClient(
                _baseUrl,
                configureSerialization: s => s.UseNewtonsoftJson(new JsonSerializerSettings()
                {
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
                })
            );

            if (!string.IsNullOrEmpty(accessToken))
            {
                SetAccessToken(accessToken);
            }
        }

        /// <summary>
        /// Set access token
        /// </summary>
        /// <param name="accessToken">JWT access token</param>
        public void SetAccessToken(string accessToken)
        {
            _accessToken = accessToken;
            _restClient.AddDefaultHeader("Authorization", $"Bearer {accessToken}");
        }

        /// <summary>
        /// Authenticate through the authentication endpoint and get token
        /// </summary>
        /// <param name="id">Authentication ID</param>
        /// <returns>Authentication success status</returns>
        public async Task<bool> AuthenticateAsync()
        {
            var request = new RestRequest($"/auth/{APP_NAME}", Method.Post);
            var response = await _restClient.ExecuteAsync(request);

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                SetAccessToken(
                    JsonObject.Parse(response.Content)["accessToken"].GetValue<string>()
                );
                return true;
            }

            return false;
        }

        #region Player Controls

        /// <summary>
        /// Play previous track
        /// </summary>
        public async Task PreviousAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/previous");
        }

        /// <summary>
        /// Play next track
        /// </summary>
        public async Task NextAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/next");
        }

        /// <summary>
        /// Play
        /// </summary>
        public async Task PlayAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/play");
        }

        /// <summary>
        /// Pause
        /// </summary>
        public async Task PauseAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/pause");
        }

        /// <summary>
        /// Toggle play/pause
        /// </summary>
        public async Task TogglePlayAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/toggle-play");
        }

        /// <summary>
        /// Like
        /// </summary>
        public async Task LikeAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/like");
        }

        /// <summary>
        /// Dislike
        /// </summary>
        public async Task DislikeAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/dislike");
        }

        /// <summary>
        /// Seek to specific time
        /// </summary>
        /// <param name="seconds">Time to seek to (seconds)</param>
        public async Task SeekToAsync(double seconds)
        {
            var request = new { seconds = seconds };
            await SendRequestAsync(Method.Post, "/api/v1/seek-to", request);
        }

        /// <summary>
        /// Go backward
        /// </summary>
        /// <param name="seconds">Time to go backward (seconds)</param>
        public async Task GoBackAsync(double seconds)
        {
            var request = new { seconds = seconds };
            await SendRequestAsync(Method.Post, "/api/v1/go-back", request);
        }

        /// <summary>
        /// Go forward
        /// </summary>
        /// <param name="seconds">Time to go forward (seconds)</param>
        public async Task GoForwardAsync(double seconds)
        {
            var request = new { seconds = seconds };
            await SendRequestAsync(Method.Post, "/api/v1/go-forward", request);
        }

        #endregion

        #region Playback Settings

        /// <summary>
        /// Get shuffle state
        /// </summary>
        /// <returns>Shuffle state</returns>
        public async Task<bool?> GetShuffleAsync()
        {
            var response = await SendRequestAsync<ShuffleResponse>(Method.Get, "/api/v1/shuffle");
            return response?.State;
        }

        /// <summary>
        /// Set shuffle
        /// </summary>
        public async Task ShuffleAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/shuffle");
        }

        /// <summary>
        /// Get repeat mode
        /// </summary>
        /// <returns>Repeat mode</returns>
        public async Task<RepeatMode?> GetRepeatModeAsync()
        {
            var response = await SendRequestAsync<RepeatModeResponse>(Method.Get, "/api/v1/repeat-mode");
            if (response?.Mode == null)
                return null;

            return Enum.Parse<RepeatMode>(response.Mode);
        }

        /// <summary>
        /// Switch repeat mode
        /// </summary>
        /// <param name="iteration">Number of repeat button clicks</param>
        public async Task SwitchRepeatAsync(int iteration)
        {
            var request = new { Iteration = iteration };
            await SendRequestAsync(Method.Post, "/api/v1/switch-repeat", request);
        }

        /// <summary>
        /// Set volume
        /// </summary>
        /// <param name="volume">Volume value (0-100)</param>
        public async Task SetVolumeAsync(int volume)
        {
            var request = new { Volume = volume };
            await SendRequestAsync(Method.Post, "/api/v1/volume", request);
        }

        /// <summary>
        /// Get current volume
        /// </summary>
        /// <returns>Current volume value</returns>
        public async Task<int> GetVolumeAsync()
        {
            var response = await SendRequestAsync<VolumeResponse>(Method.Get, "/api/v1/volume");
            return response?.State ?? 0;
        }

        /// <summary>
        /// Set full screen
        /// </summary>
        /// <param name="state">Full screen status</param>
        public async Task SetFullscreenAsync(bool state)
        {
            var request = new { State = state };
            await SendRequestAsync(Method.Post, "/api/v1/fullscreen", request);
        }

        /// <summary>
        /// Get full screen status
        /// </summary>
        /// <returns>Full screen status</returns>
        public async Task<bool> GetFullscreenAsync()
        {
            var response = await SendRequestAsync<FullscreenResponse>(Method.Get, "/api/v1/fullscreen");
            return response?.State ?? false;
        }

        /// <summary>
        /// Toggle mute
        /// </summary>
        public async Task ToggleMuteAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/toggle-mute");
        }

        #endregion

        #region Song and Queue Information

        /// <summary>
        /// Get current song information
        /// </summary>
        /// <returns>Song information</returns>
        public async Task<SongInfo?> GetSongInfoAsync()
        {
            try
            {
                return await SendRequestAsync<SongInfo>(Method.Get, "/api/v1/song");
            }
            catch (Exception ex) when (ex.Message.Contains("404") || ex.Message.Contains("204"))
            {
                return null;
            }
        }

        /// <summary>
        /// Get queue information
        /// </summary>
        /// <returns>Queue information</returns>
        public async Task<JObject?> GetQueueAsync()
        {
            try
            {
                return await SendRequestAsync<JObject>(Method.Get, "/api/v1/queue");
            }
            catch (Exception ex) when (ex.Message.Contains("404") || ex.Message.Contains("204"))
            {
                return null;
            }
        }

        /// <summary>
        /// Add song to queue
        /// </summary>
        /// <param name="videoId">Video ID</param>
        /// <param name="insertPosition">Insert position</param>
        public async Task AddToQueueAsync(string videoId, QueueInsertPosition insertPosition = QueueInsertPosition.INSERT_AT_END)
        {
            var request = new
            {
                videoId = videoId,
                insertPosition = insertPosition.ToString()
            };
            await SendRequestAsync(Method.Post, "/api/v1/queue", request);
        }

        /// <summary>
        /// Set queue index
        /// </summary>
        /// <param name="index">Index to set</param>
        public async Task SetQueueIndexAsync(int index)
        {
            var request = new { index = index };
            await SendRequestAsync(Method.Patch, "/api/v1/queue", request);
        }

        /// <summary>
        /// Clear queue
        /// </summary>
        public async Task ClearQueueAsync()
        {
            await SendRequestAsync(Method.Delete, "/api/v1/queue");
        }

        /// <summary>
        /// Move song in queue
        /// </summary>
        /// <param name="fromIndex">Original position</param>
        /// <param name="toIndex">Position to move to</param>
        public async Task MoveSongInQueueAsync(int fromIndex, int toIndex)
        {
            var request = new { toIndex = toIndex };
            await SendRequestAsync(Method.Patch, $"/api/v1/queue/{fromIndex}", request);
        }

        /// <summary>
        /// Remove song from queue
        /// </summary>
        /// <param name="index">Song index to remove</param>
        public async Task RemoveSongFromQueueAsync(int index)
        {
            await SendRequestAsync(Method.Delete, $"/api/v1/queue/{index}");
        }

        #endregion

        #region Search

        /// <summary>
        /// Search for song
        /// </summary>
        /// <param name="query">Search term</param>
        /// <returns>Search result</returns>
        public async Task<SearchResult?> Search(string query)
        {
            var request = new { query = query };
            var result = await SendRequestAsync<JObject>(Method.Post, "/api/v1/search", request);

            try
            {
                var songInfo = result?["contents"]?["tabbedSearchResultsRenderer"]?["tabs"]?[0]?["tabRenderer"]?["content"]?["sectionListRenderer"]?["contents"]?[0]?["musicCardShelfRenderer"];

                if (songInfo == null)
                {
                    return null;
                }

                string? thumbnailUrl = songInfo?["thumbnail"]?["musicThumbnailRenderer"]?["thumbnail"]?["thumbnails"]?[0]?["url"]?.Value<string>();
                string? title = songInfo?["title"]?["runs"]?[0]?["text"]?.Value<string>();
                string? videoId = songInfo?["title"]?["runs"]?[0]?["navigationEndpoint"]?["watchEndpoint"]?["videoId"]?.Value<string>();
                string? accessibilityData = songInfo?["subtitle"]?["accessibility"]?["accessibilityData"]?["label"]?.Value<string>();

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(videoId))
                {
                    return null;
                }

                return new SearchResult
                {
                    ThumbnailUrl = thumbnailUrl ?? string.Empty,
                    Title = title,
                    VideoId = videoId,
                    AccessibilityData = accessibilityData ?? string.Empty
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Send request (no response)
        /// </summary>
        private async Task SendRequestAsync(Method method, string path, object? content = null)
        {
            await SendRequestAsync<object?>(method, path, content);
        }

        /// <summary>
        /// Send request and get response
        /// </summary>
        private async Task<T?> SendRequestAsync<T>(Method method, string path, object? content = null)
        {
            if (_accessToken == null)
            {
                await AuthenticateAsync();
            }

            var request = new RestRequest(path, method);

            if (content != null)
            {
                request.AddJsonBody(content);
            }

            var response = await _restClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent ||
                    response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return default;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await AuthenticateAsync();
                    response = await _restClient.ExecuteAsync(request);

                    if (!response.IsSuccessful)
                    {
                        throw new Exception($"API request failed: {response.StatusCode}, {response.ErrorMessage}");
                    }
                }
                else
                {
                    throw new Exception($"API request failed: {response.StatusCode}, {response.ErrorMessage}");
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return default;
            }

            if (!string.IsNullOrEmpty(response.Content))
            {
                if (typeof(T) == typeof(JObject))
                {
                    return (T)(object)JObject.Parse(response.Content);
                }

                return JsonConvert.DeserializeObject<T>(response.Content);
            }

            return default;
        }

        #endregion
    }

    #region Models

    /// <summary>
    /// Authentication response
    /// </summary>
    public class AuthResponse
    {
        public AuthResponse() { }

        [JsonProperty("accessToken")]
        public string AccessToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Shuffle state response
    /// </summary>
    public class ShuffleResponse
    {
        [JsonProperty("state")]
        public bool? State { get; set; }
    }

    /// <summary>
    /// Repeat mode response
    /// </summary>
    public class RepeatModeResponse
    {
        [JsonProperty("mode")]
        public string? Mode { get; set; }
    }

    /// <summary>
    /// Volume response
    /// </summary>
    public class VolumeResponse
    {
        [JsonProperty("state")]
        public int State { get; set; }
    }

    /// <summary>
    /// Full screen response
    /// </summary>
    public class FullscreenResponse
    {
        [JsonProperty("state")]
        public bool State { get; set; }
    }

    /// <summary>
    /// Song information
    /// </summary>
    public class SongInfo
    {
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("artist")]
        public string Artist { get; set; } = string.Empty;

        [JsonProperty("views")]
        public long Views { get; set; }

        [JsonProperty("uploadDate")]
        public string UploadDate { get; set; } = string.Empty;

        [JsonProperty("imageSrc")]
        public string? ImageSrc { get; set; }

        [JsonProperty("isPaused")]
        public bool IsPaused { get; set; }

        [JsonProperty("songDuration")]
        public double SongDuration { get; set; }

        [JsonProperty("elapsedSeconds")]
        public double ElapsedSeconds { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;

        [JsonProperty("album")]
        public string? Album { get; set; }

        [JsonProperty("videoId")]
        public string VideoId { get; set; } = string.Empty;

        [JsonProperty("playlistId")]
        public string PlaylistId { get; set; } = string.Empty;

        [JsonProperty("mediaType")]
        public MediaType MediaType { get; set; }
    }

    /// <summary>
    /// Search result
    /// </summary>
    public class SearchResult
    {
        public SearchResult()
        {
            ThumbnailUrl = string.Empty;
            Title = string.Empty;
            VideoId = string.Empty;
            AccessibilityData = string.Empty;
        }

        public string ThumbnailUrl { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string VideoId { get; set; } = string.Empty;

        public string AccessibilityData { get; set; } = string.Empty;

    }

    /// <summary>
    /// Media type
    /// </summary>
    public enum MediaType
    {
        AUDIO,
        ORIGINAL_MUSIC_VIDEO,
        USER_GENERATED_CONTENT,
        PODCAST_EPISODE,
        OTHER_VIDEO
    }

    /// <summary>
    /// Repeat mode
    /// </summary>
    public enum RepeatMode
    {
        NONE,
        ONE,
        ALL
    }

    /// <summary>
    /// Queue insert position
    /// </summary>
    public enum QueueInsertPosition
    {
        INSERT_AT_END,
        INSERT_AFTER_CURRENT_VIDEO
    }

    #endregion
}