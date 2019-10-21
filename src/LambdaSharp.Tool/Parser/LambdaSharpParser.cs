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
        private readonly Dictionary<Type, Func<ANode>> _typeToParsers;
        private Dictionary<string, SyntaxInfo> _itemTypeSyntaxes;
        private readonly Dictionary<Type, SyntaxInfo> _typeToSyntax = new Dictionary<Type, SyntaxInfo>();

        //--- Constructors ---
        public LambdaSharpParser(string filePath, string source) {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _parser = new YamlDotNet.Core.Parser(new StringReader(source));
            _messages = new List<string>();
            _typeToParsers = new Dictionary<Type, Func<ANode>> {
                [typeof(StringLiteral)] = ParseStringLiteral,
                [typeof(IntLiteral)] = ParseIntLiteral,
                [typeof(BoolLiteral)] = ParseBoolLiteral,
                [typeof(DeclarationList<AItemDeclaration>)] = ParseAItemDeclarationList,

                // TODO:
                [typeof(DeclarationList<PragmaExpression>)] = ParseAnything,
                [typeof(DeclarationList<StringLiteral>)] = ParseAnything,
                [typeof(DeclarationList<UsingDeclaration>)] = ParseAnything,
                [typeof(AValueExpression)] = ParseAnything
            };
            _itemTypeSyntaxes = typeof(AItemDeclaration).Assembly.GetTypes()
                .Where(type => type.BaseType == typeof(AItemDeclaration))
                .Select(type => GetSyntax(type))
                .ToDictionary(syntax => syntax.Keyword, syntax => syntax);
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

        public T ParseDeclaration<T>() where T : ADeclaration, new() {
            var syntax = GetSyntax(typeof(T));

            // ensure first event is the beginning of a map
            if(!(_parser.Current is MappingStart mappingStart) || (mappingStart.Tag != null)) {
                LogError("expected a map", Location());
                _parser.SkipThisAndNestedEvents();
                return null;
            }
            _parser.MoveNext();
            var result = new T();

            // parse mapping
            var foundKeys = new HashSet<string>();
            var mandatoryKeys = new HashSet<string>(syntax.MandatoryKeys);
            while(!(_parser.Current is MappingEnd)) {

                // parse key
                var keyScalar = _parser.Expect<Scalar>();
                var key = keyScalar.Value;
                if(syntax.Keys.TryGetValue(key, out var keyProperty)) {

                    // check if the first key is being parsed
                    if((syntax.Keyword != null) && !foundKeys.Any() && (key != syntax.Keyword)) {
                        LogError($"expected key '{syntax.Keyword}', but found '{key}'", Location(keyScalar));
                    }

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
                    if(_typeToParsers.TryGetValue(keyProperty.PropertyType, out var parser)) {
                        keyProperty.SetValue(result, parser());
                    } else {
                        LogError($"no parser defined for '{key}'", Location(_parser.Current));
                        _parser.SkipThisAndNestedEvents();
                    }
                } else {
                    LogError($"unexpected key '{key}'", Location(keyScalar));

                    // no need to parse an invalid key
                    _parser.SkipThisAndNestedEvents();
                }
            }

            // check for missing mandatory keys
            if(mandatoryKeys.Any()) {
                LogError($"missing keys: {string.Join(", ", mandatoryKeys.OrderBy(key => key))}", Location(mappingStart));
            }
            result.SourceLocation = Location(mappingStart, _parser.Current);
            _parser.MoveNext();
            return result;
        }

        public DeclarationList<AItemDeclaration> ParseAItemDeclarationList() {
            if(!(_parser.Current is SequenceStart sequenceStart) || (sequenceStart.Tag != null)) {
                LogError("expected a sequence", Location());
                _parser.SkipThisAndNestedEvents();
                return null;
            }
            _parser.MoveNext();

            // parse declaration items in sequence
            var result = new DeclarationList<AItemDeclaration>();
            while(!(_parser.Current is SequenceEnd)) {
                var item = ParseAItemDeclaration();
                if(item != null) {
                    result.Items.Add(item);
                }
            }
            _parser.MoveNext();
            return result;
        }

        public AItemDeclaration ParseAItemDeclaration() {

            // ensure first event is the beginning of a map
            if(!(_parser.Current is MappingStart mappingStart) || (mappingStart.Tag != null)) {
                LogError("expected a map", Location());
                _parser.SkipThisAndNestedEvents();
                return null;
            }
            _parser.MoveNext();
            AItemDeclaration result = null;

            // parse mapping
            var foundKeys = new HashSet<string>();
            HashSet<string> mandatoryKeys = null;
            SyntaxInfo syntax = null;
            while(!(_parser.Current is MappingEnd)) {

                // parse key
                var keyScalar = _parser.Expect<Scalar>();
                var key = keyScalar.Value;

                // check if this is the first key being parsed
                if(syntax == null) {
                    if(_itemTypeSyntaxes.TryGetValue(key, out syntax)) {
                        result = (AItemDeclaration)Activator.CreateInstance(syntax.Type);
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
                    if(_typeToParsers.TryGetValue(keyProperty.PropertyType, out var parser)) {
                        keyProperty.SetValue(result, parser());
                    } else {
                        LogError($"no parser defined for '{key}' -> {keyProperty.PropertyType.Name}", Location(_parser.Current));
                        _parser.SkipThisAndNestedEvents();
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

        public StringLiteral ParseStringLiteral() {
            if((_parser.Current is Scalar scalar) && (scalar.Tag == null)) {
                _parser.MoveNext();
                return new StringLiteral {
                    SourceLocation = Location(scalar),
                    Value = scalar.Value
                };
            }
            LogError("expected a literal string", Location());
            _parser.SkipThisAndNestedEvents();
            return null;
        }

        public IntLiteral ParseIntLiteral() {
            if((_parser.Current is Scalar scalar) && (scalar.Tag == null) && int.TryParse(scalar.Value, out var value)) {
                _parser.MoveNext();
                return new IntLiteral {
                    SourceLocation = Location(scalar),
                    Value = value
                };
            }
            LogError("expected a literal integer", Location());
            _parser.SkipThisAndNestedEvents();
            return null;
        }

        public BoolLiteral ParseBoolLiteral() {
            if((_parser.Current is Scalar scalar) && (scalar.Tag == null) && bool.TryParse(scalar.Value, out var value)) {
                _parser.MoveNext();
                return new BoolLiteral {
                    SourceLocation = Location(scalar),
                    Value = value
                };
            }
            LogError("expected a literal boolean", Location());
            _parser.SkipThisAndNestedEvents();
            return null;
        }

        public ANode ParseAnything() {
            _parser.SkipThisAndNestedEvents();
            return null;
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
    }
}