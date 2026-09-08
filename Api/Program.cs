// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
global using static Api.Utils.GeneralUtils;

using System.Text.Json;
using System.Text.Json.Serialization;

using Api.Auth.Handlers;
using Api.Database;

using FluentValidation;

using HotChocolate.Types.NodaTime;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using NodaTime;

using Scalar.AspNetCore;

using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
var builder = WebApplication.CreateBuilder(args);



builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition =
        JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNamingPolicy =
        JsonNamingPolicy.CamelCase;
});


builder.Services.AddHttpLogging(opt =>
{
    if (builder.Environment.IsDevelopment())
    {
        opt.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    }
});

builder.Services.AddOpenApi();
builder.Services.AddSingleton<INodeIdSerializer, GuidNodeSerializer>();
builder.AddGraphQL()
    .AddApiTypes()
    .AddAuthorization()
    .AddNodaTime()
    .AddFiltering()
    .AddSorting()
    .AddPagingArguments()
    .ModifyPagingOptions(opt =>
    {
        opt.MaxPageSize = 50;
        opt.EnableRelativeCursors = true;
        opt.RequirePagingBoundaries = true;
    })
    .BindRuntimeType<Instant, HotChocolate.Types.NodaTime.DateTimeType>()
    .AddTypeConverter<Instant, OffsetDateTime>(t => t.InUtc().ToOffsetDateTime())
    .AddTypeConverter<OffsetDateTime, Instant>(t => t.ToInstant())
    .AddFairyBread(configureOptions: (opt) => opt.IncludeAttemptedValueInErrors = builder.Environment.IsDevelopment())
    .AddGlobalObjectIdentification(opt =>
    {
        opt.RegisterNodeInterface = true;
        opt.AddNodesField = true;
        opt.EnsureAllNodesCanBeResolved = true;
    })
    .ModifyServerOptions(opt =>
    {
        opt.Batching = HotChocolate.AspNetCore.AllowedBatching.All;

    });

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, AuthHandler>("x_user", null);
builder.Services.AddSingleton<IAuthorizationHandler, PermissionCheckAuthorizationHandler>();
builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());


Api.Database.Setup.RegisterServices(builder.Services);
Api.Auth.Setup.RegisterServices(builder.Services);

var app = builder.Build();

app.UseWebSockets();

app.UseHttpLogging();

// Configure the HTTP request pipeline.
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment() && app.Configuration["GRAPHQL_EXPORT"] != "1")
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    if (!app.Configuration.GetSection("IsTest").Get<bool>())
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetService<PGContext>()!;
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}

if (app.Configuration.GetSection("IsTest").Get<bool>())
{
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetService<PGContext>()!;
        RelationalDatabaseCreator databaseCreator =
            (RelationalDatabaseCreator)db.Database.GetService<IDatabaseCreator>();
        await databaseCreator.CreateTablesAsync();
    }
}

app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.MapGraphQL();

var api = app.MapGroup("/api").AddFluentValidationAutoValidation();

var auth = api.MapGroup("auth");
Api.Auth.Setup.MapEndpoints(auth);

var works = api.MapGroup("works");
Api.Works.Setup.MapEndpoints(works);

app.RunWithGraphQLCommands(args);