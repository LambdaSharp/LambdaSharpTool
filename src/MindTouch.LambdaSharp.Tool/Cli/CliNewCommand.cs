/*
 * MindTouch λ#
 * Copyright (C) 2006-2018 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
using System.IO;
using McMaster.Extensions.CommandLineUtils;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliNewCommand : ACliCommand {

        //--- Methods --
        public void Register(CommandLineApplication app) {
            app.Command("new", cmd => {
                cmd.HelpOption();
                cmd.Description = "Create new LambdaSharp asset";

                // function sub-command
                cmd.Command("function", subCmd => {
                    subCmd.HelpOption();
                    subCmd.Description = "Create new LambdaSharp function";

                    // sub-command options
                    var nameOption = subCmd.Option("--name|-n <VALUE>", "Name of new project with module name prefix (e.g. Module.Function)", CommandOptionType.SingleValue);
                    var namespaceOption = subCmd.Option("--namespace|-ns <VALUE>", "(optional) Root namespace for project (default: same as function name)", CommandOptionType.SingleValue);
                    var directoryOption = subCmd.Option("--working-directory|-wd <VALUE>", "(optional) New function project parent directory (default: current directory)", CommandOptionType.SingleValue);
                    var frameworkOption = subCmd.Option("--framework|-f <VALUE>", "(optional) Target .NET framework (default: 'netcoreapp2.1')", CommandOptionType.SingleValue);
                    var useProjectReferenceOption = subCmd.Option("--use-project-reference", "Reference LambdaSharp libraries using project references (default: use nuget package reference)", CommandOptionType.NoValue);
                    subCmd.OnExecute(() => {
                        Console.WriteLine($"{app.FullName} - {cmd.Description}");
                        var lambdasharpDirectory = Environment.GetEnvironmentVariable("LAMBDASHARP");
                        if(lambdasharpDirectory == null) {
                            AddError("missing LAMBDASHARP environment variable");
                            return;
                        }
                        if(!nameOption.HasValue()) {
                            AddError("missing project '--name' option");
                            return;
                        }
                        NewFunction(
                            lambdasharpDirectory,
                            nameOption.Value(),
                            namespaceOption.Value() ?? nameOption.Value(),
                            frameworkOption.Value() ?? "netcoreapp2.1",
                            useProjectReferenceOption.HasValue(),
                            Path.GetFullPath(directoryOption.Value() ?? Directory.GetCurrentDirectory())
                        );
                    });
                });
                cmd.OnExecute(() => {
                    Console.WriteLine(cmd.GetHelpText());
                });
            });
        }

        private static void NewFunction(
            string lambdasharpDirectory,
            string functionName,
            string rootNamespace,
            string framework,
            bool useProjectReference,
            string baseDirectory
        ) {
            var projectDirectory = Path.Combine(baseDirectory, functionName);
            if(Directory.Exists(projectDirectory)) {
                AddError($"project directory '{projectDirectory}' already exists");
                return;
            }
            try {
                Directory.CreateDirectory(projectDirectory);
            } catch(Exception e) {
                AddError($"unable to create directory '{projectDirectory}'", e);
                return;
            }
            var lambdasharpProject = Path.GetRelativePath(projectDirectory, Path.Combine(lambdasharpDirectory, "src", "MindTouch.LambdaSharp", "MindTouch.LambdaSharp.csproj"));
            var projectFile = Path.Combine(projectDirectory, functionName + ".csproj");
            try {
                var projectContents = useProjectReference
? @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>" + framework + @"</TargetFramework>
    <Deterministic>true</Deterministic>
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
    <RootNamespace>" + rootNamespace + @"</RootNamespace>
    <AWSProjectType>Lambda</AWSProjectType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Amazon.Lambda.Core"" Version=""1.0.0""/>
    <PackageReference Include=""Amazon.Lambda.Serialization.Json"" Version=""1.2.0""/>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""" + lambdasharpProject + @""" />
  </ItemGroup>
  <ItemGroup>
    <DotNetCliToolReference Include=""Amazon.Lambda.Tools"" Version=""2.2.0""/>
  </ItemGroup>
</Project>"
:  @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>" + framework + @"</TargetFramework>
    <Deterministic>true</Deterministic>
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
    <RootNamespace>" + rootNamespace + @"</RootNamespace>
    <AWSProjectType>Lambda</AWSProjectType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Amazon.Lambda.Core"" Version=""1.0.0""/>
    <PackageReference Include=""Amazon.Lambda.Serialization.Json"" Version=""1.2.0""/>
    <PackageReference Include=""MindTouch.LambdaSharp"" Version=""0.1.3""/>
  </ItemGroup>
  <ItemGroup>
    <DotNetCliToolReference Include=""Amazon.Lambda.Tools"" Version=""2.2.0""/>
  </ItemGroup>
</Project>";
                File.WriteAllText(projectFile, projectContents);
                Console.WriteLine($"Created project file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), projectFile)}");
            } catch(Exception e) {
                AddError($"unable to create project file '{projectFile}'", e);
                return;
            }
            var functionFile = Path.Combine(projectDirectory, "Function.cs");
            try {
                var functionContents = 
@"using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using MindTouch.LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace " + rootNamespace + @" {

    public class Function : ALambdaFunction {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<object> ProcessMessageStreamAsync(Stream stream, ILambdaContext context) {
            using(var reader = new StreamReader(stream)) {
                LogInfo(await reader.ReadToEndAsync());
            }
            return ""Ok"";
        }
    }
}";
                File.WriteAllText(functionFile, functionContents);
                Console.WriteLine($"Created function file: {Path.GetRelativePath(Directory.GetCurrentDirectory(), functionFile)}");
            } catch(Exception e) {
                AddError($"unable to create function file '{functionFile}'", e);
                return;
            }
        }
    }
}
