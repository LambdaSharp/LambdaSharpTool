using System;
using System.IO;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool.Cli.Build {
    public class BuildScala {
        public static void DetermineFunctionProperties(
            string functionName,
            string project,
            ref string language,
            ref string runtime,
            ref string handler
        ) {
        
            language = "scala";
            runtime = runtime ?? "java8";
            handler = handler ?? throw new ArgumentException("The handler name is required for Scala/Java functions");
        }
        
        public static string Process(
            FunctionItem function,
            bool skipCompile,
            bool noAssemblyValidation,
            string gitSha,
            string gitBranch,
            string buildConfiguration,
            bool showOutput
        ) {
            function.Language = "scala";
            var projectDirectory = Path.GetDirectoryName(function.Project);
            
            // check if we need a default handler
            if (function.Function.Handler == null) {
                throw new Exception("The function handler cannot be empty for SBT/Scala functions.");
            }

            // compile function and create assembly
            if (!skipCompile) {
                ProcessLauncher.Execute(
                    "sbt",
                    new[] { "assembly" },
                   projectDirectory, 
                    showOutput
                );
            }
            
            // check if we need to set a default runtime
            if(function.Function.Runtime == null) {
                function.Function.Runtime = "java8";
            }

            // check if the project zip file was created
            var scalaOutputJar = Path.Combine(projectDirectory, "target", "scala-2.12", "app.jar");
            return scalaOutputJar;
        }
    }
}