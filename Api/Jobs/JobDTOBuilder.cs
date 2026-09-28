// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Reflection;
using Api.Jobs.DTOs;
using Api.Jobs.Models;

namespace Api.Jobs;

public class JobDTOBuilder
{
    public delegate IJob JobFactory(JobData data);
    private readonly Dictionary<string, JobFactory> _jobFactories;

    JobDTOBuilder()
    {
        _jobFactories = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                t.GetInterfaces().Any(i => i.GetType() == typeof(IJob)) && t.IsClass
            )
            .ToDictionary(
                t => t.Name,
                t =>
                    t.GetMethod(
                                nameof(IJobType.CreateFrom),
                                BindingFlags.Static | BindingFlags.Public
                            )!
                        .CreateDelegate<JobFactory>()
            );
    }

    public IJob? CreateFrom(string jobType, JobData data) =>
        _jobFactories.TryGetValue(jobType, out var factory) ? factory(data) : null;
}
