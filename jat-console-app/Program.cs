using JatHttpLoadGenerator;

var generator = new HttpLoadGenerator();

var exit = string.Empty;
do
{
    Console.Clear();
    Console.Write("Concurrent Users: ");
    var concurrentUsers = int.Parse(Console.ReadLine());

    Console.Write("Time: ");
    var time = int.Parse(Console.ReadLine());

    var loadResult = await generator.ExecuteLoad("https://dummyjson.com/products", concurrentUsers, time);

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
    Console.WriteLine("Total Requests: {0}|Total Time: {0}ms", loadResult.Results?.Values.Sum(userResults => userResults.Count), loadResult.TotalTimeTaken);
    
    exit = Console.ReadLine();
    if (!string.IsNullOrEmpty(exit) && exit.Contains("e")) break;
} while (true);