/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.EventSources;
using LambdaSharp.Compiler.Syntax.Expressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace LambdaSharp.Compiler.Parser {

    public sealed class LambdaSharpParser {

        // TODO: `cloudformation package`, when given a YAML template, converts even explicit strings to integers when the string begins with a 0 and contains nothing but digits · Issue #2934 · aws/aws-cli · GitHub (https://github.com/aws/aws-cli/issues/2934)
        // TODO: 'null' literal can also be used where keys are allowed! (e.g. ~: foo)

        //--- Types ---
        private class SyntaxInfo {

            //--- Constructors ---
            public SyntaxInfo(Type declarationType) {
                if(declarationType == null) {
                    throw new ArgumentNullException(nameof(declarationType));
                }

                // NOTE: the keyword must be the first key in a mapping and cannot be used to identify any other parsable constructs
                var declarationKeywordAttribute = declarationType.GetCustomAttribute<SyntaxDeclarationKeywordAttribute>();
                if(declarationKeywordAttribute != null) {
                    Keyword = declarationKeywordAttribute.Keyword;
                    KeywordType = declarationKeywordAttribute.Type ?? typeof(LiteralExpression);
                }

                // NOTE: extract all properties with a syntax attribute from type to identify what keys are expected and required;
                //  in addition, one key can be called out as the keyword, which means it must be the first key to appear in the mapping.
                var syntaxProperties = declarationType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    .Select(property => new {
                        Syntax = property.GetCustomAttribute<ASyntaxAttribute>(),
                        Property = property
                    })
                    .Where(tuple => (tuple.Syntax is SyntaxRequiredAttribute) || (tuple.Syntax is SyntaxOptionalAttribute))
                    .ToList();
                DeclarationType = declarationType;
                Keys = syntaxProperties.ToDictionary(tuple => tuple.Property.Name, Tuple => Tuple.Property);
                MandatoryKeys = syntaxProperties
                    .Where(tuple => tuple.Syntax is SyntaxRequiredAttribute)
                    .Select(tuple => tuple.Property.Name)
                    .ToArray();
            }

            //--- Properties ---
            public Type DeclarationType { get; }
            public string? Keyword { get; }
            public Type? KeywordType { get; }
            public Dictionary<string, PropertyInfo> Keys { get; }
            public IEnumerable<string> MandatoryKeys { get; }
        }

        private readonly struct ParsingEventWithFilePath<T> where T : ParsingEvent {

            //--- Fields ---
            public readonly string FilePath;
            public readonly T ParsingEvent;

            //--- Constructors ---
            public ParsingEventWithFilePath(string filePath, T parsingEvent) {
                FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
                ParsingEvent = parsingEvent ?? throw new ArgumentNullException(nameof(parsingEvent));
            }
        }

        //--- Class Methods ---
        private static bool TryParseYamlInteger(string value, [NotNullWhen(true)] out string? number) {

            // NOTE (2019-12-10, bjorg): integer literal: https://yaml.org/type/int.html
            //  [-+]?0b[0-1_]+ # (base 2)
            //  |[-+]?0[0-7_]+ # (base 8)
            //  |[-+]?(0|[1-9][0-9_]*) # (base 10)
            //  |[-+]?0x[0-9a-fA-F_]+ # (base 16)
            //  |[-+]?[1-9][0-9_]*(:[0-5]?[0-9])+ # (base 60)
            var index = 0;
            var negative = false;
            var radix = 10;
            number = null;

            // detect leading sign
            if(value[0] == '-') {
                ++index;
                negative = true;
            } else if(value[0] == '+') {
                ++index;
            }
            if(index == value.Length) {
                return false;
            }

            // detect base selector
            if(string.Compare(value, index, "0b", 0, 2, StringComparison.Ordinal) == 0) {
                radix = 2;
                index += 2;
            } else if(string.Compare(value, index, "0x", 0, 2, StringComparison.Ordinal) == 0) {
                radix = 16;
                index += 2;
            } else if(value[index] == '0') {
                radix = 8;
                ++index;

                // special case where we only have a single 0 as number
                if(index == value.Length) {
                    number = "0";
                    return true;
                }
            }
            if(index == value.Length) {
                return false;
            }

            // compute result
            ulong result;
            if(radix == 10) {
                result = 0UL;

                // could be decimal or base 60 (sexagesimal)
                var first = true;
                foreach(var chunk in value.Substring(index).Split(':')) {
                    result *= 60UL;
                    if(!ulong.TryParse(chunk.Replace("_", ""), out var part)) {
                        return false;
                    }

                    // trailing chunks must be less than 60
                    if(!first && (part >= 60)) {
                        return false;
                    }
                    result += part;
                    first = false;
                }
            } else {
                try {
                    result = Convert.ToUInt64(value.Substring(index).Replace("_", "").ToString(), radix);
                } catch {
                    return false;
                }
            }

            // render number as signed integer
            if(negative) {
                number = "-" + result.ToString();
            } else {
                number = result.ToString();
            }
            return true;
        }

        private static bool TryParseYamlFloat(string value, out double number) {

            // NOTE (2019-12-12, bjorg): float literal: https://yaml.org/type/float.html
            //  [-+]?([0-9][0-9_]*)?\.[0-9.]*([eE][-+][0-9]+)? (base 10)
            //  |[-+]?[0-9][0-9_]*(:[0-5]?[0-9])+\.[0-9_]* (base 60)
            //  |[-+]?\.(inf|Inf|INF) # (infinity)
            //  |\.(nan|NaN|NAN) # (not a number)

            switch(value) {
            case "-.inf":
            case "-.Inf":
            case "-.INF":
                number = double.NegativeInfinity;
                break;
            case "+.inf":
            case "+.Inf":
            case "+.INF":
            case ".inf":
            case ".Inf":
            case ".INF":
                number = double.PositiveInfinity;
                break;
            case ".nan":
            case ".NaN":
            case ".NAN":
                number = double.NaN;
                break;
            default:
                number = 0;

                // could be decimal or base 60 (sexagesimal)
                var values = value.Split(':');
                for(var i = 0; i < values.Length; ++i) {
                    number *= 60.0;

                    // all chunks must be integers, except the last chunk can be a floating-point value
                    double chunkValue;
                    if(i != (values.Length - 1)) {
                        if(!long.TryParse(values[i].Replace("_", ""), out var part)) {
                            return false;
                        }
                        chunkValue = part;
                    } else {
                        if(!double.TryParse(values[i].Replace("_", ""), out var part)) {
                            return false;
                        }
                        chunkValue = part;
                    }

                    // trailing chunks must be less than 60
                    if((i > 0) && (chunkValue >= 60)) {
                        return false;
                    }
                    number += chunkValue;
                }
                break;
            }
            return true;
        }

        //--- Fields ---
        private readonly ILambdaSharpParserDependencyProvider _provider;
        private readonly Dictionary<Type, Func<object?>> _typeParsers;
        private readonly Dictionary<Type, SyntaxInfo> _typeToSyntax = new Dictionary<Type, SyntaxInfo>();
        private readonly Dictionary<Type, Dictionary<string, SyntaxInfo>> _syntaxCache = new Dictionary<Type, Dictionary<string, SyntaxInfo>>();
        private readonly Stack<(string FilePath, IEnumerator<ParsingEvent> ParsingEnumerator)> _parsingEvents = new Stack<(string, IEnumerator<ParsingEvent>)>();

        //--- Constructors ---
        public LambdaSharpParser(ILambdaSharpParserDependencyProvider provider) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _typeParsers = new Dictionary<Type, Func<object?>> {

                // expressions
                [typeof(AExpression)] = () => ParseExpression(),
                [typeof(ObjectExpression)] = () => ParseExpressionOfType<ObjectExpression>(Error.ExpectedMapExpression),
                [typeof(ListExpression)] = () => ParseExpressionOfType<ListExpression>(Error.ExpectedListExpression),
                [typeof(LiteralExpression)] = () => ParseExpressionOfType<LiteralExpression>(Error.ExpectedLiteralValue),

                // declarations
                [typeof(ModuleDeclaration)] = () => ParseSyntaxOfType<ModuleDeclaration>(),
                [typeof(UsingModuleDeclaration)] = () => ParseSyntaxOfType<UsingModuleDeclaration>(),
                [typeof(FunctionDeclaration.VpcExpression)] = () => ParseSyntaxOfType<FunctionDeclaration.VpcExpression>(),
                [typeof(ResourceTypeDeclaration.PropertyTypeExpression)] = () => ParseSyntaxOfType<ResourceTypeDeclaration.PropertyTypeExpression>(),
                [typeof(ResourceTypeDeclaration.AttributeTypeExpression)] = () => ParseSyntaxOfType<ResourceTypeDeclaration.AttributeTypeExpression>(),
                [typeof(AItemDeclaration)] = () => ParseSyntaxOfType<AItemDeclaration>(),
                [typeof(AEventSourceDeclaration)] = () => ParseSyntaxOfType<AEventSourceDeclaration>(),
                [typeof(ModuleDeclaration.CloudFormationSpecExpression)] = () => ParseSyntaxOfType<ModuleDeclaration.CloudFormationSpecExpression>(),

                // lists
                [typeof(SyntaxNodeCollection<AItemDeclaration>)] = () => ParseList<AItemDeclaration>(),
                [typeof(SyntaxNodeCollection<AExpression>)] = () => ParseList<AExpression>(),
                [typeof(SyntaxNodeCollection<AEventSourceDeclaration>)] = () => ParseList<AEventSourceDeclaration>(),
                [typeof(SyntaxNodeCollection<UsingModuleDeclaration>)] = () => ParseList<UsingModuleDeclaration>(),
                [typeof(SyntaxNodeCollection<LiteralExpression>)] = () => ParseListOfLiteralExpressions()
            };
        }

        public LambdaSharpParser(ILambdaSharpParserDependencyProvider provider, string filePath) : this(provider, "", filePath) { }

        public LambdaSharpParser(ILambdaSharpParserDependencyProvider provider, string workingDirectory, string filename) : this(provider) {
            ParseFileContents(workingDirectory, filename);
        }

        //--- Properties ---
        private ParsingEventWithFilePath<ParsingEvent> Current {
            get {
            again:
                var peek = _parsingEvents.Peek();
                var currentParsingEvent = peek.ParsingEnumerator.Current;

                // check if next event is an !Include statement
                if((currentParsingEvent is Scalar scalar) && (scalar.Tag == "!Include")) {

                    // NOTE (2020-06-07, bjorg): no need to call 'MoveNext()` as it will be called
                    //  automatically when the nested events have been processed.

                    // parse specified file
                    var directory = Path.GetDirectoryName(peek.FilePath) ?? throw new ShouldNeverHappenException();
                    ParseFileContents(directory, scalar.Value);
                    goto again;
                }
                return new ParsingEventWithFilePath<ParsingEvent>(peek.FilePath, peek.ParsingEnumerator.Current);
            }
        }

        //--- Methods ---
        public ModuleDeclaration? ParseModule() => ParseSyntaxOfType<ModuleDeclaration>();

        public AExpression? ParseExpression() {
            switch(Current.ParsingEvent) {
            case SequenceStart sequenceStart:
                return ConvertFunction(sequenceStart.Tag, ParseListExpression());
            case MappingStart mappingStart:
                return ConvertFunction(mappingStart.Tag, ParseObjectExpression());
            case Scalar scalar:
                return ConvertFunction(scalar.Tag, ParseLiteralExpression());
            default:
                Log(Error.ExpectedExpression, Location());
                SkipCurrent();
                return null;
            }

            // local functions
            AExpression? ParseObjectExpression() {
                if(!IsEvent<MappingStart>(out var mappingStart, out var filePath)) {
                    Log(Error.ExpectedMapExpression, Location());
                    SkipCurrent();
                    return null;
                }
                MoveNext();

                // parse declaration items in sequence
                var result = new ObjectExpression();
                while(!IsEvent<MappingEnd>(out var _, out var _)) {

                    // parse key
                    var keyScalar = Expect<Scalar>();
                    if(result.ContainsKey(keyScalar.ParsingEvent.Value)) {
                        Log(Error.DuplicateKey(keyScalar.ParsingEvent.Value), Location(keyScalar));
                        SkipCurrent();
                        continue;
                    }

                    // TODO: key could be 'Fn::Transform' indicating this structure is being transformed by a CloudFormation macro

                    // parse value
                    var value = ParseExpression();
                    if(value == null) {
                        continue;
                    }

                    // add key-value pair
                    result[new LiteralExpression(keyScalar.ParsingEvent.Value, LiteralType.String) {
                        SourceLocation = Location(keyScalar)
                    }] = value;
                }
                result.SourceLocation = Location(filePath, mappingStart, Current.ParsingEvent);
                MoveNext();
                return result;
            }

            AExpression? ParseListExpression() {
                if(!IsEvent<SequenceStart>(out var sequenceStart, out var filePath)) {
                    Log(Error.ExpectedListExpression, Location());
                    SkipCurrent();
                    return null;
                }
                MoveNext();

                // parse values in sequence
                var result = new ListExpression();
                while(!IsEvent<SequenceEnd>(out var _, out var _)) {
                    var item = ParseExpression();
                    if(item != null) {
                        result.Add(item);
                    }
                }
                result.SourceLocation = Location(filePath, sequenceStart, Current.ParsingEvent);
                MoveNext();
                return result;
            }

            AExpression? ParseLiteralExpression() {
                if(!IsEvent<Scalar>(out var scalar, out var filePath)) {
                    Log(Error.ExpectedLiteralValue, Location());
                    SkipCurrent();
                    return null;
                }
                MoveNext();

                // parse value
                string value;
                LiteralType type;
                switch(scalar.Style) {
                case ScalarStyle.Plain:
                    switch(scalar.Value) {

                    // null literal: https://yaml.org/type/null.html
                    case "~":
                    case "null":
                    case "Null":
                    case "NULL":
                    case "":

                        // TODO: 'null' is not allows in CloudFormation; how should we react?
                        value = "";
                        type = LiteralType.Null;
                        break;

                    // bool (true) literal: https://yaml.org/type/bool.html
                    case "y":
                    case "Y":
                    case "yes":
                    case "Yes":
                    case "YES":
                    case "true":
                    case "True":
                    case "TRUE":
                    case "on":
                    case "On":
                    case "ON":
                        value = "true";
                        type = LiteralType.Bool;
                        break;

                    // bool (false) literal: https://yaml.org/type/bool.html
                    case "n":
                    case "N":
                    case "no":
                    case "No":
                    case "NO":
                    case "false":
                    case "False":
                    case "FALSE":
                    case "off":
                    case "Off":
                    case "OFF":
                        value = "false";
                        type = LiteralType.Bool;
                        break;
                    default:
                        if(TryParseYamlInteger(scalar.Value, out var integerLiteral)) {
                            value = integerLiteral;
                            type = LiteralType.Integer;
                        } else if(TryParseYamlFloat(scalar.Value, out var floatLiteral)) {
                            value = floatLiteral.ToString();
                            type = LiteralType.Float;
                        } else if(DateTimeOffset.TryParseExact(scalar.Value, "yyyy-MM-dd", formatProvider: null, DateTimeStyles.AssumeUniversal, out var dateTimeLiteral)) {

                            // NOTE (2019-12-12, bjorg): timestamp literal: https://yaml.org/type/timestamp.html
                            //  [0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] # (year-month-date)
                            value = dateTimeLiteral.ToString();
                            type = LiteralType.Timestamp;
                        } else if(DateTimeOffset.TryParse(scalar.Value, formatProvider: null, DateTimeStyles.AssumeUniversal, out dateTimeLiteral)) {

                            // NOTE (2019-12-12, bjorg): timestamp literal: https://yaml.org/type/timestamp.html
                            //  [0-9][0-9][0-9][0-9] # (year)
                            //   -[0-9][0-9]? # (month)
                            //   -[0-9][0-9]? # (day)
                            //   ([Tt]|[ \t]+)[0-9][0-9]? # (hour)
                            //   :[0-9][0-9] # (minute)
                            //   :[0-9][0-9] # (second)
                            //   (\.[0-9]*)? # (fraction)
                            //   (([ \t]*)Z|[-+][0-9][0-9]?(:[0-9][0-9])?)? # (time zone)
                            value = dateTimeLiteral.ToUniversalTime().ToString();
                            type = LiteralType.Timestamp;
                        } else {

                            // always fallback to string literal
                            value = scalar.Value;
                            type = LiteralType.String;
                        }
                        break;
                    }
                    break;
                case ScalarStyle.DoubleQuoted:
                case ScalarStyle.SingleQuoted:
                case ScalarStyle.Folded:
                case ScalarStyle.Literal:

                    // all other scalar styles are always strings
                    value = scalar.Value;
                    type = LiteralType.String;
                    break;
                default:
                    throw new ShouldNeverHappenException($"unexpected scalar style: {scalar.Style} ({(int)scalar.Style})");
                }
                return new LiteralExpression(value, type) {
                    SourceLocation = Location(filePath, scalar)
                };
            }

            AExpression? ConvertFunction(string tag, AExpression? value) {
                if(value == null) {
                    return null;
                }

                // check if value is a long-form function
                AExpression? converted = value;
                if((value is ObjectExpression objectExpression) && (objectExpression.Count == 1)) {
                    var kv = objectExpression.First();
                    switch(kv.Key.Value) {
                    case "Fn::Base64":
                        converted = ConvertToBase64FunctionExpression(kv.Value);
                        break;
                    case "Fn::Cidr":
                        converted = ConvertToCidrFunctionExpression(kv.Value);
                        break;
                    case "Fn::FindInMap":
                        converted = ConvertToFindInMapFunctionExpression(kv.Value);
                        break;
                    case "Fn::GetAtt":
                        converted = ConvertToGetAttFunctionExpression(kv.Value);
                        break;
                    case "Fn::GetAZs":
                        converted = ConvertToGetAZsFunctionExpression(kv.Value);
                        break;
                    case "Fn::If":
                        converted = ConvertToIfFunctionExpression(kv.Value);
                        break;
                    case "Fn::ImportValue":
                        converted = ConvertToImportValueFunctionExpression(kv.Value);
                        break;
                    case "Fn::Join":
                        converted = ConvertToJoinFunctionExpression(kv.Value);
                        break;
                    case "Fn::Select":
                        converted = ConvertToSelectFunctionExpression(kv.Value);
                        break;
                    case "Fn::Split":
                        converted = ConvertToSplitFunctionExpression(kv.Value);
                        break;
                    case "Fn::Sub":
                        converted = ConvertToSubFunctionExpression(kv.Value);
                        break;
                    case "Fn::Transform":
                        converted = ConvertToTransformFunctionExpression(kv.Value);
                        break;
                    case "Ref":
                        converted = ConvertToRefFunctionExpression(kv.Value);
                        break;
                    case "Fn::Equals":
                        converted = ConvertToEqualsConditionExpression(kv.Value);
                        break;
                    case "Fn::Not":
                        converted = ConvertToNotConditionExpression(kv.Value);
                        break;
                    case "Fn::And":
                        converted = ConvertToAndConditionExpression(kv.Value);
                        break;
                    case "Fn::Or":
                        converted = ConvertToOrConditionExpression(kv.Value);
                        break;
                    case "Fn::Exists":
                        converted = ConvertToExistsExpression(kv.Value);
                        break;
                    case "Condition":
                        converted = ConvertToConditionRefExpression(kv.Value);
                        break;
                    default:

                        // leave as is
                        converted = value;
                        break;
                    }
                } else {

                    // leave as is
                    converted = value;
                }

                // check if there is anything to convert
                if(converted == null) {
                    return null;
                }

                // check if a short-form function tag needs to be applied
                switch(tag) {
                case null:

                    // nothing to do
                    return converted;
                case "!Base64":
                    return ConvertToBase64FunctionExpression(converted);
                case "!Cidr":
                    return ConvertToCidrFunctionExpression(converted);
                case "!FindInMap":
                    return ConvertToFindInMapFunctionExpression(converted);
                case "!GetAtt":
                    return ConvertToGetAttFunctionExpression(converted);
                case "!GetAZs":
                    return ConvertToGetAZsFunctionExpression(converted);
                case "!If":
                    return ConvertToIfFunctionExpression(converted);
                case "!ImportValue":
                    return ConvertToImportValueFunctionExpression(converted);
                case "!Join":
                    return ConvertToJoinFunctionExpression(converted);
                case "!Select":
                    return ConvertToSelectFunctionExpression(converted);
                case "!Split":
                    return ConvertToSplitFunctionExpression(converted);
                case "!Sub":
                    return ConvertToSubFunctionExpression(converted);
                case "!Transform":
                    return ConvertToTransformFunctionExpression(converted);
                case "!Ref":
                    return ConvertToRefFunctionExpression(converted);
                case "!Equals":
                    return ConvertToEqualsConditionExpression(converted);
                case "!Not":
                    return ConvertToNotConditionExpression(converted);
                case "!And":
                    return ConvertToAndConditionExpression(converted);
                case "!Or":
                    return ConvertToOrConditionExpression(converted);
                case "!Condition":
                    return ConvertToConditionRefExpression(converted);
                case "!Exists":
                    return ConvertToExistsExpression(converted);
                default:
                    Log(Error.UnknownFunctionTag(tag), converted.SourceLocation);
                    return null;
                }
            }

            AExpression ConvertToBase64FunctionExpression(AExpression value) {

                // !Base64 VALUE
                return new Base64FunctionExpression {
                    SourceLocation = value.SourceLocation,
                    Value = value
                };
            }

            AExpression? ConvertToCidrFunctionExpression(AExpression value) {

                // !Cidr [ VALUE, VALUE, VALUE ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 3) {
                        Log(Error.FunctionExpectsThreeParameters("!Cidr"), value.SourceLocation);
                        return null;
                    }
                    return new CidrFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        IpBlock = parameterList[0],
                        Count = parameterList[1],
                        CidrBits = parameterList[2]
                    };
                }
                Log(Error.FunctionInvalidParameter("!Cidr"), value.SourceLocation);
                return null;
            }

            AExpression? ConvertToFindInMapFunctionExpression(AExpression value) {

                // !FindInMap [ NAME, VALUE, VALUE ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 3) {
                        Log(Error.FunctionExpectsThreeParameters("!FindInMap"), value.SourceLocation);
                        return null;
                    }
                    if(!(parameterList[0] is LiteralExpression mapNameLiteral)) {
                        Log(Error.FunctionExpectsLiteralFirstParameter("!FindInMap"), parameterList[0].SourceLocation);
                        return null;
                    }
                    return new FindInMapFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        MapName = new LiteralExpression(mapNameLiteral.Value, LiteralType.String) {
                            SourceLocation = mapNameLiteral.SourceLocation
                        },
                        TopLevelKey = parameterList[1],
                        SecondLevelKey = parameterList[2]
                    };
                }
                Log(Error.FunctionInvalidParameter("!FindInMap"), value.SourceLocation);
                return null;
            }

            AExpression? ConvertToGetAttFunctionExpression(AExpression value) {

                // !GetAtt STRING
                if(value is LiteralExpression parameterLiteral) {
                    var referenceAndAttribute = parameterLiteral.Value.Split('.', 2);
                    return new GetAttFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        ReferenceName = new LiteralExpression(referenceAndAttribute[0], LiteralType.String) {
                            SourceLocation = parameterLiteral.SourceLocation
                        },
                        AttributeName = new LiteralExpression(
                            (referenceAndAttribute.Length == 2)
                                ? referenceAndAttribute[1]
                                : "",
                            LiteralType.String
                        ) {
                            SourceLocation = parameterLiteral.SourceLocation
                        }
                    };
                }

                // !GetAtt [ STRING, VALUE ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 2) {
                        Log(Error.FunctionExpectsTwoParameters("!GetAtt"), value.SourceLocation);
                        return null;
                    }
                    if(!(parameterList[0] is LiteralExpression resourceNameLiteral)) {
                        Log(Error.FunctionExpectsLiteralFirstParameter("!GetAtt"), parameterList[0].SourceLocation);
                        return null;
                    }
                    return new GetAttFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        ReferenceName = resourceNameLiteral,
                        AttributeName = parameterList[2]
                    };
                }
                Log(Error.FunctionInvalidParameter("!GetAtt"), value.SourceLocation);
                return null;
            }

            AExpression ConvertToGetAZsFunctionExpression(AExpression value) {

                // !GetAZs VALUE
                return new GetAZsFunctionExpression {
                    SourceLocation = value.SourceLocation,
                    Region = value
                };
            }

            AExpression? ConvertToIfFunctionExpression(AExpression value) {

                // !If [ NAME/CONDITION, VALUE, VALUE ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 3) {
                        Log(Error.FunctionExpectsThreeParameters("!If"), value.SourceLocation);
                        return null;
                    }
                    if(!(parameterList[0] is LiteralExpression conditionNameLiteral)) {
                        Log(Error.FunctionExpectsLiteralFirstParameter("!If"), parameterList[0].SourceLocation);
                        return null;
                    }
                    return new IfFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        Condition = new ConditionReferenceExpression {
                            SourceLocation = conditionNameLiteral.SourceLocation,
                            ReferenceName = new LiteralExpression(conditionNameLiteral.Value, LiteralType.String) {
                                SourceLocation = conditionNameLiteral.SourceLocation
                            }
                        },
                        IfTrue = parameterList[1],
                        IfFalse = parameterList[2]
                    };
                }
                Log(Error.FunctionInvalidParameter("!If"), value.SourceLocation);
                return null;
            }

            AExpression ConvertToImportValueFunctionExpression(AExpression value) {

                // !ImportValue VALUE
                return new ImportValueFunctionExpression {
                    SourceLocation = value.SourceLocation,
                    SharedValueToImport = value
                };
            }

            AExpression? ConvertToJoinFunctionExpression(AExpression value) {

                // !Join [ STRING, [ VALUE, ... ]]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 2) {
                        Log(Error.FunctionExpectsTwoParameters("!Join"), value.SourceLocation);
                        return null;
                    }
                    if(!(parameterList[0] is LiteralExpression separatorLiteral)) {
                        Log(Error.FunctionExpectsLiteralFirstParameter("!Join"), parameterList[0].SourceLocation);
                        return null;
                    }
                    return new JoinFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        Delimiter = separatorLiteral,
                        Values = parameterList[1]
                    };
                }
                Log(Error.FunctionInvalidParameter("!Join"), value.SourceLocation);
                return null;
            }

            AExpression? ConvertToSelectFunctionExpression(AExpression value) {

                // !Select [ VALUE, [ VALUE, ... ]]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 2) {
                        Log(Error.FunctionExpectsTwoParameters("!Select"), value.SourceLocation);
                        return null;
                    }
                    return new SelectFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        Index = parameterList[0],
                        Values = parameterList[1]
                    };
                }
                Log(Error.FunctionInvalidParameter("!Select"), value.SourceLocation);
                return null;
            }

            AExpression? ConvertToSplitFunctionExpression(AExpression value) {

                // !Split [ STRING, VALUE ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 2) {
                        Log(Error.FunctionExpectsTwoParameters("!Split"), value.SourceLocation);
                        return null;
                    }
                    if(!(parameterList[0] is LiteralExpression indexLiteral)) {
                        Log(Error.FunctionExpectsLiteralFirstParameter("!Split"), parameterList[0].SourceLocation);
                        return null;
                    }
                    return new SplitFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        Delimiter = indexLiteral,
                        SourceString = parameterList[1]
                    };
                }
                Log(Error.FunctionInvalidParameter("!Split"), value.SourceLocation);
                return null;
            }

            AExpression? ConvertToSubFunctionExpression(AExpression value) {

                // !Sub STRING
                if(value is LiteralExpression subLiteral) {
                    return new SubFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        FormatString = subLiteral,
                        Parameters = new ObjectExpression {
                            SourceLocation = value.SourceLocation
                        }
                    };
                }

                // !Sub [ STRING, { KEY: VALUE, ... }]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 2) {
                        Log(Error.FunctionExpectsTwoParameters("!Sub"), value.SourceLocation);
                        return null;
                    }
                    if(!(parameterList[0] is LiteralExpression formatLiteralExpression)) {
                        Log(Error.FunctionExpectsLiteralFirstParameter("!Sub"), parameterList[0].SourceLocation);
                        return null;
                    }
                    if(!(parameterList[1] is ObjectExpression parametersObject)) {
                        Log(Error.FunctionExpectsMapSecondParameter("!Sub"), parameterList[1].SourceLocation);
                        return null;
                    }
                    return new SubFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        FormatString = formatLiteralExpression,
                        Parameters = parametersObject
                    };
                }
                Log(Error.FunctionInvalidParameter("!Sub"), value.SourceLocation);
                return null;
            }

            AExpression? ConvertToTransformFunctionExpression(AExpression value) {

                // !Transform { Name: STRING, Parameters: { KEY: VALUE, ... } }
                if(value is ObjectExpression parameterMap) {
                    if(!parameterMap.TryGetValue("Name", out var macroNameExpression)) {
                        Log(Error.TransformFunctionMissingName, value.SourceLocation);
                        return null;
                    }
                    if(!(macroNameExpression is LiteralExpression macroNameLiteral)) {
                        Log(Error.TransformFunctionExpectsLiteralNameParameter, macroNameExpression.SourceLocation);
                        return null;
                    }
                    ObjectExpression? parametersMap = null;
                    if(parameterMap.TryGetValue("Parameters", out var parametersExpression)) {
                        if(!(parametersExpression is ObjectExpression)) {
                            Log(Error.TransformFunctionExpectsMapParametersParameter, parametersExpression.SourceLocation);
                            return null;
                        }
                        parameterMap = (ObjectExpression)parametersExpression;
                    }
                    return new TransformFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        MacroName = macroNameLiteral,
                        Parameters = parametersMap
                    };
                }
                Log(Error.FunctionInvalidParameter("!Transform"), value.SourceLocation);
                return null;
            }

            AExpression? ConvertToRefFunctionExpression(AExpression value) {

                // !Ref STRING
                if(value is LiteralExpression refLiteral) {
                    return new ReferenceFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        ReferenceName = new LiteralExpression(refLiteral.Value, LiteralType.String) {
                            SourceLocation = value.SourceLocation
                        }
                    };
                }
                Log(Error.FunctionInvalidParameter("!Ref"), value.SourceLocation);
                return null;
            }

            AExpression? ConvertToEqualsConditionExpression(AExpression value) {

                // !Equals [ VALUE, VALUE ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 2) {
                        Log(Error.FunctionExpectsTwoParameters("!Equals"), value.SourceLocation);
                        return null;
                    }
                    return new ConditionEqualsExpression {
                        SourceLocation = value.SourceLocation,
                        LeftValue = parameterList[0],
                        RightValue = parameterList[1]
                    };
                }
                Log(Error.FunctionInvalidParameter("!Equals"), value.SourceLocation);
                return null;
            }

            AExpression? ConvertToNotConditionExpression(AExpression value) {

                // !Not [ CONDITION ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 1) {
                        Log(Error.FunctionExpectsOneParameter("!Not"), value.SourceLocation);
                        return null;
                    }
                    return new ConditionNotExpression {
                        SourceLocation = value.SourceLocation,
                        Value = parameterList[0]
                    };
                }
                Log(Error.FunctionInvalidParameter("!Not"), value.SourceLocation);
                return null;
            }

            AExpression? ConvertToAndConditionExpression(AExpression value) {

                // !And [ CONDITION, CONDITION ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 2) {
                        Log(Error.FunctionExpectsTwoParameters("!And"), value.SourceLocation);
                        return null;
                    }
                    return new ConditionAndExpression {
                        SourceLocation = value.SourceLocation,
                        LeftValue = parameterList[0],
                        RightValue = parameterList[1]
                    };
                }
                Log(Error.FunctionInvalidParameter("!And"), value.SourceLocation);
                return null;
            }

            AExpression? ConvertToOrConditionExpression(AExpression value) {

                // !Or [ CONDITION, CONDITION ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 2) {
                        Log(Error.FunctionExpectsTwoParameters("!Or"), value.SourceLocation);
                        return null;
                    }
                    return new ConditionOrExpression {
                        SourceLocation = value.SourceLocation,
                        LeftValue = parameterList[0],
                        RightValue = parameterList[1]
                    };
                }
                Log(Error.FunctionInvalidParameter("!Or"), value.SourceLocation);
                return null;
            }

            AExpression? ConvertToConditionRefExpression(AExpression value) {

                // !Condition STRING
                if(value is LiteralExpression conditionLiteral) {
                    return new ConditionReferenceExpression {
                        SourceLocation = value.SourceLocation,
                        ReferenceName = new LiteralExpression(conditionLiteral.Value, LiteralType.String) {
                            SourceLocation = value.SourceLocation
                        }
                    };
                }
                Log(Error.FunctionInvalidParameter("!Condition"), value.SourceLocation);
                return null;
            }

            AExpression? ConvertToExistsExpression(AExpression value) {

                // !Exists STRING
                if(value is LiteralExpression conditionLiteral) {
                    return new ConditionExistsExpression {
                        SourceLocation = value.SourceLocation,
                        ReferenceName = new LiteralExpression(conditionLiteral.Value, LiteralType.String) {
                            SourceLocation = value.SourceLocation
                        }
                    };
                }
                Log(Error.FunctionInvalidParameter("!Condition"), value.SourceLocation);
                return null;
            }
        }

        public SyntaxNodeCollection<LiteralExpression>? ParseListOfLiteralExpressions() {
            var location = Location();
            var expression = ParseExpression();
            var result = new SyntaxNodeCollection<LiteralExpression> {
                SourceLocation = location
            };
            switch(expression) {
            case null:

                // nothing to do, error was already reported
                break;
            case LiteralExpression literalExpression:

                // for strings, check if literal is a comma-delimited list of values
                if(literalExpression.Type == LiteralType.String) {

                    // parse comma-separated items from literal value
                    var offset = 0;
                    while(offset < literalExpression.Value.Length) {

                        // skip whitespace at the beginning
                        for(; (offset < literalExpression.Value.Length) && char.IsWhiteSpace(literalExpression.Value[offset]); ++offset);

                        // find the next separator
                        var next = literalExpression.Value.IndexOf(',', offset);
                        if(next < 0) {
                            next = literalExpression.Value.Length;
                        }
                        var item = literalExpression.Value.Substring(offset, next - offset).TrimEnd();
                        if(!string.IsNullOrWhiteSpace(item)) {

                            // calculate relative position of sub-string in literal expression
                            var startLineOffset = literalExpression.Value.Take(offset).Count(c => c == '\n');
                            var endLineOffset = literalExpression.Value.Take(offset + item.Length).Count(c => c == '\n');
                            var startColumnOffset = literalExpression.Value.Take(offset).Reverse().TakeWhile(c => c != '\n').Count();
                            var endColumnOffset = literalExpression.Value.Take(offset + item.Length).Reverse().TakeWhile(c => c != '\n').Count();

                            // add literal value
                            result.Add(new LiteralExpression(item, LiteralType.String) {
                                SourceLocation = new SourceLocation(
                                    literalExpression.SourceLocation.FilePath,
                                    literalExpression.SourceLocation.LineNumberStart + startLineOffset,
                                    literalExpression.SourceLocation.LineNumberStart + endLineOffset,
                                    literalExpression.SourceLocation.ColumnNumberStart + startColumnOffset,
                                    literalExpression.SourceLocation.ColumnNumberStart + endColumnOffset - 1
                                )
                            });
                        }
                        offset = next + 1;
                    }
                } else {

                    // keep non-strings literals as-is
                    result.Add(literalExpression);
                }
                break;
            case ListExpression listExpression:

                // check that all items in the list are literals
                var nonLiteralItems = listExpression.Where(item => !(item is LiteralExpression)).ToList();
                if(nonLiteralItems.Any()) {
                    foreach(var nonLiteralItem in nonLiteralItems) {
                        Log(Error.ExpectedLiteralValue, nonLiteralItem.SourceLocation);
                    };
                    return null;
                } else {
                    foreach(var item in listExpression) {
                        result.Add((LiteralExpression)item);
                    }
                }
                break;
            default:
                Log(Error.ExpectedListExpression, expression.SourceLocation);
                break;
            }
            return result;
        }

        private void ParseFileContents(string workingDirectory, string filename) {
            var filePath = Path.Combine(workingDirectory, filename);
            string contents;

            // check if working directory is current assembly name
            var assembly = GetType().Assembly;
            var assemblyName = assembly.GetName().Name;
            if(workingDirectory == $"{assemblyName}.dll") {
                var resourceName = $"{assemblyName}.Resources.{filename.Replace(" ", "_").Replace("\\", ".").Replace("/", ".")}";
                using var resource = assembly.GetManifestResourceStream(resourceName);

                // TODO: don't throw an exception; log an error instead
                using var reader = new StreamReader(resource ?? throw new LambdaSharpParserException($"unable to locate embedded resource: '{resourceName}' in assembly '{assembly.GetName().Name}'", new SourceLocation(
                    resourceName,
                    lineNumberStart: 0,
                    lineNumberEnd: 0,
                    columnNumberStart: 0,
                    columnNumberEnd: 0
                )), Encoding.UTF8);
                contents = reader.ReadToEnd().Replace("\r", "");
            } else {
                contents = _provider.ReadFile(filePath);
            }

            // check if a YAML file is being parsed
            switch(Path.GetExtension(filename).ToLowerInvariant()) {
            case ".yml":
                ParseYaml(filePath, contents);
                break;
            default:
                ParseText(filePath, contents);
                break;
            }
        }

        private void ParseYaml(string filePath, string source) {
            var parsingEvents = new List<ParsingEvent>();
            using(var reader = new StringReader(source)) {
                var parser = new YamlDotNet.Core.Parser(reader);

                // read prologue parsing events
                parser.Expect<StreamStart>();
                parser.Expect<DocumentStart>();

                // keep reading until the end of the document is reached
                while(!(parser.Current is DocumentEnd)) {
                    parsingEvents.Add(parser.Current);
                    parser.MoveNext();
                }

                // read epilogue parsing events
                parser.Expect<DocumentEnd>();
                parser.Expect<StreamEnd>();
            }
            var enumerator = parsingEvents.GetEnumerator();
            if(enumerator.MoveNext()) {
                _parsingEvents.Push((FilePath: filePath, ParsingEnumerator: enumerator));
            }
        }

        private void ParseText(string filePath, string source) {

            // parse non-YAML file as a plaint text file
            var lines = source.Count(c => c.Equals('\n')) + 1;
            var lastLineOffset = source.LastIndexOf('\n');
            var lastLineColumnsCount = (lastLineOffset < 0)
                ? source.Length
                : source.Length - lastLineOffset;

            // push enumerator with single scalar event onto stack
            var enumerator = new List<ParsingEvent> {
                new Scalar(
                    anchor: null,
                    tag: null,
                    value: source,
                    style: ScalarStyle.Plain,
                    isPlainImplicit: true,
                    isQuotedImplicit: true,
                    start: new Mark(index: 0, line: 1, column: 1),
                    end: new Mark(source.Length, lines, lastLineColumnsCount)
                )
            }.GetEnumerator();
            enumerator.MoveNext();
            _parsingEvents.Push((FilePath: filePath, ParsingEnumerator: enumerator));
        }

        private T? ParseExpressionOfType<T>(Error error) where T : AExpression {
            var result = ParseExpression();
            if(!(result is T expression)) {

                // NOTE (2020-02-15, bjorg): only log if result is not null, otherwise an error was already emitted
                if(result != null) {
                    Log(error, result.SourceLocation);
                }
                return null;
            }
            return expression;
        }

        private SyntaxNodeCollection<T>? ParseList<T>() where T : ASyntaxNode {
            if(!IsEvent<SequenceStart>(out var sequenceStart, out var _) || (sequenceStart.Tag != null)) {
                Log(Error.ExpectedListExpression, Location());
                SkipCurrent();
                return null;
            }
            var location = Location();
            MoveNext();

            // parse declaration items in sequence
            var result = new SyntaxNodeCollection<T> {
                SourceLocation = location
            };
            while(!IsEvent<SequenceEnd>(out var _, out var _)) {
                if(TryParse(typeof(T), out var item)) {
                    try {
                        result.Add((T)item);
                    } catch(InvalidCastException) {
                        throw new ShouldNeverHappenException($"expected type {typeof(T).FullName}, but found {item.GetType().FullName} instead");
                    }
                }
            }
            MoveNext();
            return result;
        }

        private T? ParseSyntaxOfType<T>() where T : ASyntaxNode {

            // fetch all possible syntax options for specified type
            var syntaxes = GetSyntaxes(typeof(T));

            // ensure first event is the beginning of a map
            if(!IsEvent<MappingStart>(out var mappingStart, out var filePath) || (mappingStart.Tag != null)) {
                Log(Error.ExpectedMapExpression, Location());
                SkipCurrent();
                return null;
            }
            MoveNext();
            T? result = null;

            // parse mappings
            var foundKeys = new HashSet<string>();
            HashSet<string>? mandatoryKeys = null;
            SyntaxInfo? syntax = null;
            while(!IsEvent<MappingEnd>(out var _, out var _)) {

                // parse key
                var keyScalar = Expect<Scalar>();
                var key = keyScalar.ParsingEvent.Value;

                // check if this is the first key being parsed
                if(syntax == null) {
                    if((syntaxes.Count == 1) && syntaxes.TryGetValue("", out syntax)) {

                        // parse using the default syntax
                        var instance = Activator.CreateInstance(syntax.DeclarationType);
                        if(instance == null) {
                            throw new LambdaSharpParserException($"unsupported declaration type: {syntax.DeclarationType.FullName}", Location());
                        }
                        result = (T)instance;
                        result.SourceLocation = Location(keyScalar);
                        mandatoryKeys = new HashSet<string>(syntax.MandatoryKeys);
                    } else if(syntaxes.TryGetValue(key, out syntax)) {

                        // parse using the syntax matching the first key (akin to a keyword)
                        if(TryParse(syntax.KeywordType ?? throw new ShouldNeverHappenException($"Keyword: {syntax.KeywordType}"), out var keywordValue)) {
                            var instance = Activator.CreateInstance(syntax.DeclarationType, new object[] { keywordValue });
                            if(instance == null) {
                                throw new LambdaSharpParserException($"unsupported declaration type: {syntax.DeclarationType.FullName}", Location());
                            }
                            result = (T)instance;
                        } else {

                            // skip the value of the key
                            SkipCurrent();

                            // skip all remaining key-value pairs
                            SkipMapRemainingEvents();
                            return null;
                        }
                        result.SourceLocation = Location(keyScalar);
                        mandatoryKeys = new HashSet<string>(syntax.MandatoryKeys);

                        // continue to next key-value pair since we already parsed the keyword value
                        continue;
                    } else {
                        Log(Error.UnrecognizedModuleItem(key), Location(keyScalar));

                        // skip the value the key
                        SkipCurrent();

                        // skip all remaining key-value pairs
                        SkipMapRemainingEvents();
                        return null;
                    }
                }

                // map read key to syntax property
                if(syntax.Keys.TryGetValue(key, out var keyProperty)) {

                    // check if key is a duplicate
                    if(!foundKeys.Add(key)) {
                        Log(Error.DuplicateKey(key), Location(keyScalar));

                        // skip the value of the value
                        SkipCurrent();
                        continue;
                    }

                    // remove key from mandatory keys
                    if(mandatoryKeys == null) {
                        throw new ShouldNeverHappenException();
                    }
                    mandatoryKeys.Remove(key);

                    // find type appropriate parser and set target property with the parser outcome
                    if(TryParse(keyProperty.PropertyType, out var value)) {
                        keyProperty.SetValue(result, value);
                    }
                } else {
                    Log(Error.UnexpectedKey(key), Location(keyScalar));

                    // no need to parse an invalid key
                    SkipCurrent();
                }
            }

            // check for missing mandatory keys
            if(mandatoryKeys?.Any() ?? false) {
                Log(Error.RequiredKeysMissing(mandatoryKeys), Location(filePath, mappingStart));
            }
            if(result != null) {
                result.SourceLocation = Location(filePath, mappingStart, Current.ParsingEvent);
            }
            MoveNext();
            return result;
        }

        private void Log(Error error, SourceLocation? location) => _provider.Logger.Log(error, location ?? SourceLocation.Empty);

        private SourceLocation Location() {
            var current = Current;
            return Location(current.FilePath, current.ParsingEvent, current.ParsingEvent);
        }

        private SourceLocation Location<T>(ParsingEventWithFilePath<T> current) where T : ParsingEvent
            => Location(current.FilePath, current.ParsingEvent, current.ParsingEvent);

        private SourceLocation Location(string filePath, ParsingEvent parsingEvent) => Location(filePath, parsingEvent, parsingEvent);

        private SourceLocation Location(string filePath, ParsingEvent startParsingEvent, ParsingEvent stopParsingEvent) => new SourceLocation(
            filePath,
            startParsingEvent.Start.Line,
            stopParsingEvent.End.Line,
            startParsingEvent.Start.Column,
            stopParsingEvent.End.Column
        );

        private Dictionary<string, SyntaxInfo> GetSyntaxes(Type type) {
            if(!_syntaxCache.TryGetValue(type, out var syntaxes)) {
                if(type.IsAbstract) {
                    syntaxes = new Dictionary<string, SyntaxInfo>();

                    // for abstract types, we build a list of derived types recursively
                    foreach(var kv in type.Assembly
                        .GetTypes()
                        .Where(definedType => definedType.BaseType == type)
                        .SelectMany(derivedType => GetSyntaxes(derivedType))
                    ) {
                        syntaxes.Add(kv.Key, kv.Value);
                    }
                } else {

                    // for concrete types, we use the type itself
                    if(!_typeToSyntax.TryGetValue(type, out var syntax)) {
                        syntax = new SyntaxInfo(type);
                        _typeToSyntax[type] = syntax;
                    }
                    syntaxes = new Dictionary<string, SyntaxInfo>();

                    // validate that we can only have a default syntax if it's the only syntax
                    var keyword = syntax.Keyword ?? "";
                    if((keyword != "") && syntaxes.ContainsKey("")) {
                        throw new ShouldNeverHappenException("attempted to add a keyword syntax to a collection containing a default syntax");
                    } else if((keyword == "") && (syntaxes.Count != 0)) {
                        throw new ShouldNeverHappenException("attempted to add a default syntax to a collection with keyword syntaxes");
                    }
                    syntaxes.Add(keyword, syntax);
                }
                _syntaxCache[type] = syntaxes;
            }
            return syntaxes;
        }

        private bool TryParse(Type type, [NotNullWhen(true)] out object? result) {
            if(_typeParsers.TryGetValue(type, out var parser)) {
                result = parser();
                return result != null;
            }
            Log(Error.MissingParserDefinition(type.Name), Location());
            result = null;
            SkipCurrent();
            return false;
        }

        private bool IsEvent<T>([NotNullWhen(true)] out T? parsingEvent, [NotNullWhen(true)] out string? filePath) where T : ParsingEvent {
            var current = Current;
            if(current.ParsingEvent is T typedParsingEvent) {
                parsingEvent = typedParsingEvent;
                filePath = current.FilePath;
                return true;
            }
            parsingEvent = null;
            filePath = null;
            return false;
        }

        private bool MoveNext() {
            while(_parsingEvents.Any()) {
                var current = _parsingEvents.Peek();
                if(current.ParsingEnumerator.MoveNext()) {
                    return true;
                }
                current.ParsingEnumerator.Dispose();
                _parsingEvents.Pop();
            }
            return false;
        }

        private void SkipCurrent() {
            var current = Current.ParsingEvent;
            MoveNext();
            switch(current) {
            case Scalar _:

                // nothing to do
                break;
            case MappingStart _:
                while(!IsEvent<MappingEnd>(out var _, out var _)) {

                    // read key
                    SkipCurrent();

                    // read value
                    SkipCurrent();
                }
                MoveNext();
                break;
            case SequenceStart _:
                while(!IsEvent<SequenceEnd>(out var _, out var _)) {

                    // read value
                    SkipCurrent();
                }
                MoveNext();
                break;
            default:
                throw new LambdaSharpParserException($"Unexpected parsing event {Current.ParsingEvent.GetType().Name ?? "<null>"}", Location());
            }
        }

        private void SkipMapRemainingEvents() {
            while(!IsEvent<MappingEnd>(out var _, out var _)) {
                SkipCurrent();
            }
            MoveNext();
        }

        private ParsingEventWithFilePath<T> Expect<T>() where T : ParsingEvent {
            if(!IsEvent<T>(out var parsingEvent, out var filePath)) {
                throw new LambdaSharpParserException($"Expected parsing event {typeof(T).Name} instead of {Current.ParsingEvent.GetType().Name ?? "<null>"}", Location());
            }
            MoveNext();
            return new ParsingEventWithFilePath<T>(filePath, parsingEvent);
        }
    }
}