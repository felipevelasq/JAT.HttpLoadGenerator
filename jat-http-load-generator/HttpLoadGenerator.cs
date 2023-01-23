using System.Diagnostics;
using System.Net;

namespace JatHttpLoadGenerator;
public class HttpLoadGenerator
{
    static HttpClient? httpClient;
    public HttpLoadGenerator(HttpMessageHandler? handler = null)
    {
        httpClient = handler != null ? new HttpClient(handler) : new HttpClient();
    }

    public async Task<LoadResult> ExecuteLoad(string url, int concurrentUsers, int nSeconds = 1)
    {
        ServicePointManager.FindServicePoint(new Uri(url)).ConnectionLimit = concurrentUsers;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var requests = new List<Task<RequestResult>>();
        foreach (var user in Enumerable.Range(1, concurrentUsers))
        {
            requests.Add(ExecuteRequest(url, user));
        }

        var results = new Dictionary<int, List<RequestResult>>();
        while (requests.Any())
        {
            Task<RequestResult> finishedTask = await Task.WhenAny(requests);

            if (results.TryGetValue(finishedTask.Result.UserId, out List<RequestResult>? userResults))
            {
                userResults.Add(finishedTask.Result);
            }
            else
            {
                var newUserResults = new List<RequestResult>
                {
                    finishedTask.Result
                };
                results.Add(finishedTask.Result.UserId, newUserResults);
            }

            requests.Remove(finishedTask);

            if (stopwatch.ElapsedMilliseconds < nSeconds * 1000)
            {
                requests.Add(ExecuteRequest(url, finishedTask.Result.UserId));
            }
        }
        stopwatch.Stop();

        return new LoadResult
        {
            TotalTimeTaken = stopwatch.ElapsedMilliseconds,
            Results = results,
        };
    }

    private async Task<RequestResult> ExecuteRequest(string url, int userId)
    {
        if (httpClient == null)
        {
            throw new Exception("HttpClient was not be instantiated in the constructor");
        }

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await httpClient.SendAsync(request);
            
            response.EnsureSuccessStatusCode();
            stopwatch.Stop();
            return new RequestResult
            {
                UserId = userId,
                Status = response.StatusCode,
                Time = stopwatch.ElapsedMilliseconds,
            };
        }
        catch (HttpRequestException e)
        {
            stopwatch.Stop();
            return new RequestResult
            {
                UserId = userId,
                Status = e.StatusCode,
                Time = stopwatch.ElapsedMilliseconds,
                Error = e.Message,
            };
        }
        catch (Exception e)
        {
            stopwatch.Stop();
            return new RequestResult
            {
                UserId = userId,
                Status = default,
                Time = stopwatch.ElapsedMilliseconds,
                Error = e.Message,
            };
        }
    }
}
