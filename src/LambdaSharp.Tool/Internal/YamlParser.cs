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
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace LambdaSharp.Tool.Internal {

    public class YamlStream {

        //--- Properties ---
        public StreamStart Start { get; set; }
        public List<YamlDocument> Documents { get; set; }
        public StreamEnd End { get; set; }

        //--- Methods ---
        public void AppendTo(List<ParsingEvent> parsingEvents) {
            parsingEvents.Add(Start);
            foreach(var document in Documents) {
                document.AppendTo(parsingEvents);
            }
            parsingEvents.Add(End);
        }
    }

    public class YamlDocument {

        //--- Properties ---
        public DocumentStart Start { get; set; }
        public List<AYamlValue> Values { get; set; }
        public DocumentEnd End { get; set; }

        //--- Methods ---
        public void AppendTo(List<ParsingEvent> parsingEvents) {
            parsingEvents.Add(Start);
            foreach(var value in Values) {
                value.AppendTo(parsingEvents);
            }
            parsingEvents.Add(End);
        }
    }

    public abstract class AYamlValue {

        //--- Methods ---
        public abstract void AppendTo(List<ParsingEvent> parsingEvents);
    }

    public class YamlMap : AYamlValue {

        //--- Properties ---
        public MappingStart Start { get; set; }
        public List<KeyValuePair<YamlScalar, AYamlValue>> Entries { get; set; }
        public MappingEnd End { get; set; }

        //--- Methods ---
        public override void AppendTo(List<ParsingEvent> parsingEvents) {
            parsingEvents.Add(Start);
            foreach(var entry in Entries) {
                entry.Key.AppendTo(parsingEvents);
                entry.Value.AppendTo(parsingEvents);
            }
            parsingEvents.Add(End);
        }
    }

    public class YamlScalar : AYamlValue {

        //--- Constructors ---
        public YamlScalar() { }

        public YamlScalar(Scalar scalar) {
            Scalar = scalar;
        }

        //--- Properties ---
        public Scalar Scalar { get; set; }

        //--- Methods ---
        public override void AppendTo(List<ParsingEvent> parsingEvents) {
            parsingEvents.Add(Scalar);
        }
    }

    public class YamlAnchorAlias : AYamlValue {

        //--- Constructors ---
        public YamlAnchorAlias() { }

        public YamlAnchorAlias(AnchorAlias anchorAlias) {
            AnchorAlias = anchorAlias;
        }

        //--- Properties ---
        public AnchorAlias AnchorAlias { get; set; }

        //--- Methods ---
        public override void AppendTo(List<ParsingEvent> parsingEvents) {
            parsingEvents.Add(AnchorAlias);
        }
    }

    public class YamlSequence : AYamlValue {

        //--- Properties ---
        public SequenceStart Start { get; set; }
        public List<AYamlValue> Values { get; set; }
        public SequenceEnd End { get; set; }

        //--- Methods ---
        public override void AppendTo(List<ParsingEvent> parsingEvents) {
            parsingEvents.Add(Start);
            foreach(var value in Values) {
                value.AppendTo(parsingEvents);
            }
            parsingEvents.Add(End);
        }
    }

    public class YamlParsingEventsParser : IParser {

        //--- Fields ---
        private readonly IEnumerator<ParsingEvent> _parsingEventsEnumerator;

        //--- Constructors ---
        public YamlParsingEventsParser(IEnumerable<ParsingEvent> parsingEvents) {
            _parsingEventsEnumerator = (parsingEvents ?? throw new ArgumentNullException(nameof(parsingEvents))).GetEnumerator();
        }

        //--- Properties ---
        public ParsingEvent Current => _parsingEventsEnumerator.Current;

        public bool MoveNext() => _parsingEventsEnumerator.MoveNext();
    }

    public class YamlParser {

        //--- Class Methods ---
        public static YamlStream Parse(string source) {
            var parser = new Parser(new StringReader(source));
            if(!parser.MoveNext()) {
                throw new Exception("unexpected end");
            }
            var yamlParser = new YamlParser();
            return yamlParser.ParseStream(parser);
        }

        //--- Methods ---
        public YamlStream ParseStream(IParser parser) {
            if(!(parser.Current is StreamStart start)) {
                throw new Exception($"unexpected ({parser.Current.GetType()})");
            }
            var result = new YamlStream {
                Start = start,
                Documents = new List<YamlDocument>()
            };
            while(true) {
                if(!parser.MoveNext()) {
                    throw new Exception("unexpected end");
                }
                switch(parser.Current) {
                case StreamEnd end:
                    result.End = end;
                    return result;
                case DocumentStart _:
                    result.Documents.Add(ParseDocument(parser));
                    break;
                case DocumentEnd _:
                case StreamStart _:
                case MappingEnd _:
                case MappingStart _:
                case AnchorAlias _:
                case Scalar _:
                case SequenceEnd _:
                case SequenceStart _:
                default:
                    throw new Exception($"unexpected ({parser.Current.GetType()})");
                }
            }
        }

        public YamlDocument ParseDocument(IParser parser) {
            if(!(parser.Current is DocumentStart start)) {
                throw new Exception($"unexpected ({parser.Current.GetType()})");
            }
            var result = new YamlDocument {
                Start = start,
                Values = new List<AYamlValue>()
            };
            while(true) {
                if(!parser.MoveNext()) {
                    throw new Exception("unexpected end");
                }
                switch(parser.Current) {
                case DocumentEnd end:
                    result.End = end;
                    return result;
                case MappingStart _:
                    result.Values.Add(ParseMap(parser));
                    break;
                case Scalar scalar:
                    result.Values.Add(new YamlScalar(scalar));
                    break;
                case SequenceStart _:
                    result.Values.Add(ParseSequence(parser));
                    break;
                case DocumentStart _:
                case MappingEnd _:
                case AnchorAlias _:
                case SequenceEnd _:
                case StreamEnd _:
                case StreamStart _:
                default:
                    throw new Exception($"unexpected ({parser.Current.GetType()})");
                }
            }
        }

        public YamlMap ParseMap(IParser parser) {
            if(!(parser.Current is MappingStart start)) {
                throw new Exception($"unexpected ({parser.Current.GetType()})");
            }
            var result = new YamlMap {
                Start = start,
                Entries = new List<KeyValuePair<YamlScalar, AYamlValue>>()
            };
            while(true) {

                // next token is either end-of-map or scalar key
                if(!parser.MoveNext()) {
                    throw new Exception("unexpected end");
                }
                YamlScalar key;
                switch(parser.Current) {
                case MappingEnd end:
                    result.End = end;
                    return result;
                case Scalar scalar:
                    key = new YamlScalar(scalar);
                    break;
                case MappingStart _:
                case AnchorAlias _:
                case DocumentEnd _:
                case DocumentStart _:
                case SequenceEnd _:
                case SequenceStart _:
                case StreamEnd _:
                case StreamStart _:
                default:
                    throw new Exception($"unexpected ({parser.Current.GetType()})");
                }

                // parse value
                if(!parser.MoveNext()) {
                    throw new Exception("unexpected end");
                }
                AYamlValue value;
                switch(parser.Current) {
                case MappingStart _:
                    value = ParseMap(parser);
                    break;
                case AnchorAlias anchorAlias:
                    value = new YamlAnchorAlias(anchorAlias);
                    break;
                case Scalar scalar:
                    value = new YamlScalar(scalar);
                    break;
                case SequenceStart _:
                    value = ParseSequence(parser);
                    break;
                case DocumentEnd _:
                case DocumentStart _:
                case SequenceEnd _:
                case StreamEnd _:
                case StreamStart _:
                case MappingEnd _:
                default:
                    throw new Exception($"unexpected ({parser.Current.GetType()})");
                }
                result.Entries.Add(new KeyValuePair<YamlScalar, AYamlValue>(key, value));
            }
        }

        public YamlSequence ParseSequence(IParser parser) {
            if(!(parser.Current is SequenceStart start)) {
                throw new Exception($"unexpected ({parser.Current.GetType()})");
            }
            var result = new YamlSequence {
                Start = start,
                Values = new List<AYamlValue>()
            };
            while(true) {

                // next token is either end-of-map or scalar
                if(!parser.MoveNext()) {
                    throw new Exception("unexpected end");
                }
                switch(parser.Current) {
                case MappingStart _:
                    result.Values.Add(ParseMap(parser));
                    break;
                case SequenceEnd end:
                    result.End = end;
                    return result;
                case Scalar scalar:
                    result.Values.Add(new YamlScalar(scalar));
                    break;
                case SequenceStart _:
                    result.Values.Add(ParseSequence(parser));
                    break;
                case MappingEnd _:
                case AnchorAlias _:
                case DocumentEnd _:
                case DocumentStart _:
                case StreamEnd _:
                case StreamStart _:
                default:
                    throw new Exception($"unexpected ({parser.Current.GetType()})");
                }
            }
        }
    }
}