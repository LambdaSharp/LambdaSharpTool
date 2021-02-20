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
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.CloudFormation.Specification.TypeSystem {

    public abstract class _Init {

        //--- Class Methods ---
        protected static void ShouldNotBeNull([NotNull] object? value, string? because = null) {
            value.Should().NotBeNull(because);
            if(value == null) {
                throw new InvalidOperationException();
            }
        }

        //--- Fields ---
        protected readonly ITestOutputHelper Output;

        //--- Constructors ---
        public _Init(ITestOutputHelper output) => Output = output;
    }
}