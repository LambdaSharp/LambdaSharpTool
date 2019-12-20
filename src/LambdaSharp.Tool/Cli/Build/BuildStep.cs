/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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
using System.IO;
using System.Threading.Tasks;
using LambdaSharp.Tool.Compiler;
using LambdaSharp.Tool.Compiler.Analyzers;
using LambdaSharp.Tool.Compiler.Parser;
using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace LambdaSharp.Tool.Cli.Build {

    public class BuildStep : AModelProcessor {

        //--- Constructors ---
        public BuildStep(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Class Methods ---
        public async Task<bool> DoAsync(
            string outputCloudFormationFilePath,
            bool noAssemblyValidation,
            bool noPackageBuild,
            string gitSha,
            string gitBranch,
            string buildConfiguration,
            string selector,
            VersionInfo moduleVersion,
            bool forceBuild
        ) {
            if(!File.Exists(SourceFilename)) {
                LogError($"could not find '{SourceFilename}'");
                return false;
            }

            // delete output files
            try {
                File.Delete(Path.Combine(Settings.OutputDirectory, "manifest.json"));
            } catch { }
            try {
                File.Delete(outputCloudFormationFilePath);
            } catch { }

            // read input file
            Console.WriteLine();
            Console.WriteLine($"Parsing module: {Path.GetRelativePath(Directory.GetCurrentDirectory(), SourceFilename)}");

            // parse yaml to module declaration AST
            var moduleDeclaration = new LambdaSharpParser(this, SourceFilename).ParseSyntaxOfType<ModuleDeclaration>();
            if(HasErrors) {
                return false;
            }

            // prepare AST for processing
            var moduleBuilder = new Builder();
            moduleDeclaration.Visit(parent: null, new SyntaxHierarchyAnalyzer(moduleBuilder));
            if(HasErrors) {
                return false;
            }

            // optionally, override module version
            if(moduleVersion != null) {
                moduleBuilder.ModuleVersion = moduleVersion;
            }

            // prepare compilation
            Console.WriteLine($"Compiling: {moduleDeclaration.Module.Value} (v{moduleVersion?.ToString() ?? moduleDeclaration.Version.Value})");

            // analyze structure of AST
            moduleDeclaration.Visit(parent: null, new StructureAnalyzer(moduleBuilder));
            if(HasErrors) {
                return false;
            }

            // analyze references in AST
            moduleDeclaration.Visit(parent: null, new ReferencesAnalyzer(moduleBuilder));
            if(HasErrors) {
                return false;
            }

            // TODO:
            //  * collect definitions to download
            //  * type validation

            // resolve references in AST
            new ReferenceResolver(moduleBuilder).Visit();
            if(HasErrors) {
                return false;
            }

            // generate function environment based on scoped resources
            moduleDeclaration.Visit(parent: null, new FunctionEnvironmentAnalyzer(moduleBuilder));
            if(HasErrors) {
                return false;
            }

            // determine the necessary dependencies for the finalizer
            new FinalizerDependenciesAnalyzer(moduleBuilder).Visit();
            if(HasErrors) {
                return false;
            }

            // TODO:
            //  * generate build artifacts
            //  * convert declaration to cloudformation resources

            // TODO:
            throw new NotImplementedException();

            // // package all functions
            // new ModelFunctionPackager(Settings, SourceFilename).Package(
            //     module,
            //     noCompile: noPackageBuild,
            //     noAssemblyValidation: noAssemblyValidation,
            //     gitSha: gitSha,
            //     gitBranch: gitBranch,
            //     buildConfiguration: buildConfiguration,
            //     forceBuild: forceBuild
            // );

            // // package all files
            // new ModelFilesPackager(Settings, SourceFilename).Package(module, noPackageBuild);

            // // create folder for cloudformation output
            // var outputCloudFormationDirectory = Path.GetDirectoryName(outputCloudFormationFilePath);
            // if(outputCloudFormationDirectory != "") {
            //     Directory.CreateDirectory(outputCloudFormationDirectory);
            // }

            // // generate & save cloudformation template
            // var template = new ModelStackGenerator(Settings, SourceFilename).Generate(module.ToModule(), gitSha, gitBranch);
            // if(HasErrors) {
            //     return false;
            // }
            // File.WriteAllText(outputCloudFormationFilePath, template);
            // Console.WriteLine($"=> Module compilation done: {Path.GetRelativePath(Settings.WorkingDirectory, outputCloudFormationFilePath)}");
            // return true;
        }
    }
}