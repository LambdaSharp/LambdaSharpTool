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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace MindTouch.LambdaSharp.Tool.Internal {

    public class CloudFormationFunctionTypeConverter : TypeConverter {

        //--- Methods ---
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => throw new NotSupportedException();

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            => throw new NotSupportedException();

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string);
    }

    [TypeConverter(typeof(CloudFormationRefTypeConverter))]
    public class CloudFormationRef { }

    public class CloudFormationRefTypeConverter : CloudFormationFunctionTypeConverter {

        //--- Methods ---
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            => new Dictionary<string, object> {
                ["Ref"] = value
            };
    }

    [TypeConverter(typeof(CloudFormationGetAttTypeConverter))]
    public class CloudFormationGetAtt { }

    public class CloudFormationGetAttTypeConverter : CloudFormationFunctionTypeConverter {

        //--- Methods ---
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            var parts = ((string)value).Split('.', 2);
            return new Dictionary<string, object> {
                ["Fn::GetAtt"] = new List<object> {
                    parts[0],
                    (parts.Length == 2) ? parts[1] : ""
                }
            };
        }
    }
}