using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FantasyHoops.Infrastructure.Yahoo;

public static class YahooServiceCollectionExtensions
{
    public static IServiceCollection AddYahooIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<YahooOptions>(configuration.GetSection(YahooOptions.SectionName));

        services.AddSingleton<FileYahooTokenStore>();
        services.AddSingleton<IYahooTokenStore>(sp => sp.GetRequiredService<FileYahooTokenStore>());

        services.AddHttpClient<YahooAuthService>();
        services.AddHttpClient<YahooApiClient>();

        return services;
    }
}
