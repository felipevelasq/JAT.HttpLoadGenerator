using System.Diagnostics;

namespace JAT.HttpLoadGenerator;
public class HttpLoadGenerator
{
    private const int defaultMaxConnections = 1;
    private int maxConnections = defaultMaxConnections;
    public int MaxConnections
    {
        get
        {
            return maxConnections;
        }
        set
        {
            if (maxConnections != value)
            {
                httpClient?.Dispose();
                httpClient = null;
            }
            maxConnections = value;
        }
    }

    private HttpClient? httpClient;

    public HttpLoadGenerator()
    {
    }

    public HttpLoadGenerator(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<LoadResult> ExecuteLoad(string url, int concurrentUsers, int nSeconds = 1, Action<RequestResult>? requestProcessedCallback = null)
    {
        if (httpClient == null)
        {
            var socketHandler = new SocketsHttpHandler
            {
                MaxConnectionsPerServer = maxConnections,
            };
            httpClient = new HttpClient(socketHandler);
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var requests = new List<Task<RequestResult>>();
        var results = new Dictionary<int, List<RequestResult>>();
        foreach (var user in Enumerable.Range(1, concurrentUsers))
        {
            requests.Add(AwaitAndProcessAsync(ExecuteRequestAsync(url, user), requestProcessedCallback));
            results[user] = new List<RequestResult>();
        }

        while (requests.Any())
        {
            var finishedTask = await Task.WhenAny(requests);
            var taskResult = await finishedTask;
            results[taskResult.UserId].Add(taskResult);
            requests.Remove(finishedTask);

            if (stopwatch.ElapsedMilliseconds < nSeconds * 1000)
            {
                requests.Add(AwaitAndProcessAsync(ExecuteRequestAsync(url, taskResult.UserId), requestProcessedCallback));
            }
        }

        stopwatch.Stop();
        return new LoadResult
        {
            Results = results,
            TotalTimeTaken = stopwatch.ElapsedMilliseconds,
        };
    }

    private async Task<RequestResult> AwaitAndProcessAsync(Task<RequestResult> task, Action<RequestResult>? requestProcessedCallback)
    {
        var result = await task;
        if (requestProcessedCallback != null)
            requestProcessedCallback.Invoke(result);
        return result;
    }

    private async Task<RequestResult> ExecuteRequestAsync(string url, int userId)
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
