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

namespace LambdaSharp.Tool.Compiler.Parser.Syntax {

    public class SourceLocation {

        //--- Properties ---
        public string FilePath { get; set; }
        public int LineNumberStart { get; set; }
        public int ColumnNumberStart { get; set; }
        public int LineNumberEnd { get; set; }
        public int ColumnNumberEnd { get; set; }
    }
}