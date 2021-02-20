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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LambdaSharp.CloudFormation.Template.Serialization {

    public class CloudFormationExpressionConverter : JsonConverter<ACloudFormationExpression> {

        //--- Class Fields ---
        private readonly CloudFormationListConverter _listSerializer = new CloudFormationListConverter();
        private readonly CloudFormationLiteralConverter _literalSerializer = new CloudFormationLiteralConverter();
        private readonly CloudFormationObjectConverter _objectSerializer = new CloudFormationObjectConverter();

        //--- Methods ---
        public override ACloudFormationExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, ACloudFormationExpression value, JsonSerializerOptions options) {
            switch(value) {
            case CloudFormationList list:
                _listSerializer.Write(writer, list, options);
                break;
            case CloudFormationLiteral literal:
                _literalSerializer.Write(writer, literal, options);
                break;
            case CloudFormationObject obj:
                _objectSerializer.Write(writer, obj, options);
                break;
            default:
                throw new ArgumentException($"unsupported serialization type {value?.GetType().FullName ?? "<null>"}");
            }
        }
    }
}