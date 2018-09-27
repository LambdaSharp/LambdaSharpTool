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
using System.Linq;

namespace MindTouch.Rollbar.Builders {

    internal static class ExtensionMethods {

        //--- Methods ---
        public static IEnumerable<Exception> FlattenHierarchy(this Exception exception) {
            return FlattenExceptionHierarchy(exception).ToArray();
        }

        private static IEnumerable<Exception> FlattenExceptionHierarchy(Exception exception) {
            var current = exception;
            do {
                yield return current;
                current = current.InnerException;
            } while(current != null);
        }
    }
}
