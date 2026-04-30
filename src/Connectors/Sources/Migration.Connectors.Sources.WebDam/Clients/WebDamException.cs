using System;
using System.Net;

namespace Migration.Connectors.Sources.WebDam.Clients;

public sealed class WebDamException : Exception
{
    public WebDamException(string message)
        : base(message)
    {
    }

    public WebDamException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public HttpStatusCode? StatusCode { get; init; }
    public string? ResponseBody { get; init; }
}
