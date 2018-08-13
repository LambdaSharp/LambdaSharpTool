/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
using System.Diagnostics;
using System.Linq;
using MindTouch.Rollbar.Data;

namespace MindTouch.Rollbar.Builders {
    public class FrameCollectionBuilder : IFrameCollectionBuilder {
        
        //--- Methods ---
        public IEnumerable<Frame> CreateFromException(Exception exception) {
            var stackTrace = new StackTrace(exception, true);
            var lines = new List<Frame>();
            var stackFrames = stackTrace.GetFrames();
            if(stackFrames == null || stackFrames.Length == 0) {
                return lines;
            }

            // process all stack frames
            foreach(var frame in stackFrames) {
                var lineNumber = frame.GetFileLineNumber();
                var fileName = frame.GetFileName();

                string methodName = null;
                var method = frame.GetMethod();
                if(method != null) {
                    var methodParams = method.GetParameters();

                    // add method parameters to the method name. helpful for resolving overloads.
                    methodName = method.Name;
                    if(methodParams.Length > 0) {
                        var paramDesc = string.Join(", ", methodParams.Select(p => p.ParameterType + " " + p.Name));
                        methodName = methodName + "(" + paramDesc + ")";
                    }
                }

                // when the line number is zero, you can try using the IL offset
                if(lineNumber == 0) {
                    lineNumber = frame.GetILOffset();
                }

                if(lineNumber == -1) {
                    lineNumber = frame.GetNativeOffset();
                }

                // line numbers less than 0 are not accepted
                if(lineNumber < 0) {
                    lineNumber = 0;
                }

                // file names aren't always available, so use the type name instead, if possible
                if(string.IsNullOrEmpty(fileName)) {
                    fileName = method.ReflectedType.ToString();
                }

                // NOTE: Set CodeContext and Code (lines of code above and below the line that raised the exception).
                lines.Add(new Frame(fileName, lineNumber, null, methodName));
            }

            return lines.ToArray();
        }
    }
}
