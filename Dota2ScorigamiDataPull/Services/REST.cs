using System;
using System.Threading.Tasks;

using Polly;
using Flurl;
using Flurl.Http;


namespace Dota2ScorigamiDataPull.Services
{
    public static class REST
    {
        public static async Task<T> ExecuteAsnycGet<T>(string url)
        {
            // API is throttled at 60 queries per minute, so we add a retry with wait
            Utility.LogInfo(url);
            return await Policy
                .Handle<FlurlHttpException>(IsWorthRetrying)
                .WaitAndRetryAsync(new[] {
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                },
                (result, timeSpan, retryCount, context) =>
                {
                    Utility.LogDebug($"Request failed with {result.Message}. Waiting {timeSpan} before next retry. Retry attempt {retryCount}");
                })
                .ExecuteAsync(() => url
                .WithTimeout(60)
                .SetQueryParam("significant", 0)
                .GetJsonAsync<T>());
        }

        public static bool IsWorthRetrying(FlurlHttpException ex)
        {
            if (ex.Call.Response == null)
            {
                // Likely server unavailable exception, worth retrying
                return true;
            }

            switch ((int)ex.Call.Response.StatusCode)
            {
                case 401:
                case 404:
                case 408:
                case 429:
                case 500:
                case 502:
                case 503:
                case 504:
                    return true;
                default:
                    return false;
            }
        }
    }
}
