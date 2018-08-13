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
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

namespace MindTouch.LambdaSharp {

    public abstract class ALambdaApiGatewayFunction : ALambdaFunction<APIGatewayProxyRequest, APIGatewayProxyResponse> {

        //--- Constructors ---
        protected ALambdaApiGatewayFunction() : this(LambdaFunctionConfiguration.Instance) { }

        protected ALambdaApiGatewayFunction(LambdaFunctionConfiguration configuration) : base(configuration) { }

        //--- Abstract Methods ---
        public abstract Task<APIGatewayProxyResponse> HandleRequestAsync(APIGatewayProxyRequest request, ILambdaContext context);

        //--- Methods ---
        public override async Task<APIGatewayProxyResponse> ProcessMessageAsync(APIGatewayProxyRequest request, ILambdaContext context) 
            => await HandleRequestAsync(request, context);
    }
}
