using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace TMWRR.Frontend.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services, IConfiguration config)
    {
        // Add services to the container.
        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        // Figures out HTTPS behind proxies
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            foreach (var knownProxy in config.GetSection("KnownProxies").Get<string[]>() ?? [])
            {
                if (IPAddress.TryParse(knownProxy, out var ipAddress))
                {
                    options.KnownProxies.Add(ipAddress);
                    continue;
                }

                foreach (var hostIpAddress in Dns.GetHostAddresses(knownProxy))
                {
                    options.KnownProxies.Add(hostIpAddress);
                }
            }
        });
    }
}
