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
using LambdaSharp.App.Bus.Events;

namespace Test.LambdaSharp.App.Bus.EventPatternMatcherTests {

    public class IsMatch {

        //--- Methods ---

        [Fact]
        public void Empty_event_is_not_matched() {

            // arrange
            var evt = JObject.Parse(@"{}");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ ""Bar"" ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeFalse();
        }

        [Fact]
        public void Event_with_literal_is_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": ""Bar""
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ ""Bar"" ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void Event_with_list_is_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": [ ""Bar"" ]
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ ""Bar"" ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void Event_with_empty_is_not_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": [ ]
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ ""Bar"" ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeFalse();
        }

        [Fact]
        public void Event_with_prefix_is_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": ""Bar""
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""prefix"": ""B"" } ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void Event_with_prefix_is_not_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": ""Bar""
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""prefix"": ""F"" } ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeFalse();
        }

        [Fact]
        public void Event_with_anything_but_is_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": ""Bar""
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""anything-but"": { ""prefix"": ""F"" } } ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void Event_with_anything_but_is_not_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": ""Bar""
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""anything-but"": { ""prefix"": ""B"" } } ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeFalse();
        }

        [Fact]
        public void Event_with_numeric_one_operation_is_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": 42
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""numeric"": [ "">="", 40 ] } ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void Event_with_numeric_two_operation_is_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": 42
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""numeric"": [ "">="", 40, ""<"", 404 ] } ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void Event_with_numeric_one_operation_type_mismatch_is_not_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": ""Bar""
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""numeric"": [ "">="", 40 ] } ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeFalse();
        }

        [Fact]
        public void Event_with_cidr_is_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": ""192.168.1.42""
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""cidr"": ""192.168.1.1/24"" } ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void Event_with_cidr_out_of_range_is_not_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": ""192.168.16.42""
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""cidr"": ""192.168.1.1/24"" } ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeFalse();
        }

        [Fact]
        public void Event_with_cidr_mismatch_is_not_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": ""Bar""
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""cidr"": ""192.168.1.1/24"" } ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeFalse();
        }

        [Fact]
        public void Event_with_exists_is_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": ""Bar""
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""exists"": true } ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void Event_with_not_exists_is_matched() {

            // arrange
            var evt = JObject.Parse(@"{}");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""exists"": false } ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void Event_with_not_exists_on_non_leaf_node_is_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": {
                    ""Bar"": ""ABC""
                }
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": [ { ""exists"": false } ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void Event_with_nested_literal_is_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": {
                    ""Bar"": ""ABC""
                }
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": {
                    ""Bar"": [ ""ABC"" ]
                }
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void Event_with_nested_prefix_is_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": {
                    ""Bar"": ""ABC""
                }
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": {
                    ""Bar"": [ { ""prefix"": ""A"" } ]
                }
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void Event_with_multiple_is_matched() {

            // arrange
            var evt = JObject.Parse(@"{
                ""Foo"": {
                    ""Bar"": ""ABC""
                },
                ""Bar"": 42
            }");
            var pattern = JObject.Parse(@"{
                ""Foo"": {
                    ""Bar"": [ { ""prefix"": ""A"" } ]
                },
                ""Bar"": [ 40, 41, 42 ]
            }");

            // act
            var isMatch = EventPatternMatcher.IsMatch(evt, pattern);

            // assert
            isMatch.Should().BeTrue();
        }
    }
}
