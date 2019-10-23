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
using System.IO;
using System.Linq;
using System.Reflection;
using LambdaSharp.Tool.Parser.Syntax;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace LambdaSharp.Tool.Parser {

    public sealed class LambdaSharpParser {

        //--- Types ---
        private class SyntaxInfo {

            //--- Constructors ---
            public SyntaxInfo(Type type) {

                // NOTE: extract syntax poproperties from type to identify what keys are expected and required;
                //  in addition, one key can be called out as the keyword, which means it must be the first key
                //  to appear in the mapping.
                var syntaxProperties = type.GetProperties()
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
        private readonly string _filePath;
        private readonly IParser _parser;
        private readonly List<string> _messages;
        private readonly Dictionary<Type, Func<object>> _typeParsers;
        private readonly Dictionary<Type, SyntaxInfo> _typeToSyntax = new Dictionary<Type, SyntaxInfo>();
        private readonly Dictionary<Type, Dictionary<string, SyntaxInfo>> _syntaxCache = new Dictionary<Type, Dictionary<string, SyntaxInfo>>();

        //--- Constructors ---
        public LambdaSharpParser(string filePath, string source) {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _parser = new YamlDotNet.Core.Parser(new StringReader(source));
            _messages = new List<string>();
            _typeParsers = new Dictionary<Type, Func<object>> {

                // expressions
                [typeof(AValueExpression)] = () => ParseExpression(),
                [typeof(ObjectExpression)] = () => ParseExpressionOf<ObjectExpression>("map expression"),
                [typeof(ListExpression)] = () => ParseExpressionOf<ListExpression>("list expression"),
                [typeof(LiteralExpression)] = () => ParseExpressionOf<LiteralExpression>("literal expression"),

                // TODO:
                [typeof(AConditionExpression)] = () => throw new NotImplementedException("AConditionExpression"),

                // declarations
                [typeof(ModuleDeclaration)] = () => ParseDeclarationOf<ModuleDeclaration>(),
                [typeof(UsingDeclaration)] = () => ParseDeclarationOf<UsingDeclaration>(),
                [typeof(FunctionVpcDeclaration)] = () => ParseDeclarationOf<FunctionVpcDeclaration>(),
                [typeof(ResourcePropertyTypeDeclaration)] = () => ParseDeclarationOf<ResourcePropertyTypeDeclaration>(),
                [typeof(ResourceAttributeTypeDeclaration)] = () => ParseDeclarationOf<ResourceAttributeTypeDeclaration>(),
                [typeof(AItemDeclaration)] = () => ParseDeclarationOf<AItemDeclaration>(),
                [typeof(AEventSourceDeclaration)] = () => ParseDeclarationOf<AEventSourceDeclaration>(),

                // lists
                [typeof(List<AItemDeclaration>)] = () => ParseList<AItemDeclaration>(),
                [typeof(List<AValueExpression>)] = () => ParseList<AValueExpression>(),
                [typeof(List<AEventSourceDeclaration>)] = () => ParseList<AEventSourceDeclaration>(),
                [typeof(List<UsingDeclaration>)] = () => ParseList<UsingDeclaration>(),
                [typeof(List<LiteralExpression>)] = () => ParseListOfLiteralExpressions()
            };
        }

        //--- Properties ---
        public IEnumerable<string> Messages => _messages;

        //--- Methods ---
        public void Start() {
            _parser.Expect<StreamStart>();
            _parser.Expect<DocumentStart>();
        }

        public void End() {
            _parser.Expect<DocumentEnd>();
            _parser.Expect<StreamEnd>();
        }

        public List<T> ParseList<T>() where T : ASyntaxNode {
            if(!(_parser.Current is SequenceStart sequenceStart) || (sequenceStart.Tag != null)) {
                LogError("expected a sequence", Location());
                _parser.SkipThisAndNestedEvents();
                return null;
            }
            _parser.MoveNext();

            // parse declaration items in sequence
            var result = new List<T>();
            while(!(_parser.Current is SequenceEnd)) {
                if(TryParse(typeof(T), out var item)) {

                    // TODO: maybe we can do better than having a cast exception here in case something goes wrong?
                    result.Add((T)item);
                }
            }
            _parser.MoveNext();
            return result;
        }

        public T ParseDeclarationOf<T>() where T : ADeclaration {

            // fetch all possible syntax options for specified type
            var syntaxes = GetSyntaxes(typeof(T));

            // ensure first event is the beginning of a map
            if(!(_parser.Current is MappingStart mappingStart) || (mappingStart.Tag != null)) {
                LogError("expected a map", Location());
                _parser.SkipThisAndNestedEvents();
                return null;
            }
            _parser.MoveNext();
            T result = null;

            // parse mappings
            var foundKeys = new HashSet<string>();
            HashSet<string> mandatoryKeys = null;
            SyntaxInfo syntax = null;
            while(!(_parser.Current is MappingEnd)) {

                // parse key
                var keyScalar = _parser.Expect<Scalar>();
                var key = keyScalar.Value;

                // check if this is the first key being parsed
                if(syntax == null) {
                    if(syntaxes.TryGetValue(key, out syntax)) {
                        result = (T)Activator.CreateInstance(syntax.Type);
                        mandatoryKeys = new HashSet<string>(syntax.MandatoryKeys);
                    } else {
                        LogError($"unexpected item keyword '{key}'", Location(keyScalar));

                        // skip the value of the key
                        _parser.SkipThisAndNestedEvents();

                        // skip all remaining key-value pairs
                        while(!(_parser.Current is MappingEnd)) {

                            // skip key
                            _parser.Expect<Scalar>();

                            // skip value
                            _parser.SkipThisAndNestedEvents();
                        }
                        return null;
                    }
                }

                // map read key to syntax property
                if(syntax.Keys.TryGetValue(key, out var keyProperty)) {

                    // check if key is a duplicate
                    if(!foundKeys.Add(key)) {
                        LogError($"duplicate key '{key}'", Location(keyScalar));

                        // no need to parse the value for the duplicate key
                        _parser.SkipThisAndNestedEvents();
                        continue;
                    }

                    // remove key from mandatory keys
                    mandatoryKeys.Remove(key);

                    // find type appropriate parser and set target property with the parser outcome
                    if(TryParse(keyProperty.PropertyType, out var value)) {
                        keyProperty.SetValue(result, value);
                    }
                } else {
                    LogError($"unexpected key '{key}'", Location(keyScalar));

                    // no need to parse an invalid key
                    _parser.SkipThisAndNestedEvents();
                }
            }

            // check for missing mandatory keys
            if(mandatoryKeys?.Any() ?? false) {
                LogError($"missing keys: {string.Join(", ", mandatoryKeys.OrderBy(key => key))}", Location(mappingStart));
            }
            if(result != null) {
                result.SourceLocation = Location(mappingStart, _parser.Current);
            }
            _parser.MoveNext();
            return result;
        }

        public T ParseExpressionOf<T>(string constraint) where T : AValueExpression {
            var result = ParseExpression();
            if(!(result is T expression)) {
                var isVowel = ("aeiouAEIOU".IndexOf(constraint.First()) >= 0);
                LogError($"expected {(isVowel ? "an" : "a")} {constraint}", result.SourceLocation);
                return null;
            }
            return expression;
        }

        public AValueExpression ParseExpression() {
            switch(_parser.Current) {
            case SequenceStart sequenceStart:
                return ConvertFunction(sequenceStart.Tag, ParseListExpression());
            case MappingStart mappingStart:
                return ConvertFunction(mappingStart.Tag, ParseObjectExpression());
            case Scalar scalar:
                return ConvertFunction(scalar.Tag, ParseLiteralExpression());
            default:
                LogError("expected a map, sequence, or literal", Location());
                _parser.SkipThisAndNestedEvents();
                return null;
            }

            // local functions
            AValueExpression ParseObjectExpression() {
                if(!(_parser.Current is MappingStart mappingStart)) {
                    LogError("expected a map", Location());
                    _parser.SkipThisAndNestedEvents();
                    return null;
                }
                _parser.MoveNext();

                // parse declaration items in sequence
                var result = new ObjectExpression();
                while(!(_parser.Current is MappingEnd)) {

                    // parse key
                    var keyScalar = _parser.Expect<Scalar>();
                    if(result.Values.ContainsKey(keyScalar.Value)) {
                        LogError($"duplicate key '{keyScalar.Value}'", Location(keyScalar));
                        _parser.SkipThisAndNestedEvents();
                        continue;
                    }

                    // parse value
                    var value = ParseExpression();
                    if(value == null) {
                        continue;
                    }

                    // add key-value pair
                    result.Keys.Add(new LiteralExpression {
                        SourceLocation = Location(keyScalar),
                        Value = keyScalar.Value
                    });
                    result.Values.Add(keyScalar.Value, value);
                }
                result.SourceLocation = Location(mappingStart, _parser.Current);
                _parser.MoveNext();
                return result;
            }

            AValueExpression ParseListExpression() {
                if(!(_parser.Current is SequenceStart sequenceStart)) {
                    LogError("expected a sequence", Location());
                    _parser.SkipThisAndNestedEvents();
                    return null;
                }
                _parser.MoveNext();

                // parse values in sequence
                var result = new ListExpression();
                while(!(_parser.Current is SequenceEnd)) {
                    var item = ParseExpression();
                    if(item != null) {
                        result.Values.Add(item);
                    }
                }
                result.SourceLocation = Location(sequenceStart, _parser.Current);
                _parser.MoveNext();
                return result;
            }

            AValueExpression ParseLiteralExpression() {
                if(!(_parser.Current is Scalar scalar)) {
                    LogError("expected a literal string", Location());
                    _parser.SkipThisAndNestedEvents();
                    return null;
                }
                _parser.MoveNext();

                // parse values in sequence
                return new LiteralExpression {
                    SourceLocation = Location(scalar),
                    Value = scalar.Value
                };
            }

            AValueExpression ConvertFunction(string tag, AValueExpression value) {

                // check if value is a long-form function
                if((value is ObjectExpression objectExpression) && (objectExpression.Values.Count == 1)) {
                    var kv = objectExpression.Values.First();
                    switch(kv.Key) {
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
                default:
                    LogError($"unknown tag '{tag}'", value.SourceLocation);
                    return null;
                }
            }

            AValueExpression ConvertToBase64FunctionExpression(AValueExpression value) {

                // !Base64 VALUE
                return new Base64FunctionExpression {
                    SourceLocation = value.SourceLocation,
                    Value = value
                };
            }

            AValueExpression ConvertToCidrFunctionExpression(AValueExpression value) {

                // !Cidr [VALUE, VALUE, VALUE]
                if((value is ListExpression cidrList) && (cidrList.Values.Count == 3)) {
                    return new CidrFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        IpBlock = cidrList.Values[0],
                        Count = cidrList.Values[1],
                        CidrBits = cidrList.Values[2]
                    };
                }
                LogError($"invalid parameters for !Cidr function", value.SourceLocation);
                return null;
            }

            AValueExpression ConvertToFindInMapFunctionExpression(AValueExpression value) {

                // !FindInMap [ VALUE, VALUE, VALUE ]
                if((value is ListExpression findInMapList) && (findInMapList.Values.Count == 3)) {
                    return new FindInMapExpression {
                        SourceLocation = value.SourceLocation,
                        MapName = findInMapList.Values[0],
                        TopLevelKey = findInMapList.Values[1],
                        SecondLevelKey = findInMapList.Values[2]
                    };
                }
                LogError($"invalid parameters for !FindInMap function", value.SourceLocation);
                return null;
            }

            AValueExpression ConvertToGetAttFunctionExpression(AValueExpression value) {

                // !GetAtt STRING
                if(value is LiteralExpression getAttLiteral) {
                    var referenceAndAttribute = getAttLiteral.Value.Split('.', 2);
                    return new GetAttFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        ResourceName = new LiteralExpression {
                            SourceLocation = getAttLiteral.SourceLocation,
                            Value = referenceAndAttribute[0]
                        },
                        AttributeName = new LiteralExpression {
                            SourceLocation = getAttLiteral.SourceLocation,
                            Value = (referenceAndAttribute.Length == 2) ? referenceAndAttribute[1] : ""
                        }
                    };
                }

                // !GetAtt [ STRING, VALUE ]
                if(value is ListExpression getAttList) {
                    if(getAttList.Values.Count != 2) {
                        LogError("!GetAtt expects 2 parameters", value.SourceLocation);
                        return null;
                    }
                    if(!(getAttList.Values[0] is LiteralExpression resourceNameLiteral)) {
                        LogError("!GetAtt first parameter must be a literal value", getAttList.Values[0].SourceLocation);
                        return null;
                    }
                    return new GetAttFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        ResourceName = resourceNameLiteral,
                        AttributeName = getAttList.Values[2]
                    };
                }
                LogError($"invalid parameters for !GetAtt function", value.SourceLocation);
                return null;
            }

            AValueExpression ConvertToGetAZsFunctionExpression(AValueExpression value) {

                // !GetAZs VALUE
                return new GetAZsFunctionExpression {
                    SourceLocation = value.SourceLocation,
                    Region = value
                };
            }

            AValueExpression ConvertToIfFunctionExpression(AValueExpression value) {

                // !If [ STRING, VALUE, VALUE ]
                if(value is ListExpression ifList) {
                    if(ifList.Values.Count != 3) {
                        LogError("!If expects 3 parameters", value.SourceLocation);
                        return null;
                    }
                    if(!(ifList.Values[0] is LiteralExpression conditionNameLiteral)) {
                        LogError("!If first parameter must be a literal value", ifList.Values[0].SourceLocation);
                        return null;
                    }
                    return new IfFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        ConditionName = new ConditionNameLiteralExpression {
                            SourceLocation = conditionNameLiteral.SourceLocation,
                            Name = conditionNameLiteral.Value
                        },
                        IfTrue = ifList.Values[1],
                        IfFalse = ifList.Values[2]
                    };
                }
                LogError($"invalid parameters for !If function", value.SourceLocation);
                return null;
            }

            AValueExpression ConvertToImportValueFunctionExpression(AValueExpression value) {

                // !ImportValue VALUE
                return new ImportValueFunctionExpression {
                    SourceLocation = value.SourceLocation,
                    SharedValueToImport = value
                };
            }

            AValueExpression ConvertToJoinFunctionExpression(AValueExpression value) {

                // !Join [ STRING, [ VALUE, ... ]]
                if(value is ListExpression joinList) {
                    if(joinList.Values.Count != 2) {
                        LogError("!Join expects 2 parameters", value.SourceLocation);
                        return null;
                    }
                    if(!(joinList.Values[0] is LiteralExpression separatorLiteral)) {
                        LogError("!Join first parameter must be a literal value", joinList.Values[0].SourceLocation);
                        return null;
                    }
                    return new JoinFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        Separator = separatorLiteral,
                        Values = joinList.Values[1]
                    };
                }
                LogError($"invalid parameters for !Join function", value.SourceLocation);
                return null;
            }

            AValueExpression ConvertToSelectFunctionExpression(AValueExpression value) {

                // !Select [ VALUE, [ VALUE, ... ]]
                if(value is ListExpression selectList) {
                    if(selectList.Values.Count != 2) {
                        LogError("!Select expects 2 parameters", value.SourceLocation);
                        return null;
                    }
                    return new SelectFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        Index = selectList.Values[0],
                        Values = selectList.Values[1]
                    };
                }
                LogError($"invalid parameters for !Select function", value.SourceLocation);
                return null;
            }

            AValueExpression ConvertToSplitFunctionExpression(AValueExpression value) {

                // !Split [ STRING, VALUE ]
                if(value is ListExpression splitList) {
                    if(splitList.Values.Count != 2) {
                        LogError("!Split expects 2 parameters", value.SourceLocation);
                        return null;
                    }
                    if(!(splitList.Values[0] is LiteralExpression indexLiteral)) {
                        LogError("!Split first parameter must be a literal value", splitList.Values[0].SourceLocation);
                        return null;
                    }
                    return new SplitFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        Delimiter = indexLiteral,
                        SourceString = splitList.Values[1]
                    };
                }
                LogError($"invalid parameters for !Split function", value.SourceLocation);
                return null;
            }

            AValueExpression ConvertToSubFunctionExpression(AValueExpression value) {

                // !Sub STRING
                if(value is LiteralExpression subLiteral) {
                    return new SubFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        FormatString = subLiteral,
                        Parameters = null
                    };
                }

                // !Sub [ STRING, { KEY: VALUE, ... }]
                if(value is ListExpression subList) {
                    if(subList.Values.Count != 2) {
                        LogError("!Sub expects 2 parameters", value.SourceLocation);
                        return null;
                    }
                    if(!(subList.Values[0] is LiteralExpression formatLiteralExpression)) {
                        LogError("!Sub first parameter must be a literal value", subList.Values[0].SourceLocation);
                        return null;
                    }
                    if(!(subList.Values[1] is ObjectExpression parametersObject)) {
                        LogError("!Sub second parameter must be a map", subList.Values[1].SourceLocation);
                        return null;
                    }
                    return new SubFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        FormatString = formatLiteralExpression,
                        Parameters = parametersObject
                    };
                }
                LogError($"invalid parameters for !Sub function", value.SourceLocation);
                return null;
            }

            AValueExpression ConvertToTransformFunctionExpression(AValueExpression value) {

                // !Transform { Name: STRING, Parameters: { KEY: VALUE, ... } }
                if(value is ObjectExpression transformMap) {
                    if(!transformMap.Values.TryGetValue("Name", out var macroNameExpression)) {
                        LogError("!Transform missing 'Name'", value.SourceLocation);
                        return null;
                    }
                    if(!(macroNameExpression is LiteralExpression macroNameLiteral)) {
                        LogError("!Transform 'Name' must be a literal value", macroNameExpression.SourceLocation);
                        return null;
                    }
                    if(!transformMap.Values.TryGetValue("Parameters", out var parametersExpression)) {
                        LogError("!Transform missing 'Parameters'", value.SourceLocation);
                        return null;
                    }
                    if(!(parametersExpression is ObjectExpression parametersMap)) {
                        LogError("!Transform 'Parameters' must be a map", parametersExpression.SourceLocation);
                        return null;
                    }
                    return new TransformFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        MacroName = macroNameLiteral,
                        Parameters = parametersMap
                    };
                }
                LogError($"invalid parameters for !Transform function", value.SourceLocation);
                return null;
            }

            AValueExpression ConvertToRefFunctionExpression(AValueExpression value) {

                // !Ref STRING
                if(value is LiteralExpression refLiteral) {
                    return new ReferenceFunctionExpression {
                        SourceLocation = value.SourceLocation,
                        ResourceName = refLiteral
                    };
                }
                LogError($"invalid parameters for !Ref function", value.SourceLocation);
                return null;
            }
        }

        public List<LiteralExpression> ParseListOfLiteralExpressions() {
            var result = new List<LiteralExpression>();

            // attempt to parse a single scalar
            if(_parser.Current is Scalar scalar) {
                _parser.MoveNext();
                result.Add(new LiteralExpression {
                    SourceLocation = Location(scalar),
                    Value = scalar.Value
                });
                return result;
            }

            // check if we have a sequence instead
            if(!(_parser.Current is SequenceStart sequenceStart)) {
                LogError("expected a sequence", Location());
                _parser.SkipThisAndNestedEvents();
                return null;
            }
            _parser.MoveNext();

            // parse values in sequence
            while(!(_parser.Current is SequenceEnd)) {
                var item = ParseExpressionOf<LiteralExpression>("literal expression");
                if(item != null) {
                    result.Add(item);
                }
            }
            _parser.MoveNext();
            return result;
        }

        private void LogError(string message, SourceLocation location) {
            _messages.Add($"ERROR: {message} @ {location.FilePath}({location.LineNumberStart},{location.ColumnNumberStart})");
        }

        private SourceLocation Location() => Location(_parser.Current);

        private SourceLocation Location(ParsingEvent parsingEvent) => Location(parsingEvent, parsingEvent);

        private SourceLocation Location(ParsingEvent startParsingEvent, ParsingEvent stopParsingEvent) => new SourceLocation {
            FilePath = _filePath,
            LineNumberStart = startParsingEvent.Start.Line,
            ColumnNumberStart = startParsingEvent.Start.Column,
            LineNumberEnd = stopParsingEvent.End.Line,
            ColumnNumberEnd = stopParsingEvent.End.Column
        };

        private SyntaxInfo GetSyntax(Type type) {
            if(!_typeToSyntax.TryGetValue(type, out var result)) {
                result = new SyntaxInfo(type);
                _typeToSyntax[type] = result;
            }
            return result;
        }

        private Dictionary<string, SyntaxInfo> GetSyntaxes(Type type) {
            if(!_syntaxCache.TryGetValue(type, out var syntaxes)) {
                if(type.IsAbstract) {

                    // for abstract types, we build a list derived types
                    syntaxes = typeof(AItemDeclaration).Assembly.GetTypes()
                        .Where(definedType => definedType.BaseType == typeof(AItemDeclaration))
                        .Select(definedType => GetSyntax(definedType))
                        .ToDictionary(syntax => syntax.Keyword, syntax => syntax);
                } else {

                    // for conrete types, we use the type itself
                    var syntax = GetSyntax(type);
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
            LogError($"no parser defined for type {type.Name}", Location(_parser.Current));
            result = null;
            _parser.SkipThisAndNestedEvents();
            return false;
        }
    }
}