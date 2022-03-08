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

namespace LambdaSharp.Core.ProcessLogEventsFunction.Tests;

using System.Text.Json;
using FluentAssertions;
using LambdaSharp.Core.RollbarApi;
using Xunit;
using Xunit.Abstractions;

public class SerializationTests {

    //--- Constructors ---
    public SerializationTests(ITestOutputHelper output) => Output = output;

    //--- Properties ---
    private ITestOutputHelper Output { get; }

    //--- Methods ---

    [Fact]
    public void Deserialize_RollbarProject() {

        // arrange
        var project = new RollbarProject {
            Id = 42,
            Name = "MyRollbarProject",
            Status = "enabled",
            Created = DateTimeOffset.FromUnixTimeSeconds(1597324772),
            Modified = DateTimeOffset.FromUnixTimeSeconds(1622557791)
        };
        var json = JsonSerializer.Serialize(project);

        // act
        var data = JsonSerializer.Deserialize<RollbarProject>(json);

        // assert
        data.Should().BeEquivalentTo(project);
    }
}
