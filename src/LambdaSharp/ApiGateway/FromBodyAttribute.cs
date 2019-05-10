/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
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

namespace LambdaSharp.ApiGateway {

    /// <summary>
    /// The <see cref="FromBodyAttribute"/> attribute is used to indicate that a parameter
    /// must be deserialized from the request body.
    /// </summary>
    /// <remarks>
    /// The <see cref="FromUriAttribute"/> and <see cref="FromBodyAttribute"/> are mutually exclusive.
    /// The <see cref="FromBodyAttribute"/> is not needed for complex types
    /// as they are deserialized from the request body by default.
    /// There can only be one parameter that is explicitly marked with the <see cref="FromBodyAttribute"/> attribute
    /// or which implicitly is deserialized from the request body.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple=false, Inherited=true)]
    public class FromBodyAttribute : Attribute { }
}
