using JAT.HttpLoadGenerator;

var generator = new HttpLoadGenerator();
var exit = string.Empty;
do
{
    Console.Clear();
    Console.Write("Concurrent Users: ");
    var concurrentUsers = int.Parse(Console.ReadLine() ?? "0");

    Console.Write("Max Connections: ");
    generator.MaxConnections = int.Parse(Console.ReadLine() ?? "0");

    Console.Write("Time: ");
    var time = int.Parse(Console.ReadLine() ?? "0");
    
    Console.WriteLine("Start");
    LoadResult? loadResult = await generator.ExecuteLoad("https://dummyjson.com/products", concurrentUsers, time, requestResult =>
    {
        // Console.WriteLine($"TaskEnded|UserId:{requestResult.UserId}|Status:{requestResult.Status}|Time:{requestResult.Time}");
        Console.Write(".");
    });
    Console.WriteLine("\nEnd");

    if (loadResult == null)
    {
        throw new Exception("Could not process load");
    }

    foreach (var userResults in loadResult.Results!.OrderBy(x => x.Key))
    {
        foreach (var requestResult in userResults.Value)
        {
            if (!string.IsNullOrEmpty(requestResult.Error))
            {
                Console.WriteLine(requestResult.Error);
            }
        }
        var userResultsGroupByStatus = userResults.Value.GroupBy(x => x.Status);
        userResultsGroupByStatus.Select(x => x.FirstOrDefault()?.Status?.ToString());
        var statusCountMessage = string.Join(",", userResultsGroupByStatus.Select(group => $"{group.FirstOrDefault()?.Status?.ToString()}: {group.Count()}"));
        var totalTimeMessage = string.Join(",", userResultsGroupByStatus.Select(group => $"Total Time: {group.Sum(x => x.Time)}"));
        Console.WriteLine("UserId: {0}|{1}|{2}", userResults.Key, statusCountMessage, totalTimeMessage);
    }
    var requestsCount = loadResult.Results?.Values.Sum(userResults => userResults.Count);
    Console.WriteLine($"Total Requests: {requestsCount}|Total Time: {loadResult.TotalTimeTaken}ms");

    Console.WriteLine("Press \"e\" and ENTER to exit");
    exit = Console.ReadLine();
    if (!string.IsNullOrEmpty(exit) && exit.StartsWith("e", StringComparison.InvariantCultureIgnoreCase))
        break;
} while (true);