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

namespace MindTouch.LambdaSharp.Tool {

    public abstract class AModelProcessor {

        //--- Fields ---
        private readonly Settings _settings;
        private Stack<string> _locations = new Stack<string>();

        //--- Constructors ---
        protected AModelProcessor(Settings settings) {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        //--- Properties ---
        public Settings Settings { get => _settings; }

        //--- Methods ---
        protected void AtLocation(string location, Action action) {
            try {
                _locations.Push(location);
                action();
            } catch(Exception e) {
                AddError($"internal error: {e.Message}", e);
            } finally {
                _locations.Pop();
            }
        }

        protected T AtLocation<T>(string location, Func<T> function, T onErrorReturn) {
            try {
                _locations.Push(location);
                return function();
            } catch(Exception e) {
                AddError($"internal error: {e.Message}", e);
                return onErrorReturn;
            } finally {
                _locations.Pop();
            }
        }

        protected void AddError(string message, Exception exception = null)
            => _settings.AddError($"{message} @ {string.Join("/", _locations.Reverse())} [{Settings.ModuleFileName}]", exception);
    }
}