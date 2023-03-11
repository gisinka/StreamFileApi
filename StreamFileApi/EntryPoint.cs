using Vostok.Hosting;
using Vostok.Hosting.Setup;

namespace StreamFileApi;

public static class EntryPoint
{
    public static async Task Main()
    {
        var application = new StreamFileApiApplication();
        var hostSettings = new VostokHostSettings(application, EnvironmentSetup);

        var host = new VostokHost(hostSettings);

        await host.WithConsoleCancellation().RunAsync();
    }

    private static void EnvironmentSetup(IVostokHostingEnvironmentBuilder builder)
    {
        builder
            .SetupApplicationIdentity(identityBuilder => identityBuilder
                .SetProject("File")
                .SetApplication("Api")
                .SetEnvironment("default")
                .SetInstance("single"))
            .DisableClusterConfig()
            .SetupLog(logBuilder => logBuilder
                .SetupConsoleLog())
            .SetPort(80);
    }
}