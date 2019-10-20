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

    public class LambdaSharpParser {

        //--- Fields ---
        private readonly string _filePath;
        private readonly IParser _parser;
        private readonly List<string> _messages;
        private readonly Dictionary<Type, Func<ANode>> _typeToParsers;
        private int _currentLine;
        private int _currentColumn;

        //--- Constructors ---
        public LambdaSharpParser(string filePath, string source) {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _parser = new YamlDotNet.Core.Parser(new StringReader(source));
            _messages = new List<string>();
            _typeToParsers = new Dictionary<Type, Func<ANode>> {
                [typeof(StringLiteral)] = ParseStringLiteral,
                [typeof(DeclarationList<PragmaExpression>)] = ParseAnything,
                [typeof(DeclarationList<StringLiteral>)] = ParseAnything,
                [typeof(DeclarationList<UsingDeclaration>)] = ParseAnything,
                [typeof(DeclarationList<AItemDeclaration>)] = ParseAnything
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

        public T ParseDeclaration<T>() where T : ADeclaration, new() {

            // NOTE: extract syntax poproperties from type to identify what keys are expected and required;
            //  in addition, one key can be called out as the keyword, which means it must be the first key
            //  to appear in the mapping.
            var syntaxProperties = typeof(T).GetProperties()
                .Select(property => new {
                    Syntax = property.GetCustomAttributes<ASyntaxAttribute>().FirstOrDefault(),
                    Property = property
                })
                .Where(tuple => tuple.Syntax != null)
                .ToList();
            var mandatoryKeys = new HashSet<string>(
                syntaxProperties
                    .Where(tuple => tuple.Syntax.Type != SyntaxType.Optional)
                    .Select(tuple => tuple.Property.Name)
            );
            var optionalKeyword = syntaxProperties.FirstOrDefault(tuple => tuple.Syntax.Type == SyntaxType.Keyword);

            // ensure first event is the beginning of a map
            if(!(_parser.Current is MappingStart mappingStart) || (mappingStart.Tag != null)) {
                LogError("expected a map", Location());
                _parser.SkipThisAndNestedEvents();
                return null;
            }
            var result = new T();
            _parser.MoveNext();

            // parse mapping
            var foundKeys = new HashSet<string>();
            while(!(_parser.Current is MappingEnd)) {

                // parse key
                var keyScalar = _parser.Expect<Scalar>();
                var key = keyScalar.Value;
                var matchingSyntax = syntaxProperties.FirstOrDefault(tuple => tuple.Property.Name == key);
                if(matchingSyntax != null) {

                    // check if key is a duplicate
                    if(!foundKeys.Add(key)) {
                        LogError($"duplicate key '{key}'", Location(keyScalar));

                        // no need to parse the value for the duplicate key
                        _parser.SkipThisAndNestedEvents();
                        continue;
                    }

                    // check if the first key is being parsed
                    if((optionalKeyword != null) && (optionalKeyword.Property.Name != key)) {
                        LogError($"expected key '{optionalKeyword.Property.Name}', but found '{key}'", Location(keyScalar));
                    }
                    optionalKeyword = null;

                    // remove key from mandatory keys
                    mandatoryKeys.Remove(key);

                    // find type appropriate parser and set target property with the parser outcome
                    if(_typeToParsers.TryGetValue(matchingSyntax.Property.PropertyType, out var parser)) {
                        matchingSyntax.Property.SetValue(result, parser());
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
    }
}