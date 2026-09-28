namespace Api.Jobs;

public static class Setup
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<JobDTOBuilder>();
    }
}
