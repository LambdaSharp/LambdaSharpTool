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
using MindTouch.LambdaSharp.Tool.Internal;
using MindTouch.LambdaSharp.Tool.Model.AST;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelParser : AModelProcessor {

        //--- Constructors ---
        public ModelParser(Settings settings) : base(settings) { }

        //--- Methods ---
        public ModuleNode Process(YamlDotNet.Core.IParser yamlParser) {

            // parse YAML file into module AST
            try {
                var builder = new DeserializerBuilder()
                    .WithNamingConvention(new PascalCaseNamingConvention())
                    .WithNodeDeserializer(new CloudFormationFunctionNodeDeserializer());
                foreach(var tag in CloudFormationFunctionNodeDeserializer.SupportedTags) {
                    builder = builder.WithTagMapping(tag, typeof(CloudFormationFunction));
                }
                return builder
                    .Build()
                    .Deserialize<ModuleNode>(yamlParser);
            } catch(Exception e) {
                AddError($"parse error: {e.Message}", e);
                return null;
            }
        }
    }
}