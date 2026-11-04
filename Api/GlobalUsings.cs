// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR Apache-2.0
//
// Implicit usings that the Web SDK + HotChocolate MSBuild props inject when
// building via dotnet build (see packages/*/build/*.targets). rules_dotnet's
// csharp_binary has no ImplicitUsings support, so they live here for the
// Bazel build; dotnet build handles duplicates without complaint.


// Mostly Ai Generated - Start
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using HotChocolate;
global using HotChocolate.Data;
global using HotChocolate.Types;
global using HotChocolate.Types.Relay;
global using HotChocolate.Types.Composite;
global using GreenDonut;
// Mostly Ai Generated - End
