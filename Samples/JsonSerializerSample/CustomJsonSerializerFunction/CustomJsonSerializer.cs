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

using System;
using System.IO;
using System.Text;
using LambdaSharp.Serialization;
using LitJson;

namespace Sample.JsonSerializer.CustomJsonSerializerFunction {

    public class CustomJsonSerializer : ILambdaJsonSerializer {

        //--- Methods ---
        public object Deserialize(Stream stream, Type type) {
            if(type == typeof(FunctionRequest)) {

                // custom deserialization for FunctionRequest type
                var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var jsonReader = new JsonReader(json);
                var result = new FunctionRequest();
                while(jsonReader.Read()) {
                    switch(jsonReader.Token) {
                    case JsonToken.PropertyName:
                        if((string)jsonReader.Value == "foo") {
                            if(jsonReader.Read() && (jsonReader.Token == JsonToken.String)) {
                                result.Bar = (string)jsonReader.Value;
                            }
                        }
                        break;
                    }
                }
                return result;
            } else {
                throw new NotSupportedException($"JSON deserialization is not supported for {type.FullName}");
            }
        }

        public T Deserialize<T>(Stream requestStream) => (T)Deserialize(requestStream, typeof(T));

        public void Serialize<T>(T response, Stream responseStream) {
            switch(response) {
            case FunctionRequest functionRequest: {

                    // custom serialization for FunctionRequest type
                    var builder = new StringBuilder();
                    var jsonWriter = new JsonWriter(builder);
                    jsonWriter.WriteObjectStart();
                    jsonWriter.WritePropertyName("foo");
                    jsonWriter.Write(functionRequest.Bar);
                    jsonWriter.WriteObjectEnd();
                    var json = builder.ToString();
                    responseStream.Write(Encoding.UTF8.GetBytes(json));
                }
                break;
            case FunctionResponse functionResponse: {

                    // custom serialization for FunctionResponse type
                    var builder = new StringBuilder();
                    var jsonWriter = new JsonWriter(builder);
                    jsonWriter.WriteObjectStart();
                    jsonWriter.WritePropertyName("foo");
                    jsonWriter.Write(functionResponse.Bar);
                    jsonWriter.WriteObjectEnd();
                    var json = builder.ToString();
                    responseStream.Write(Encoding.UTF8.GetBytes(json));
                }
                break;
            default:
                throw new NotSupportedException($"JSON serialization is not supported for {response?.GetType().FullName ?? "<null>"}");
            }
        }
    }
}
