using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.NewtonsoftJson;

namespace ThchYoutubeMusicExtension
{
    /// <summary>
    /// YouTube Music API 클라이언트
    /// </summary>
    public class YoutubeMusicApiClient
    {
        private readonly string APP_NAME = "Powertoys-Extension";

        private readonly RestClient _restClient;
        private readonly string _baseUrl;
        private string? _accessToken;
        
        private static YoutubeMusicApiClient? _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 싱글톤 인스턴스 접근 속성
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
        /// YoutubeMusicApiClient 초기화
        /// </summary>
        /// <param name="baseUrl">API 서버 기본 URL</param>
        /// <param name="accessToken">인증 토큰 (선택 사항)</param>
        /// <returns>초기화된 인스턴스</returns>
        public static YoutubeMusicApiClient Initialize(string baseUrl, string? accessToken = null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new YoutubeMusicApiClient(baseUrl, accessToken);
                }
                return _instance;
            }
        }

        /// <summary>
        /// 테스트 목적으로 인스턴스 초기화 해제
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// YouTube Music API 클라이언트 생성자
        /// </summary>
        /// <param name="baseUrl">API 서버 기본 URL</param>
        /// <param name="accessToken">인증 토큰 (선택 사항)</param>
        private YoutubeMusicApiClient(string baseUrl, string? accessToken = null)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _restClient = new RestClient(
                _baseUrl,
                configureSerialization: s => s.UseNewtonsoftJson()
            );
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                SetAccessToken(accessToken);
            }
        }

        /// <summary>
        /// 액세스 토큰 설정
        /// </summary>
        /// <param name="accessToken">JWT 액세스 토큰</param>
        public void SetAccessToken(string accessToken)
        {
            _accessToken = accessToken;
            _restClient.AddDefaultHeader("Authorization", accessToken);
        }

        /// <summary>
        /// 인증 엔드포인트를 통해 인증하고 토큰 받기
        /// </summary>
        /// <param name="id">인증 ID</param>
        /// <returns>인증 성공 여부</returns>
        public async Task<bool> AuthenticateAsync()
        {
            var request = new RestRequest($"/auth/{APP_NAME}", Method.Post);
            var response = await _restClient.ExecuteAsync<AuthResponse>(request);
            
            if (response.IsSuccessful && response.Data != null)
            {
                SetAccessToken(response.Data.AccessToken);
                return true;
            }
            
            return false;
        }

        #region Player Controls

        /// <summary>
        /// 이전 곡 재생
        /// </summary>
        public async Task PreviousAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/previous");
        }

        /// <summary>
        /// 다음 곡 재생
        /// </summary>
        public async Task NextAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/next");
        }

        /// <summary>
        /// 재생
        /// </summary>
        public async Task PlayAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/play");
        }

        /// <summary>
        /// 일시정지
        /// </summary>
        public async Task PauseAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/pause");
        }

        /// <summary>
        /// 재생/일시정지 토글
        /// </summary>
        public async Task TogglePlayAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/toggle-play");
        }

        /// <summary>
        /// 좋아요 표시
        /// </summary>
        public async Task LikeAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/like");
        }

        /// <summary>
        /// 싫어요 표시
        /// </summary>
        public async Task DislikeAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/dislike");
        }

        /// <summary>
        /// 특정 시간으로 이동
        /// </summary>
        /// <param name="seconds">이동할 시간(초)</param>
        public async Task SeekToAsync(double seconds)
        {
            var request = new { Seconds = seconds };
            await SendRequestAsync(Method.Post, "/api/v1/seek-to", request);
        }

        /// <summary>
        /// 뒤로 이동
        /// </summary>
        /// <param name="seconds">뒤로 이동할 시간(초)</param>
        public async Task GoBackAsync(double seconds)
        {
            var request = new { Seconds = seconds };
            await SendRequestAsync(Method.Post, "/api/v1/go-back", request);
        }

        /// <summary>
        /// 앞으로 이동
        /// </summary>
        /// <param name="seconds">앞으로 이동할 시간(초)</param>
        public async Task GoForwardAsync(double seconds)
        {
            var request = new { Seconds = seconds };
            await SendRequestAsync(Method.Post, "/api/v1/go-forward", request);
        }

        #endregion

        #region Playback Settings

        /// <summary>
        /// 셔플 상태 가져오기
        /// </summary>
        /// <returns>셔플 상태</returns>
        public async Task<bool?> GetShuffleAsync()
        {
            var response = await SendRequestAsync<ShuffleResponse>(Method.Get, "/api/v1/shuffle");
            return response?.State;
        }

        /// <summary>
        /// 셔플 설정
        /// </summary>
        public async Task ShuffleAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/shuffle");
        }

        /// <summary>
        /// 반복 모드 가져오기
        /// </summary>
        /// <returns>반복 모드</returns>
        public async Task<RepeatMode?> GetRepeatModeAsync()
        {
            var response = await SendRequestAsync<RepeatModeResponse>(Method.Get, "/api/v1/repeat-mode");
            if (response?.Mode == null)
                return null;
            
            return Enum.Parse<RepeatMode>(response.Mode);
        }

        /// <summary>
        /// 반복 모드 전환
        /// </summary>
        /// <param name="iteration">반복 버튼 클릭 횟수</param>
        public async Task SwitchRepeatAsync(int iteration)
        {
            var request = new { Iteration = iteration };
            await SendRequestAsync(Method.Post, "/api/v1/switch-repeat", request);
        }

        /// <summary>
        /// 볼륨 설정
        /// </summary>
        /// <param name="volume">볼륨 값 (0-100)</param>
        public async Task SetVolumeAsync(int volume)
        {
            var request = new { Volume = volume };
            await SendRequestAsync(Method.Post, "/api/v1/volume", request);
        }

        /// <summary>
        /// 현재 볼륨 가져오기
        /// </summary>
        /// <returns>현재 볼륨 값</returns>
        public async Task<int> GetVolumeAsync()
        {
            var response = await SendRequestAsync<VolumeResponse>(Method.Get, "/api/v1/volume");
            return response?.State ?? 0;
        }

        /// <summary>
        /// 전체 화면 설정
        /// </summary>
        /// <param name="state">전체 화면 여부</param>
        public async Task SetFullscreenAsync(bool state)
        {
            var request = new { State = state };
            await SendRequestAsync(Method.Post, "/api/v1/fullscreen", request);
        }

        /// <summary>
        /// 전체 화면 상태 가져오기
        /// </summary>
        /// <returns>전체 화면 상태</returns>
        public async Task<bool> GetFullscreenAsync()
        {
            var response = await SendRequestAsync<FullscreenResponse>(Method.Get, "/api/v1/fullscreen");
            return response?.State ?? false;
        }

        /// <summary>
        /// 음소거 토글
        /// </summary>
        public async Task ToggleMuteAsync()
        {
            await SendRequestAsync(Method.Post, "/api/v1/toggle-mute");
        }

        #endregion

        #region Song and Queue Information

        /// <summary>
        /// 현재 노래 정보 가져오기
        /// </summary>
        /// <returns>노래 정보</returns>
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
        /// 대기열 정보 가져오기
        /// </summary>
        /// <returns>대기열 정보</returns>
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
        /// 대기열에 노래 추가
        /// </summary>
        /// <param name="videoId">비디오 ID</param>
        /// <param name="insertPosition">삽입 위치</param>
        public async Task AddToQueueAsync(string videoId, QueueInsertPosition insertPosition = QueueInsertPosition.INSERT_AT_END)
        {
            var request = new 
            { 
                VideoId = videoId,
                InsertPosition = insertPosition.ToString()
            };
            await SendRequestAsync(Method.Post, "/api/v1/queue", request);
        }

        /// <summary>
        /// 대기열 인덱스 설정
        /// </summary>
        /// <param name="index">설정할 인덱스</param>
        public async Task SetQueueIndexAsync(int index)
        {
            var request = new { Index = index };
            await SendRequestAsync(Method.Patch, "/api/v1/queue", request);
        }

        /// <summary>
        /// 대기열 비우기
        /// </summary>
        public async Task ClearQueueAsync()
        {
            await SendRequestAsync(Method.Delete, "/api/v1/queue");
        }

        /// <summary>
        /// 대기열에서 노래 위치 이동
        /// </summary>
        /// <param name="fromIndex">원래 위치</param>
        /// <param name="toIndex">이동할 위치</param>
        public async Task MoveSongInQueueAsync(int fromIndex, int toIndex)
        {
            var request = new { ToIndex = toIndex };
            await SendRequestAsync(Method.Patch, $"/api/v1/queue/{fromIndex}", request);
        }

        /// <summary>
        /// 대기열에서 노래 제거
        /// </summary>
        /// <param name="index">제거할 노래 인덱스</param>
        public async Task RemoveSongFromQueueAsync(int index)
        {
            await SendRequestAsync(Method.Delete, $"/api/v1/queue/{index}");
        }

        #endregion

        #region Search

        /// <summary>
        /// 노래 검색
        /// </summary>
        /// <param name="query">검색어</param>
        /// <returns>검색 결과</returns>
        public SearchResult? Search(string query)
        {
            var request = new { Query = query };
            var result = SendRequestAsync<JObject>(Method.Post, "/api/v1/search", request).GetAwaiter().GetResult();

            var songInfo = result["contents"]["tabbedSearchResultsRenderer"]["tabs"][0]["tabRenderer"]["content"]["sectionListRenderer"]["contents"][0]["musicCardShelfRenderer"];
            try
            {
               return new SearchResult
                {
                    ThumbnailUrl = songInfo["thumbnail"]["musicThumbnailRenderer"]["thumbnail"]["thumbnails"][0]["url"].Value<string>(),
                    Title = songInfo["title"]["runs"][0]["text"].Value<string>(),
                    VideoId = songInfo["title"]["runs"][0]["navigationEndpoint"]["watchEndpoint"]["videoId"].Value<string>(),
                    AccessibilityData = songInfo["subtitle"]["accessibility"]["accessibilityData"]["label"].Value<string>()
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
        /// 요청 전송 (응답 없음)
        /// </summary>
        private async Task SendRequestAsync(Method method, string path, object? content = null)
        {
            await SendRequestAsync<object?>(method, path, content);
        }

        /// <summary>
        /// 요청 전송 후 응답 받기
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
                        throw new Exception($"API 요청 실패: {response.StatusCode}, {response.ErrorMessage}");
                    }
                }
                else
                {
                    throw new Exception($"API 요청 실패: {response.StatusCode}, {response.ErrorMessage}");
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
    /// 인증 응답
    /// </summary>
    public class AuthResponse
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// 셔플 상태 응답
    /// </summary>
    public class ShuffleResponse
    {
        [JsonProperty("state")]
        public bool? State { get; set; }
    }

    /// <summary>
    /// 반복 모드 응답
    /// </summary>
    public class RepeatModeResponse
    {
        [JsonProperty("mode")]
        public string? Mode { get; set; }
    }

    /// <summary>
    /// 볼륨 응답
    /// </summary>
    public class VolumeResponse
    {
        [JsonProperty("state")]
        public int State { get; set; }
    }

    /// <summary>
    /// 전체 화면 응답
    /// </summary>
    public class FullscreenResponse
    {
        [JsonProperty("state")]
        public bool State { get; set; }
    }

    /// <summary>
    /// 노래 정보
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
    /// 검색 결과
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

        public string AccessibilityData {  get; set; } = string.Empty;

    }

    /// <summary>
    /// 미디어 타입
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
    /// 반복 모드
    /// </summary>
    public enum RepeatMode
    {
        NONE,
        ONE,
        ALL
    }

    /// <summary>
    /// 대기열 삽입 위치
    /// </summary>
    public enum QueueInsertPosition
    {
        INSERT_AT_END,
        INSERT_AFTER_CURRENT_VIDEO
    }

    #endregion
}