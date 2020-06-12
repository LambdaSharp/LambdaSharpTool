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
using System.Linq;
using FluentAssertions;
using LambdaSharp.Compiler.TypeSystem;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Compiler.TypeSystem.CloudFormation {

    public class CloudFormationTypeSystemTests : _Init, IClassFixture<CloudFormationTypeSystemFixture> {

        //--- Fields ---
        private readonly CloudFormationTypeSystemFixture _typeSystemFixture;

        //--- Constructors ---
        public CloudFormationTypeSystemTests(ITestOutputHelper output, CloudFormationTypeSystemFixture typeSystemFixture)
            : base(output)
        {
            _typeSystemFixture = typeSystemFixture ?? throw new ArgumentNullException(nameof(typeSystemFixture));
        }

        //--- Properties ---
        protected ITypeSystem TypeSystem => _typeSystemFixture.TypeSystem;

        //--- Methods ---

        [Fact]
        public void CountResourceTypes() {

            // act
            var count = TypeSystem.ResourceTypes.Count();

            // assert
            count.Should().Be(540);
        }

        [Fact]
        public void ValidateSnsTopicResourceType() {

            // act
            var success = TypeSystem.TryGetResourceType("AWS::SNS::Topic", out var resourceType);

            // assert
            success.Should().Be(true);
            ShouldNotBeNull(resourceType);
        }

        [Fact]
        public void ValidateDislayNamePropertyForSnsTopicResourceType() {

            // arrange
            TypeSystem.TryGetResourceType("AWS::SNS::Topic", out var resourceType).Should().Be(true);
            ShouldNotBeNull(resourceType);

            // act
            var success = resourceType.TryGetProperty("DisplayName", out var property);

            // assert
            success.Should().Be(true);
            ShouldNotBeNull(property);
            property.Name.Should().Be("DisplayName");
            property.Required.Should().Be(false);
            property.CollectionType.Should().Be(PropertyCollectionType.NoCollection);
            property.ItemType.Should().Be(PropertyItemType.String);
        }

        [Fact]
        public void ValidateSubscriptionPropertyForSnsTopicResourceType() {

            // arrange
            TypeSystem.TryGetResourceType("AWS::SNS::Topic", out var resourceType).Should().Be(true);
            ShouldNotBeNull(resourceType);

            // act
            var success = resourceType.TryGetProperty("Subscription", out var property);

            // assert
            success.Should().Be(true);
            ShouldNotBeNull(property);
            property.Name.Should().Be("Subscription");
            property.Required.Should().Be(false);
            property.CollectionType.Should().Be(PropertyCollectionType.List);
            property.ItemType.Should().Be(PropertyItemType.ComplexType);
            var subscriptionType = property.ComplexType;
            ShouldNotBeNull(subscriptionType);
            subscriptionType.RequiredProperties.Count().Should().Be(2);
        }

        [Fact]
        public void ValidateCustomResourceType() {

            // arrange
            TypeSystem.TryGetResourceType("Custom::Acme", out var resourceType).Should().Be(true);
            ShouldNotBeNull(resourceType);

            // act
            var success = resourceType.TryGetProperty("Whatever", out var property);

            // assert
            success.Should().Be(true);
            ShouldNotBeNull(property);
            property.Required.Should().Be(false);
            property.CollectionType.Should().Be(PropertyCollectionType.NoCollection);
            property.ItemType.Should().Be(PropertyItemType.Any);
        }
    }
}