/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
 * lambdasharp.net
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using LambdaSharp.App.Config;
using LambdaSharp.App.EventBus;
using LambdaSharp.App.Logging;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LambdaSharp.App {

    /// <summary>
    /// Static class with extension methods for initializing the <see cref="WebAssemblyHostBuilder"/> instance.
    /// </summary>
    public static class WebAssemblyHostBuilderEx {

        //--- Extension Methods ---

        /// <summary>
        /// Configure the <see cref="WebAssemblyHostBuilder"/> instance for use with LambdaSharp.
        /// </summary>
        /// <param name="builder">The <see cref="WebAssemblyHostBuilder"/> instance.</param>
        /// <typeparam name="T">The application type used to determine the assembly version GUID.</typeparam>
        /// <returns>The <see cref="WebAssemblyHostBuilder"/> instance.</returns>
        public static WebAssemblyHostBuilder AddLambdaSharp<T>(this WebAssemblyHostBuilder builder)
            => UseLambdaSharp(builder, typeof(T));

        /// <summary>
        /// Configure the <see cref="WebAssemblyHostBuilder"/> instance for use with LambdaSharp.
        /// </summary>
        /// <param name="builder">The <see cref="WebAssemblyHostBuilder"/> instance.</param>
        /// <param name="mainType">The application type used to determine the assembly version GUID.</param>
        /// <returns>The <see cref="WebAssemblyHostBuilder"/> instance.</returns>
        public static WebAssemblyHostBuilder UseLambdaSharp(this WebAssemblyHostBuilder builder, Type mainType) {
            var emptyLambdaSharpSection = !builder.Configuration.GetSection("LambdaSharp").GetChildren().Any();

            // check if the LambdaSharp configuration section missing
            if(emptyLambdaSharpSection) {
                if(builder.HostEnvironment.IsDevelopment()) {
                    Console.WriteLine($"*** INFO: LambdaSharp App => using localhost test configuration");
                } else {
                    Console.WriteLine($"*** WARN: LambdaSharp App => config missing; using localhost test configuration instead");
                }
            }

            // enable debug logging when running locally or when LambadSharp DevMode is enabled
            if(
                builder.HostEnvironment.IsDevelopment()
                || (builder.Configuration.GetValue<string>("LambdaSharp:DevMode") == "Enabled")
            ) {
                builder.Logging.SetMinimumLevel(LogLevel.Debug);
                Console.WriteLine($"*** INFO: LambdaSharp App => DevMode is Enabled");
            }

            // register HttpClient, which is required to connect to the LambdaSharp app API
            builder.Services.TryAddSingleton(_ => new HttpClient {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
            });

            // register LambdaSharp configuration instance
            builder.Services.AddSingleton(_ => {
                if(emptyLambdaSharpSection) {

                    // initialize with localhost test data
                    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string> {
                        ["LambdaSharp:ModuleId"] = "My-Module",
                        ["LambdaSharp:ModuleInfo"] = "My.Module:1.0-DEV@localhost",
                        ["LambdaSharp:DeploymentTier"] = "",
                        ["LambdaSharp:AppId"] = "Local",
                        ["LambdaSharp:AppName"] = "Local",
                        ["LambdaSharp:AppFramework"] = "netstandard2.1",
                        ["LambdaSharp:ApiKey"] = "MDAwMDAwMDAtMDAwMC0wMDAwLTAwMDAtMDAwMDAwMDAwMDAw",
                        ["LambdaSharp:ApiUrl"] = "http://localhost:5000/.app",
                        ["LambdaSharp:DevMode"] = "Enabled",
                    });
                }

                // initialize LambdaSharp app config
                var config = builder.Configuration.GetSection("LambdaSharp").Get<LambdaSharpAppConfig>();

                // set application instance identifier
                var appInstanceId = Guid.NewGuid().ToString();
                config.AppInstanceId = appInstanceId;

                // set app version identifer using to app assembly
                config.AppVersionId = mainType.Assembly.ManifestModule.ModuleVersionId.ToString();

                // update configuration
                builder.Configuration["LambdaSharp:AppInstanceId"] = config.AppInstanceId;
                builder.Configuration["LambdaSharp:AppVersionId"] = config.AppVersionId;

                // emit minimal information for application diagnostics
                Console.WriteLine($"LambdaSharp App Instance Id: {appInstanceId}");
                return config;
            });

            // register LambdaSharp app API client
            builder.Services.AddSingleton<LambdaSharpAppClient>();

            // register LambdaSharp app EventBus client
            builder.Services.AddSingleton<LambdaSharpEventBusClient>();

            // register LambdaSharp logging provider
            builder.Services.AddSingleton<ILoggerProvider, LambdaSharpAppLoggerProvider>();
            return builder;
        }

    }
}
