/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
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

namespace LambdaSharp {

    public static class LambdaConfigEx {

        //--- Extension Methods ---
        public static string ReadText(this LambdaConfig config, string key, Action<string> validate = null)
            => config.Read(key, fallback: null, convert: v => v, validate: validate);

        public static string ReadText(this LambdaConfig config, string key, string defaultValue, Action<string> validate = null)
            => config.Read(key, fallback: _ => defaultValue, convert: v => v, validate: validate);

        public static IEnumerable<string> ReadCommaDelimitedList(this LambdaConfig config, string key, Action<IEnumerable<string>> validate = null)
            => config.Read(key, fallback: null, convert: v => v.Split(",", StringSplitOptions.RemoveEmptyEntries), validate: validate);

        public static int ReadInt(this LambdaConfig config, string key, Action<int> validate = null)
            => config.Read(key, fallback: null, convert: int.Parse, validate: validate);

        public static int ReadInt(this LambdaConfig config, string key, int defaultValue, Action<int> validate = null)
            => config.Read(key, fallback: _ => defaultValue, convert: int.Parse, validate: validate);

        public static TimeSpan ReadTimeSpan(this LambdaConfig config, string key, Action<TimeSpan> validate = null)
            => config.Read(key, fallback: null, convert: value => TimeSpan.FromSeconds(float.Parse(value)), validate: validate);

        public static TimeSpan ReadTimeSpan(this LambdaConfig config, string key, TimeSpan defaultValue, Action<TimeSpan> validate = null)
            => config.Read(key, fallback: _ => defaultValue, convert: value => TimeSpan.FromSeconds(float.Parse(value)), validate: validate);
    }
}
