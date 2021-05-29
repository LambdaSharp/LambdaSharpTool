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

namespace LambdaSharp.CloudFormation.Syntax {

    public class SourceLocation {

        //--- Class Fields ---
        public static readonly SourceLocation Empty = new SourceLocation("", lineStart: 0, lineEnd: 0, columnStart: 0, columnEnd: 0, exact: true);

        //--- Constructors ---
        public SourceLocation(string filePath, int lineStart, int lineEnd, int columnStart, int columnEnd, bool exact) {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            LineStart = lineStart;
            LineEnd = lineEnd;
            ColumnStart = columnStart;
            ColumnEnd = columnEnd;
            Exact = exact;
        }

        public SourceLocation([CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            : this(filePath, lineNumber, lineNumber, columnStart: 0, columnEnd: 0, exact: true) { }

        //--- Properties ---
        public string FilePath { get; }
        public int LineStart { get; }
        public int ColumnStart { get; }
        public int LineEnd { get; }
        public int ColumnEnd { get; }
        public bool Exact { get; }

        //--- Methods ---
        public SourceLocation WithExact(bool exact)
            => new SourceLocation(FilePath, LineStart, LineEnd, ColumnStart, ColumnEnd, exact);

        public SourceLocation NotExact() => WithExact(exact: false);

        public override string ToString() {
            if(FilePath == "") {
                return "";
            }
            if(LineStart == 0) {
                return FilePath;
            }
            if(ColumnStart == 0) {
                return $"{FilePath}({LineStart})";
            }
            return $"{FilePath}({LineStart},{ColumnStart})";
        }

        public override bool Equals(object? obj)
            => (obj is SourceLocation other) && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(FilePath.GetHashCode(), LineStart.GetHashCode(), LineEnd.GetHashCode(), ColumnStart.GetHashCode(), ColumnEnd.GetHashCode(), Exact.GetHashCode());

        public bool Equals(SourceLocation other)
            => (other != null)
                && (FilePath == other.FilePath)
                && (LineStart == other.LineStart) && (LineEnd == other.LineEnd)
                && (ColumnStart == other.ColumnStart) && (ColumnEnd == other.ColumnEnd)
                && (Exact == other.Exact);
    }
}