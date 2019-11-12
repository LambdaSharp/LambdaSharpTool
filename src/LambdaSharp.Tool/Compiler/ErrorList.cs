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

using System.Collections.Generic;
using LambdaSharp.Tool.Compiler.Parser;

namespace LambdaSharp.Tool.Compiler {

    public class ErrorList {

        //--- Fields ---
        private readonly List<string> _messages = new List<string>();

        //--- Properties ---
        public IEnumerable<string> Messages => _messages;

        //--- Methods ---
        public void Add(Error error, SourceLocation sourceLocation, bool excact) {
            if(sourceLocation == null) {
                _messages.Add($"ERROR{error.Code}: {error.Message} @ (no location information available)");
            } else if(excact) {
                _messages.Add($"ERROR{error.Code}: {error.Message} @ {sourceLocation.FilePath ?? "n/a"}({sourceLocation.LineNumberStart},{sourceLocation.ColumnNumberStart})");
            } else {
                _messages.Add($"ERROR{error.Code}: {error.Message} @ (near) {sourceLocation.FilePath ?? "n/a"}({sourceLocation.LineNumberStart},{sourceLocation.ColumnNumberStart})");
            }
        }
    }
}
