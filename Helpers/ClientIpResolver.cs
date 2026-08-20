using System.Net;

namespace API.Helpers;

public static class ClientIpResolver
{
    private const string CloudflareConnectingIpHeader = "CF-Connecting-IP";

    public static IPAddress? Resolve(
        HttpContext context,
        bool trustCloudflareConnectingIp)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (trustCloudflareConnectingIp &&
            context.Request.Headers.TryGetValue(
                CloudflareConnectingIpHeader,
                out var headerValues) &&
            headerValues.Count == 1 &&
            IPAddress.TryParse(headerValues[0]?.Trim(), out var clientIp))
        {
            return clientIp;
        }

        return context.Connection.RemoteIpAddress;
    }
}
