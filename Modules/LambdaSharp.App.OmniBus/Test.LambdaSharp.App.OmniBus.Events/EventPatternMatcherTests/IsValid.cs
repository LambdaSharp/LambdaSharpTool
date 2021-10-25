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

using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;
using LambdaSharp.App.OmniBus.Events;

namespace Test.LambdaSharp.App.OmniBus.EventPatternMatcherTests {

    public class IsValid {

        //--- Methods ---

        [Fact]
        public void Empty_pattern_is_not_valid() {

            // arrange
            var pattern = JObject.Parse(@"{}");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Pattern_with_literal_is_not_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": ""Bar""
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Pattern_with_empty_list_is_not_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Pattern_with_text_is_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ ""Bar"" ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Pattern_with_numeric_is_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ 42 ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Pattern_with_null_is_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ null ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Pattern_with_nested_list_is_not_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ [ 42 ]  ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Pattern_with_prefix_text_is_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""prefix"": ""Bar"" } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Pattern_with_prefix_numeric_is_not_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""prefix"": 42 } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Pattern_with_prefix_null_is_not_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""prefix"": null } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Pattern_with_prefix_list_is_not_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""prefix"": [ ""Bar"" ] } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Pattern_with_anything_but_text_is_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""anything-but"": ""Bar"" } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Pattern_with_anything_but_numeric_is_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""anything-but"": 42 } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Pattern_with_anything_but_list_is_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""anything-but"": [ ""Bar"" ] } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Pattern_with_anything_but_content_filter_is_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""anything-but"": { ""prefix"": ""Bar"" } } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Pattern_with_numeric_one_operation_is_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""numeric"": [ ""="", 42 ] } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Pattern_with_numeric_two_operations_is_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""numeric"": [ "">"", 42, ""<"", 404 ] } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Pattern_with_cidr_is_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""cidr"": ""1.1.1.1/24"" } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Pattern_with_cidr_bad_ip_is_not_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""cidr"": ""1.404.1.1/24"" } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Pattern_with_cidr_bad_prefix_is_not_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""cidr"": ""1.1.1.1/42"" } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Pattern_with_exists_is_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""exists"": true } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Pattern_with_exists_text_is_not_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""exists"": ""Bar"" } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Pattern_with_exists_numeric_is_not_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""exists"": 42 } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Pattern_with_exists_null_is_not_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""exists"": null } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Pattern_with_invalid_filter_is_not_valid() {

            // arrange
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""Bar"": 42 } ]
            }");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Pattern_with_null_serialized_is_valid() {

            // arrange
            var pattern = JObject.Parse("{\"source\":[\"Sample.BlazorEventsSample:1.0-DEV@stevebv7-lambdasharp-cor-deploymentbucketresource-9h53iqcat7uj::MyBlazorApp\"],\"detail-type\":[\"Sample.BlazorEventsSample.MyBlazorApp.Shared.TodoItem\"],\"resources\":[\"lambdasharp:tier:SteveBv7\"],\"detail\":null}");

            // act
            var isValid = EventPatternMatcher.IsValid(pattern);

            // assert
            isValid.Should().BeTrue();
        }
    }
}
