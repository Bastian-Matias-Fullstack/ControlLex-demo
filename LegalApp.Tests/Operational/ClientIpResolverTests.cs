using System.Net;
using API.Helpers;
using Microsoft.AspNetCore.Http;

namespace LegalApp.Tests.Operational;

public class ClientIpResolverTests
{
    private static readonly IPAddress SocketIp = IPAddress.Parse("10.0.0.20");

    [Fact]
    public void Resolve_RenderConHeaderValido_UsaIpCloudflare()
    {
        var context = CreateContext("203.0.113.25");

        var result = ClientIpResolver.Resolve(
            context,
            trustCloudflareConnectingIp: true);

        Assert.Equal(IPAddress.Parse("203.0.113.25"), result);
    }

    [Fact]
    public void Resolve_RenderSinHeader_UsaIpSocket()
    {
        var context = CreateContext();

        var result = ClientIpResolver.Resolve(
            context,
            trustCloudflareConnectingIp: true);

        Assert.Equal(SocketIp, result);
    }

    [Fact]
    public void Resolve_RenderConHeaderInvalido_UsaIpSocket()
    {
        var context = CreateContext("no-es-una-ip");

        var result = ClientIpResolver.Resolve(
            context,
            trustCloudflareConnectingIp: true);

        Assert.Equal(SocketIp, result);
    }

    [Fact]
    public void Resolve_RenderConValoresMultiples_UsaIpSocket()
    {
        var context = CreateContext();
        context.Request.Headers["CF-Connecting-IP"] =
            new Microsoft.Extensions.Primitives.StringValues(
                ["203.0.113.25", "203.0.113.26"]);

        var result = ClientIpResolver.Resolve(
            context,
            trustCloudflareConnectingIp: true);

        Assert.Equal(SocketIp, result);
    }

    [Fact]
    public void Resolve_FueraDeRender_IgnoraHeader()
    {
        var context = CreateContext("203.0.113.25");

        var result = ClientIpResolver.Resolve(
            context,
            trustCloudflareConnectingIp: false);

        Assert.Equal(SocketIp, result);
    }

    [Fact]
    public void Resolve_RenderConIpv6Valida_UsaIpCloudflare()
    {
        var context = CreateContext("2001:db8::25");

        var result = ClientIpResolver.Resolve(
            context,
            trustCloudflareConnectingIp: true);

        Assert.Equal(IPAddress.Parse("2001:db8::25"), result);
    }

    private static DefaultHttpContext CreateContext(string? cloudflareIp = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = SocketIp;

        if (cloudflareIp is not null)
        {
            context.Request.Headers["CF-Connecting-IP"] = cloudflareIp;
        }

        return context;
    }
}
