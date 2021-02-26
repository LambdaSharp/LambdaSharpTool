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
using System.Runtime.CompilerServices;

namespace LambdaSharp.Compiler {

    public class SourceLocation {

        //--- Class Fields ---
        public static readonly SourceLocation Empty = new SourceLocation("", 0, 0, 0, 0);

        //--- Constructors ---
        public SourceLocation(string filePath, int lineNumberStart, int lineNumberEnd, int columnNumberStart, int columnNumberEnd) {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            LineNumberStart = lineNumberStart;
            LineNumberEnd = lineNumberEnd;
            ColumnNumberStart = columnNumberStart;
            ColumnNumberEnd = columnNumberEnd;
        }

        public SourceLocation(string filePath, int lineNumber) : this(filePath, lineNumber, lineNumber, 0, 0) { }

        //--- Properties ---
        public string FilePath { get; }
        public int LineNumberStart { get; }
        public int ColumnNumberStart { get; }
        public int LineNumberEnd { get; }
        public int ColumnNumberEnd { get; }

        //--- Methods ---
        public override string ToString() {
            if(FilePath == "") {
                return "";
            }
            if(LineNumberStart == 0) {
                return FilePath;
            }
            if(ColumnNumberStart == 0) {
                return $"{FilePath}({LineNumberStart})";
            }
            return $"{FilePath}({LineNumberStart},{ColumnNumberStart})";
        }
    }
}