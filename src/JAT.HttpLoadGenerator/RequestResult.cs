using System.Net;

namespace JAT.HttpLoadGenerator;
public class RequestResult
{
    public int UserId { get; set; }
    public HttpStatusCode? Status { get; set; }
    public long Time { get; set; }
    public string? Error { get; set; }
}