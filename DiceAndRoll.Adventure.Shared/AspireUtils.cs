namespace DiceAndRoll.Adventure.Shared;

public static class AspireUtils
{
    public static bool IsAspireConfigured()
    {
        bool isAspireConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ASPIRE_CONTAINER_RUNTIME")) ||
                                  !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"));

        if (isAspireConfigured)
        {
            Console.WriteLine("This application is configured with .NET Aspire.");
        }
        else
        {
            Console.WriteLine("This application is not configured with .NET Aspire.");
        }

        return isAspireConfigured;
    }
}
