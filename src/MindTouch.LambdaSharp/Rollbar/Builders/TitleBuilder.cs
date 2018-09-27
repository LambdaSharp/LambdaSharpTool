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

using System.Globalization;
using System.Linq;
using MindTouch.Rollbar.Data;

namespace MindTouch.Rollbar.Builders {

    public class TitleBuilder {

        //--- Constants ---
        private const int MAX_TITLE_LENGTH = 255;

        //--- Interface Methods ---
        public string CreateFromBody(Body body) {
            if(body.Message != null) {
                return body.Message.Body;
            }
            var trace = body.Trace ?? body.TraceChain.LastOrDefault() ?? body.TraceChain.Last();
            var message = trace.Exception.Message;
            var title = string.Format(CultureInfo.InvariantCulture, "{0}: {1}", trace.Exception.ClassName, message);
            return title.Length > MAX_TITLE_LENGTH ? title.Substring(0, MAX_TITLE_LENGTH) : title;
        }
    }
}
