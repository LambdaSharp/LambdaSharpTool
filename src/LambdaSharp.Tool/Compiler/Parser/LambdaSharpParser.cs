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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using LambdaSharp.Tool.Compiler.Parser.Syntax;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace LambdaSharp.Tool.Compiler.Parser {

    public interface ILambdaSharpParserDependencyProvider {

        //--- Methods ---
        void Log(Error error, SourceLocation sourceLocation);
        string ReadFile(string filePath);
    }

    public sealed class LambdaSharpParser {

        // TODO:
        //  - `cloudformation package`, when given a YAML template, converts even explicit strings to integers when the string begins with a 0 and contains nothing but digits · Issue #2934 · aws/aws-cli · GitHub (https://github.com/aws/aws-cli/issues/2934)
        //  - 'null' literal can also be used where keys are allowed! (e.g. ~: foo)

        //--- Types ---
        private class SyntaxInfo {

            //--- Constructors ---
            public SyntaxInfo(Type type) {

                // NOTE: extract syntax poproperties from type to identify what keys are expected and required;
                //  in addition, one key can be called out as the keyword, which means it must be the first key
                //  to appear in the mapping.
                var syntaxProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    .Select(property => new {
                        Syntax = property.GetCustomAttributes<ASyntaxAttribute>().FirstOrDefault(),
                        Property = property
                    })
                    .Where(tuple => tuple.Syntax != null)
                    .ToList();
                Type = type;
                Keyword = syntaxProperties.FirstOrDefault(tuple => tuple.Syntax.Type == SyntaxType.Keyword)?.Property.Name;
                Keys = syntaxProperties.ToDictionary(tuple => tuple.Property.Name, Tuple => Tuple.Property);
                MandatoryKeys = syntaxProperties.Where(tuple => tuple.Syntax.Type != SyntaxType.Optional).Select(tuple => tuple.Property.Name).ToArray();
            }

            //--- Properties ---
            public Type Type { get; private set; }
            public string Keyword { get; private set; }
            public Dictionary<string, PropertyInfo> Keys { get; private set; }
            public IEnumerable<string> MandatoryKeys { get; private set; }
        }

        //--- Fields ---
        private readonly ILambdaSharpParserDependencyProvider _provider;
        private readonly Dictionary<Type, Func<object>> _typeParsers;
        private readonly Dictionary<Type, SyntaxInfo> _typeToSyntax = new Dictionary<Type, SyntaxInfo>();
        private readonly Dictionary<Type, Dictionary<string, SyntaxInfo>> _syntaxCache = new Dictionary<Type, Dictionary<string, SyntaxInfo>>();
        private readonly Stack<(string FilePath, IEnumerator<ParsingEvent> ParsingEnumerator)> _parsingEvents = new Stack<(string, IEnumerator<ParsingEvent>)>();

        //--- Constructors ---
        public LambdaSharpParser(ILambdaSharpParserDependencyProvider provider) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _typeParsers = new Dictionary<Type, Func<object>> {

                // expressions
                [typeof(AExpression)] = () => ParseExpression(),
                [typeof(ObjectExpression)] = () => ParseExpressionOfType<ObjectExpression>(Error.ExpectedMapExpression),
                [typeof(ListExpression)] = () => ParseExpressionOfType<ListExpression>(Error.ExpectedListExpression),
                [typeof(LiteralExpression)] = () => ParseExpressionOfType<LiteralExpression>(Error.ExpectedLiteralValue),

                // declarations
                [typeof(ModuleDeclaration)] = () => ParseSyntaxOfType<ModuleDeclaration>(),
                [typeof(UsingDeclaration)] = () => ParseSyntaxOfType<UsingDeclaration>(),
                [typeof(FunctionDeclaration.VpcExpression)] = () => ParseSyntaxOfType<FunctionDeclaration.VpcExpression>(),
                [typeof(ResourceTypeDeclaration.PropertyTypeExpression)] = () => ParseSyntaxOfType<ResourceTypeDeclaration.PropertyTypeExpression>(),
                [typeof(ResourceTypeDeclaration.AttributeTypeExpression)] = () => ParseSyntaxOfType<ResourceTypeDeclaration.AttributeTypeExpression>(),

                // TODO: enumerate all acceptable types explicitly instead of relying on inheritance
                [typeof(AItemDeclaration)] = () => ParseSyntaxOfType<AItemDeclaration>(),
                [typeof(AEventSourceDeclaration)] = () => ParseSyntaxOfType<AEventSourceDeclaration>(),

                // lists
                [typeof(List<AItemDeclaration>)] = () => ParseList<AItemDeclaration>(),
                [typeof(List<AExpression>)] = () => ParseList<AExpression>(),
                [typeof(List<AEventSourceDeclaration>)] = () => ParseList<AEventSourceDeclaration>(),
                [typeof(List<UsingDeclaration>)] = () => ParseList<UsingDeclaration>(),
                [typeof(List<LiteralExpression>)] = () => ParseListOfLiteralExpressions()
            };
        }

        public LambdaSharpParser(ILambdaSharpParserDependencyProvider provider, string filePath) : this(provider) {
            ParseFile(filePath);
        }

        //--- Properties ---
        private (string FilePath, ParsingEvent ParsingEvent) Current {
            get {
            again:
                var peek = _parsingEvents.Peek();
                var currentParsingEvent = peek.ParsingEnumerator.Current;

                // check if next event is an !Include statement
                if((currentParsingEvent is Scalar scalar) && (scalar.Tag == "!Include")) {

                    // consume !Include event
                    MoveNext();

                    // parse specified file
                    ParseFile(Path.Combine(Path.GetDirectoryName(peek.FilePath), scalar.Value));
                    goto again;
                }
                return (FilePath: peek.FilePath, ParsingEvent: peek.ParsingEnumerator.Current);
            }
        }

        //--- Methods ---
        public void ParseFile(string filePath) {
            var contents = _provider.ReadFile(filePath);

            // check if a YAML file is being parsed
            switch(Path.GetExtension(filePath).ToLowerInvariant()) {
            case ".yml":
                ParseYaml(filePath, contents);
                break;
            default:
                ParseText(filePath, contents);
                break;
            }
        }

        public void ParseYaml(string filePath, string source) {
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

        public void ParseText(string filePath, string source) {

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

        public List<T> ParseList<T>() where T : ASyntaxNode {
            if(!IsEvent<SequenceStart>(out var sequenceStart, out var _) || (sequenceStart.Tag != null)) {
                Log(Error.ExpectedListExpression, Location());
                SkipThisAndNestedEvents();
                return null;
            }
            MoveNext();

            // parse declaration items in sequence
            var result = new List<T>();
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

        public T ParseSyntaxOfType<T>() where T : ASyntaxNode {

            // fetch all possible syntax options for specified type
            var syntaxes = GetSyntaxes(typeof(T));

            // ensure first event is the beginning of a map
            if(!IsEvent<MappingStart>(out var mappingStart, out var filePath) || (mappingStart.Tag != null)) {
                Log(Error.ExpectedMapExpression, Location());
                SkipThisAndNestedEvents();
                return null;
            }
            MoveNext();
            T result = null;

            // parse mappings
            var foundKeys = new HashSet<string>();
            HashSet<string> mandatoryKeys = null;
            SyntaxInfo syntax = null;
            while(!IsEvent<MappingEnd>(out var _, out var _)) {

                // parse key
                var keyScalar = Expect<Scalar>();
                var key = keyScalar.ParsingEvent.Value;

                // check if this is the first key being parsed
                if(syntax == null) {
                    if(syntaxes.TryGetValue(key, out syntax)) {
                        result = (T)Activator.CreateInstance(syntax.Type);
                        mandatoryKeys = new HashSet<string>(syntax.MandatoryKeys);
                    } else {
                        Log(Error.UnrecognizedModuleItem(key), Location(keyScalar));

                        // skip the value of the key
                        SkipThisAndNestedEvents();

                        // skip all remaining key-value pairs
                        while(!IsEvent<MappingEnd>(out var _, out var _)) {

                            // skip key
                            Expect<Scalar>();

                            // skip value
                            SkipThisAndNestedEvents();
                        }
                        return null;
                    }
                }

                // map read key to syntax property
                if(syntax.Keys.TryGetValue(key, out var keyProperty)) {

                    // check if key is a duplicate
                    if(!foundKeys.Add(key)) {
                        Log(Error.DuplicateKey(key), Location(keyScalar));

                        // no need to parse the value for the duplicate key
                        SkipThisAndNestedEvents();
                        continue;
                    }

                    // remove key from mandatory keys
                    mandatoryKeys.Remove(key);

                    // find type appropriate parser and set target property with the parser outcome
                    if(TryParse(keyProperty.PropertyType, out var value)) {
                        keyProperty.SetValue(result, value);
                    }
                } else {
                    Log(Error.UnexpectedKey(key), Location(keyScalar));

                    // no need to parse an invalid key
                    SkipThisAndNestedEvents();
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

        public T ParseExpressionOfType<T>(Error error) where T : AExpression {
            var result = ParseExpression();
            if(!(result is T expression)) {
                Log(error, result.SourceLocation);
                return null;
            }
            return expression;
        }

        public AExpression ParseExpression() {
            switch(Current.ParsingEvent) {
            case SequenceStart sequenceStart:
                return ConvertFunction(sequenceStart.Tag, ParseListExpression());
            case MappingStart mappingStart:
                return ConvertFunction(mappingStart.Tag, ParseObjectExpression());
            case Scalar scalar:
                return ConvertFunction(scalar.Tag, ParseLiteralExpression());
            default:
                Log(Error.ExpectedExpression, Location());
                SkipThisAndNestedEvents();
                return null;
            }

            // local functions
            AExpression ParseObjectExpression() {
                if(!IsEvent<MappingStart>(out var mappingStart, out var filePath)) {
                    Log(Error.ExpectedMapExpression, Location());
                    SkipThisAndNestedEvents();
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
                        SkipThisAndNestedEvents();
                        continue;
                    }

                    // TODO: key could be 'Fn::Transform' indicating this structure is being transformed by a CloudFormation macro

                    // parse value
                    var value = ParseExpression();
                    if(value == null) {
                        continue;
                    }

                    // add key-value pair
                    result.Items.Add(new ObjectExpression.KeyValuePair {
                        Key = new LiteralExpression {
                            SourceLocation = Location(keyScalar),
                            Value = keyScalar.ParsingEvent.Value
                        },
                        Value = value
                    });
                }
                result.SourceLocation = Location(filePath, mappingStart, Current.ParsingEvent);
                MoveNext();
                return result;
            }

            AExpression ParseListExpression() {
                if(!IsEvent<SequenceStart>(out var sequenceStart, out var filePath)) {
                    Log(Error.ExpectedListExpression, Location());
                    SkipThisAndNestedEvents();
                    return null;
                }
                MoveNext();

                // parse values in sequence
                var result = new ListExpression();
                while(!IsEvent<SequenceEnd>(out var _, out var _)) {
                    var item = ParseExpression();
                    if(item != null) {
                        result.Items.Add(item);
                    }
                }
                result.SourceLocation = Location(filePath, sequenceStart, Current.ParsingEvent);
                MoveNext();
                return result;
            }

            AExpression ParseLiteralExpression() {

                // TODO: handle unquoted literals

                if(!IsEvent<Scalar>(out var scalar, out var filePath)) {
                    Log(Error.ExpectedLiteralValue, Location());
                    SkipThisAndNestedEvents();
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
                            //  [0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] # (ymd)
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
                return new LiteralExpression {
                    SourceLocation = Location(filePath, scalar),
                    Value = value,
                    Type = type
                };

                // local functions
                bool TryParseYamlInteger(string value, out string number) {

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

                bool TryParseYamlFloat(string value, out double number) {

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
            }

            AExpression ConvertFunction(string tag, AExpression value) {

                // check if value is a long-form function
                if((value is ObjectExpression objectExpression) && (objectExpression.Items.Count == 1)) {
                    var kv = objectExpression.Items.First();
                    switch(kv.Key.Value) {
                    case "Fn::Base64":
                        value = ConvertToBase64FunctionExpression(kv.Value);
                        break;
                    case "Fn::Cidr":
                        value = ConvertToCidrFunctionExpression(kv.Value);
                        break;
                    case "Fn::FindInMap":
                        value = ConvertToFindInMapFunctionExpression(kv.Value);
                        break;
                    case "Fn::GetAtt":
                        value = ConvertToGetAttFunctionExpression(kv.Value);
                        break;
                    case "Fn::GetAZs":
                        value = ConvertToGetAZsFunctionExpression(kv.Value);
                        break;
                    case "Fn::If":
                        value = ConvertToIfFunctionExpression(kv.Value);
                        break;
                    case "Fn::ImportValue":
                        value = ConvertToImportValueFunctionExpression(kv.Value);
                        break;
                    case "Fn::Join":
                        value = ConvertToJoinFunctionExpression(kv.Value);
                        break;
                    case "Fn::Select":
                        value = ConvertToSelectFunctionExpression(kv.Value);
                        break;
                    case "Fn::Split":
                        value = ConvertToSplitFunctionExpression(kv.Value);
                        break;
                    case "Fn::Sub":
                        value = ConvertToSubFunctionExpression(kv.Value);
                        break;
                    case "Fn::Transform":
                        value = ConvertToTransformFunctionExpression(kv.Value);
                        break;
                    case "Ref":
                        value = ConvertToRefFunctionExpression(kv.Value);
                        break;
                    case "Fn::Equals":
                        value = ConvertToEqualsConditionExpression(kv.Value);
                        break;
                    case "Fn::Not":
                        value = ConvertToNotConditionExpression(kv.Value);
                        break;
                    case "Fn::And":
                        value = ConvertToAndConditionExpression(kv.Value);
                        break;
                    case "Fn::Or":
                        value = ConvertToOrConditionExpression(kv.Value);
                        break;
                    case "Condition":
                        value = ConvertToConditionRefExpression(kv.Value);
                        break;
                    default:

                        // leave as is
                        break;
                    }
                }

                // check if there is anything to convert
                if(value == null) {
                    return null;
                }

                // check if a short-form function tag needs to be applied
                switch(tag) {
                case null:

                    // nothing to do
                    return value;
                case "!Base64":
                    return ConvertToBase64FunctionExpression(value);
                case "!Cidr":
                    return ConvertToCidrFunctionExpression(value);
                case "!FindInMap":
                    return ConvertToFindInMapFunctionExpression(value);
                case "!GetAtt":
                    return ConvertToGetAttFunctionExpression(value);
                case "!GetAZs":
                    return ConvertToGetAZsFunctionExpression(value);
                case "!If":
                    return ConvertToIfFunctionExpression(value);
                case "!ImportValue":
                    return ConvertToImportValueFunctionExpression(value);
                case "!Join":
                    return ConvertToJoinFunctionExpression(value);
                case "!Select":
                    return ConvertToSelectFunctionExpression(value);
                case "!Split":
                    return ConvertToSplitFunctionExpression(value);
                case "!Sub":
                    return ConvertToSubFunctionExpression(value);
                case "!Transform":
                    return ConvertToTransformFunctionExpression(value);
                case "!Ref":
                    return ConvertToRefFunctionExpression(value);
                case "!Equals":
                    return ConvertToEqualsConditionExpression(value);
                case "!Not":
                    return ConvertToNotConditionExpression(value);
                case "!And":
                    return ConvertToAndConditionExpression(value);
                case "!Or":
                    return ConvertToOrConditionExpression(value);
                case "!Condition":
                    return ConvertToConditionRefExpression(value);
                default:
                    Log(Error.UnknownFunctionTag(tag), value.SourceLocation);
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

            AExpression ConvertToCidrFunctionExpression(AExpression value) {

                // !Cidr [ VALUE, VALUE, VALUE ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Items.Count != 3) {
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

            AExpression ConvertToFindInMapFunctionExpression(AExpression value) {

                // !FindInMap [ NAME, VALUE, VALUE ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Items.Count != 3) {
                        Log(Error.FunctionExpectsThreeParameters("!FindInMap"), value.SourceLocation);
                        return null;
                    }
                    if(!(parameterList[0] is LiteralExpression mapNameLiteral)) {
                        Log(Error.FunctionExpectsLiteralFirstParameter("!FindInMap"), parameterList[0].SourceLocation);
                        return null;
                    }
                    return new FindInMapFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        MapName = new LiteralExpression {
                            SourceLocation = mapNameLiteral.SourceLocation,
                            Value = mapNameLiteral.Value
                        },
                        TopLevelKey = parameterList[1],
                        SecondLevelKey = parameterList[2]
                    };
                }
                Log(Error.FunctionInvalidParameter("!FindInMap"), value.SourceLocation);
                return null;
            }

            AExpression ConvertToGetAttFunctionExpression(AExpression value) {

                // !GetAtt STRING
                if(value is LiteralExpression parameterLiteral) {
                    var referenceAndAttribute = parameterLiteral.Value.Split('.', 2);
                    return new GetAttFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        ReferenceName = new LiteralExpression {
                            SourceLocation = parameterLiteral.SourceLocation,
                            Value = referenceAndAttribute[0]
                        },
                        AttributeName = new LiteralExpression {
                            SourceLocation = parameterLiteral.SourceLocation,
                            Value = (referenceAndAttribute.Length == 2) ? referenceAndAttribute[1] : ""
                        }
                    };
                }

                // !GetAtt [ STRING, VALUE ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Items.Count != 2) {
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

            AExpression ConvertToIfFunctionExpression(AExpression value) {

                // !If [ NAME/CONDITION, VALUE, VALUE ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Items.Count != 3) {
                        Log(Error.FunctionExpectsThreeParameters("!If"), value.SourceLocation);
                        return null;
                    }
                    if(!(parameterList[0] is LiteralExpression conditionNameLiteral)) {
                        Log(Error.FunctionExpectsLiteralFirstParameter("!If"), parameterList[0].SourceLocation);
                        return null;
                    }
                    return new IfFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        Condition = new ConditionExpression {
                            SourceLocation = conditionNameLiteral.SourceLocation,
                            ReferenceName = new LiteralExpression {
                                SourceLocation = conditionNameLiteral.SourceLocation,
                                Value = conditionNameLiteral.Value
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

            AExpression ConvertToJoinFunctionExpression(AExpression value) {

                // !Join [ STRING, [ VALUE, ... ]]
                if(value is ListExpression parameterList) {
                    if(parameterList.Items.Count != 2) {
                        Log(Error.FunctionExpectsTwoParameters("!Join"), value.SourceLocation);
                        return null;
                    }
                    if(!(parameterList[0] is LiteralExpression separatorLiteral)) {
                        Log(Error.FunctionExpectsLiteralFirstParameter("!Join"), parameterList[0].SourceLocation);
                        return null;
                    }
                    return new JoinFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        Separator = separatorLiteral,
                        Values = parameterList[1]
                    };
                }
                Log(Error.FunctionInvalidParameter("!Join"), value.SourceLocation);
                return null;
            }

            AExpression ConvertToSelectFunctionExpression(AExpression value) {

                // !Select [ VALUE, [ VALUE, ... ]]
                if(value is ListExpression parameterList) {
                    if(parameterList.Items.Count != 2) {
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

            AExpression ConvertToSplitFunctionExpression(AExpression value) {

                // !Split [ STRING, VALUE ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Items.Count != 2) {
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

            AExpression ConvertToSubFunctionExpression(AExpression value) {

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
                    if(parameterList.Items.Count != 2) {
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

            AExpression ConvertToTransformFunctionExpression(AExpression value) {

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
                    ObjectExpression parametersMap = null;
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

            AExpression ConvertToRefFunctionExpression(AExpression value) {

                // !Ref STRING
                if(value is LiteralExpression refLiteral) {
                    return new ReferenceFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        ReferenceName = new LiteralExpression {
                            SourceLocation = value.SourceLocation,
                            Value = refLiteral.Value
                        }
                    };
                }
                Log(Error.FunctionInvalidParameter("!Ref"), value.SourceLocation);
                return null;
            }

            AExpression ConvertToEqualsConditionExpression(AExpression value) {

                // !Equals [ VALUE, VALUE ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 2) {
                        Log(Error.FunctionExpectsTwoParameters("!Equals"), value.SourceLocation);
                        return null;
                    }
                    return new EqualsConditionExpression {
                        SourceLocation = value.SourceLocation,
                        LeftValue = parameterList[0],
                        RightValue = parameterList[1]
                    };
                }
                Log(Error.FunctionInvalidParameter("!Equals"), value.SourceLocation);
                return null;
            }

            AExpression ConvertToNotConditionExpression(AExpression value) {

                // !Not [ CONDITION ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 1) {
                        Log(Error.FunctionExpectsOneParameter("!Not"), value.SourceLocation);
                        return null;
                    }
                    return new NotConditionExpression {
                        SourceLocation = value.SourceLocation,
                        Value = parameterList[0]
                    };
                }
                Log(Error.FunctionInvalidParameter("!Not"), value.SourceLocation);
                return null;
            }

            AExpression ConvertToAndConditionExpression(AExpression value) {

                // !And [ CONDITION, CONDITION ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 2) {
                        Log(Error.FunctionExpectsTwoParameters("!And"), value.SourceLocation);
                        return null;
                    }
                    return new AndConditionExpression {
                        SourceLocation = value.SourceLocation,
                        LeftValue = parameterList[0],
                        RightValue = parameterList[1]
                    };
                }
                Log(Error.FunctionInvalidParameter("!And"), value.SourceLocation);
                return null;
            }

            AExpression ConvertToOrConditionExpression(AExpression value) {

                // !Or [ CONDITION, CONDITION ]
                if(value is ListExpression parameterList) {
                    if(parameterList.Count != 2) {
                        Log(Error.FunctionExpectsTwoParameters("!Or"), value.SourceLocation);
                        return null;
                    }
                    return new OrConditionExpression {
                        SourceLocation = value.SourceLocation,
                        LeftValue = parameterList[0],
                        RightValue = parameterList[1]
                    };
                }
                Log(Error.FunctionInvalidParameter("!Or"), value.SourceLocation);
                return null;
            }

            AExpression ConvertToConditionRefExpression(AExpression value) {

                // !Condition STRING
                if(value is LiteralExpression conditionLiteral) {
                    return new ConditionExpression {
                        SourceLocation = value.SourceLocation,
                        ReferenceName = new LiteralExpression {
                            SourceLocation = value.SourceLocation,
                            Value = conditionLiteral.Value
                        }
                    };
                }
                Log(Error.FunctionInvalidParameter("!Condition"), value.SourceLocation);
                return null;
            }
        }

        public List<LiteralExpression> ParseListOfLiteralExpressions() {
            var result = new List<LiteralExpression>();

            // attempt to parse a single scalar
            if(IsEvent<Scalar>(out var scalar, out var filePath)) {
                MoveNext();
                result.Add(new LiteralExpression {
                    SourceLocation = Location(filePath, scalar),
                    Value = scalar.Value
                });
                return result;
            }

            // check if we have a sequence instead
            if(!IsEvent<SequenceStart>(out var sequenceStart, out var _)) {
                Log(Error.ExpectedListExpression, Location());
                SkipThisAndNestedEvents();
                return null;
            }
            MoveNext();

            // parse values in sequence
            while(!IsEvent<SequenceEnd>(out var _, out var _)) {
                var item = ParseExpressionOfType<LiteralExpression>(Error.ExpectedLiteralValue);
                if(item != null) {
                    result.Add(item);
                }
            }
            MoveNext();
            return result;
        }

        private void Log(Error error, SourceLocation location) => _provider.Log(error, location);

        private SourceLocation Location() {
            var current = Current;
            return Location(current.FilePath, current.ParsingEvent, current.ParsingEvent);
        }

        private SourceLocation Location((string FilePath, ParsingEvent ParsingEvent) current) => Location(current.FilePath, current.ParsingEvent, current.ParsingEvent);

        private SourceLocation Location(string filePath, ParsingEvent parsingEvent) => Location(filePath, parsingEvent, parsingEvent);

        private SourceLocation Location(string filePath, ParsingEvent startParsingEvent, ParsingEvent stopParsingEvent) => new SourceLocation {
            FilePath = filePath,
            LineNumberStart = startParsingEvent.Start.Line,
            ColumnNumberStart = startParsingEvent.Start.Column,
            LineNumberEnd = stopParsingEvent.End.Line,
            ColumnNumberEnd = stopParsingEvent.End.Column
        };

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
                    syntaxes = new Dictionary<string, SyntaxInfo> {
                        [syntax.Keyword] = syntax
                    };
                }
                _syntaxCache[type] = syntaxes;
            }
            return syntaxes;
        }

        private bool TryParse(Type type, out object result) {
            if(_typeParsers.TryGetValue(type, out var parser)) {
                result = parser();
                return result != null;
            }
            Log(Error.MissingParserDefinition(type.Name), Location());
            result = null;
            SkipThisAndNestedEvents();
            return false;
        }

        private bool IsEvent<T>(out T parsingEvent, out string filePath) where T : ParsingEvent {
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

        private void SkipThisAndNestedEvents() {
            var current = Current.ParsingEvent;
            MoveNext();
            switch(current) {
            case Scalar _:

                // nothing to do
                break;
            case MappingStart _:
                while(!IsEvent<MappingEnd>(out var _, out var _)) {

                    // read key
                    Expect<Scalar>();

                    // read value
                    SkipThisAndNestedEvents();
                }
                break;
            case SequenceStart _:
                while(!IsEvent<SequenceEnd>(out var _, out var _)) {

                    // read value
                    SkipThisAndNestedEvents();
                }
                break;
            default:

                // TODO: better exception
                throw new ApplicationException($"Unexpected parsing event {Current.ParsingEvent.GetType().Name ?? "<null>"}");
            }
        }

        private (string FilePath, T ParsingEvent) Expect<T>() where T : ParsingEvent {
            if(!IsEvent<T>(out var parsingEvent, out var filePath)) {

                // TODO: better exception
                throw new ApplicationException($"Expected parsing event {typeof(T).Name} instead of {Current.ParsingEvent.GetType().Name ?? "<null>"}");
            }
            MoveNext();
            return (filePath, parsingEvent);
        }
    }
}