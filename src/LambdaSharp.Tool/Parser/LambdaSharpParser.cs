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
using LambdaSharp.Tool.Parser.Syntax;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace LambdaSharp.Tool.Parser {

    public class LambdaSharpParser {

        //--- Fields ---
        private readonly string _filePath;
        private readonly IParser _parser;
        private readonly List<string> _messages = new List<string>();

        //--- Constructors ---
        public LambdaSharpParser(string filePath, string source) {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _parser = new YamlDotNet.Core.Parser(new StringReader(source));
        }

        //--- Properties ---
        public IEnumerable<string> Messages => _messages;

        //--- Methods ---
        public void Parse() {
            _parser.Expect<StreamStart>();
            _parser.Expect<DocumentStart>();
            ParseModuleDeclaration();
            _parser.Expect<DocumentEnd>();
            _parser.Expect<StreamEnd>();
        }

        public ModuleDeclaration ParseModuleDeclaration() {
            if((_parser.Current is MappingStart mappingStart) && (mappingStart.Tag == null)) {
                var result = new ModuleDeclaration();
                var start = mappingStart.Start;
                _parser.MoveNext();

                // keys to parse and how to parse them
                var foundKeys = new HashSet<string>();
                var mandatoryKeys = new HashSet<string> {
                    "Module",
                    "Items"
                };
                var validKeys = new Dictionary<string, Action>() {
                    ["Module"] = () => result.Module = ParseStringLiteral(),
                    ["Version"] = () => result.Version = ParseStringLiteral(),
                    ["Description"] = () => result.Version = ParseStringLiteral(),

                    // TODO: add missing parse steps
                    ["Pragmas"] = () => _parser.SkipThisAndNestedEvents(),
                    ["Secrets"] = () => _parser.SkipThisAndNestedEvents(),
                    ["Using"] = () => _parser.SkipThisAndNestedEvents(),
                    ["Items"] = () => _parser.SkipThisAndNestedEvents()
                };
                var firstKey = "Module";

                // TODO: parse remainder of the mapping, checking for duplicate keys
                while(!(_parser.Current is MappingEnd)) {

                    // parse key
                    var key = ((Scalar)_parser.Current).Value;
                    if(validKeys.TryGetValue(key, out var valueParser)) {

                        // check if key is a duplicate
                        if(!foundKeys.Add(key)) {
                            LogError($"duplicate key '{key}'");
                            _parser.MoveNext();

                            // no need to parse a duplicate key
                            _parser.SkipThisAndNestedEvents();
                            continue;
                        }

                        // check if the first key is being parsed
                        if(firstKey != null) {
                            if(firstKey != key) {
                                LogError($"expected key '{firstKey}', but found '{key}'");
                                _parser.MoveNext();
                            }
                            firstKey = null;
                        }

                        // remove key from mandatory keys
                        mandatoryKeys.Remove(key);

                        // parse value
                        _parser.MoveNext();
                        valueParser();
                    } else {
                        LogError($"unexpected key '{key}'");
                        _parser.MoveNext();

                        // no need to parse an invalid key
                        _parser.SkipThisAndNestedEvents();
                    }
                }
                result.SourceLocation = new SourceLocation(_filePath, mappingStart) {
                    LineNumberEnd = _parser.Current.End.Line,
                    ColumnNumberEnd  = _parser.Current.End.Column
                };
                _parser.MoveNext();
                return result;
            }
            LogError("expected a map");
            return null;
        }

        public StringLiteral ParseStringLiteral() {
            if((_parser.Current is Scalar scalar) && (scalar.Tag == null)) {
                _parser.MoveNext();
                return new StringLiteral {
                    SourceLocation = new SourceLocation(_filePath, scalar),
                    Value = scalar.Value
                };
            }
            LogError("expected a literal string");
            _parser.SkipThisAndNestedEvents();
            return null;
        }

        private void LogError(string message) {
            _messages.Add($"ERROR: {message} @ line: {_parser.Current.Start.Line}, column: {_parser.Current.Start.Column}, file: {_filePath}");
        }
    }
}