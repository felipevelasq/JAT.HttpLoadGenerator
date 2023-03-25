namespace JAT.HttpLoadGenerator;
public class LoadResult
{
    public long TotalTimeTaken { get; set; }
    public Dictionary<int, List<RequestResult>>? Results { get; set; }
}
