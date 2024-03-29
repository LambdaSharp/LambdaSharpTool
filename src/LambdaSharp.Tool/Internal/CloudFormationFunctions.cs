/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.ObjectFactories;

namespace LambdaSharp.Tool.Internal {

    [TypeConverter(typeof(CloudFormationFunctionTypeConverter))]
    public class CloudFormationListFunction : List<object> { }

    public class CloudFormationFunctionTypeConverter : TypeConverter {

        //--- Methods ---
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => throw new NotSupportedException();

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            => throw new NotSupportedException();

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            => value;
    }

    public class CloudFormationMapFunction : Dictionary<string, object> { }

    public static class CloudFromationFunctionsEx {

        //--- Class Methods ---
        public static DeserializerBuilder WithCloudFormationFunctions(this DeserializerBuilder builder) {
            foreach(var tag in CloudFormationFunctionNodeDeserializer.SupportedTags) {
                builder = builder.WithTagMapping(tag, typeof(CloudFormationListFunction));
            }
            return builder
                .WithTagMapping("!Transform", typeof(CloudFormationMapFunction));
        }
    }

    public class CloudFormationFunctionNodeDeserializer : INodeDeserializer {

        //--- Class Fields ---
        public static HashSet<string> SupportedTags = new HashSet<string> {
            "!And",
            "!Base64",
            "!Cidr",
            "!Equals",
            "!FindInMap",
            "!GetAtt",
            "!GetAZs",
            "!If",
            "!ImportValue",
            "!Join",
            "!Not",
            "!Or",
            "!Ref",
            "!Select",
            "!Split",
            "!Sub",
            "!Condition"
        };

        //--- Methods ---
        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value) {
            if((reader.Current is SequenceStart sequenceStart) && sequenceStart.Tag.IsLocal && SupportedTags.Contains(sequenceStart.Tag.Value)) {

                // deserialize parameter list
                INodeDeserializer nested = new CollectionNodeDeserializer(new DefaultObjectFactory());
                if(nested.Deserialize(reader, expectedType, nestedObjectDeserializer, out value)) {
                    var key = TagToFunctionName(sequenceStart.Tag.Value);
                    value = new Dictionary<string, object> {
                        [key] = value
                    };
                    return true;
                }
            } else if((reader.Current is MappingStart mapStart) && mapStart.Tag.IsLocal && (mapStart.Tag.Value == "!Transform")) {

                // deserialize parameter map
                INodeDeserializer nested = new DictionaryNodeDeserializer(new DefaultObjectFactory());
                if(nested.Deserialize(reader, expectedType, nestedObjectDeserializer, out value)) {
                    var key = TagToFunctionName(mapStart.Tag.Value);
                    value = new Dictionary<string, object> {
                        [key] = value
                    };
                    return true;
                }
            } else if((reader.Current is Scalar scalar) && scalar.Tag.IsLocal && SupportedTags.Contains(scalar.Tag.Value)) {

                // deserialize single parameter
                INodeDeserializer nested = new ScalarNodeDeserializer();
                if(nested.Deserialize(reader, expectedType, nestedObjectDeserializer, out value)) {
                    var key = TagToFunctionName(scalar.Tag.Value);

                    // special case for !GetAtt as the single parameter must be converted into a parameter list
                    if(key == "Fn::GetAtt") {
                        var parts = ((string)value).Split('.', 2);
                        value = new Dictionary<string, object> {
                            ["Fn::GetAtt"] = new List<object> {
                                parts[0],
                                (parts.Length == 2) ? parts[1] : ""
                            }
                        };
                    } else {
                        value = new Dictionary<string, object> {
                            [key] = value
                        };
                    }
                    return true;
                }
            }
            value = null;
            return false;

            // local functions
            string TagToFunctionName(string tag) {
                var suffix = tag.Substring(1);

                // special case for !Ref as it doesn't get the Fn:: prefix
                if(suffix == "Ref") {
                    return suffix;
                }
                if(suffix == "Condition") {
                    return suffix;
                }
                return "Fn::" + suffix;
           }
        }
    }
}