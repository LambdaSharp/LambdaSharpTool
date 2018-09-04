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
using System.Security.Cryptography;
using System.Text;

namespace MindTouch.LambdaSharp.Tool.Internal {

    internal static class StringEx {

        //--- Extension Methods ---
        public static string ToMD5Hash(this string text) {
            using(var md5 = MD5.Create()) {
                return md5.ComputeHash(Encoding.UTF8.GetBytes(text)).ToHexString();
            }
        }

        public static string ToHexString(this IEnumerable<byte> bytes)
            => string.Concat(bytes.Select(x => x.ToString("X2")));
    }
}