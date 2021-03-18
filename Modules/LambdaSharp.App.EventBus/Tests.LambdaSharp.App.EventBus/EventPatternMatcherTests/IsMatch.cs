using LambdaSharp.App.EventBus;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Test.LambdaSharp.App.EventBus.EventPatternMatcherTests {

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
