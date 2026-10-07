// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Api.Jobs;

public static class Setup
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<JobDTOBuilder>();
        services.AddSingleton<JobNotifier>();
        services.AddScoped<IJobsInterfae, JobsInterfae>();
    }
}
