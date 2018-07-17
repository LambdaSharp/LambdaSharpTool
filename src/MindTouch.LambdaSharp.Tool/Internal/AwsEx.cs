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

using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace MindTouch.LambdaSharp.Tool.Internal {
    
    internal static class AwsEx {

        //--- Extension Methods ---
        public async static Task<Dictionary<string, KeyValuePair<string, string>>> GetAllParametersByPathAsync(this IAmazonSimpleSystemsManagement client, string path) {
            var parametersRequest = new GetParametersByPathRequest {
                MaxResults = 10,
                Recursive = true,
                Path = path
            };
            var result = new Dictionary<string, KeyValuePair<string, string>>();
            do {
                var response = await client.GetParametersByPathAsync(parametersRequest);
                foreach(var parameter in response.Parameters) {
                    result[parameter.Name] = new KeyValuePair<string, string>(parameter.Type, parameter.Value);
                }
                parametersRequest.NextToken = response.NextToken;
            } while(parametersRequest.NextToken != null);
            return result;
        }
    }
}