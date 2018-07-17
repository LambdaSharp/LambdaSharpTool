/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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
using System.Reflection;
using MindTouch.Rollbar.Data;

namespace MindTouch.Rollbar.Builders {
    internal sealed class ServerBuilder {

        //--- Constants ---
        private const string GIT_SHA_ENVIRONMENT_VARIABLE = "GIT_SHA";
        private const string GIT_BRANCH_ENVIRONMENT_VARIABLE = "GIT_BRANCH";
        
        //--- Fields ---
        private static readonly Lazy<ServerBuilder> lazy =
            new Lazy<ServerBuilder>(() => new ServerBuilder());
        private readonly Server _server;

        //--- Contructors ---
        private ServerBuilder() {
            var assembly = Assembly.GetExecutingAssembly();
            var host = Environment.MachineName;
            var root = Path.GetDirectoryName(assembly.GetName().CodeBase);
            var branch = Environment.GetEnvironmentVariable(GIT_BRANCH_ENVIRONMENT_VARIABLE);
            var codeVersion = Environment.GetEnvironmentVariable(GIT_SHA_ENVIRONMENT_VARIABLE);
            _server = new Server(host, root, branch, codeVersion);
        }

        //--- Properties ---
        public static ServerBuilder Instance {
            get { return lazy.Value; }
        }

        public Server Server {
            get { return _server; }
        }
    }
}
