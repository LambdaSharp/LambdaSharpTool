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

using System.Collections.Specialized;

namespace MindTouch.Rollbar.Data {
    
    public class Context {
        
        //--- Fields ---
        private readonly string _applicationContext;
        private readonly NameValueCollection _custom;
        private readonly Person _person;
        private readonly Request _request;

        //--- Constructors ---
        public Context(
            string applicationContext,
            Request request,
            Person person,
            NameValueCollection custom) {
            _applicationContext = applicationContext;
            _request = request;
            _person = person;
            _custom = custom;
        }

        //--- Properties ---
        public string ApplicationContext {
            get { return _applicationContext; }
        }

        public Request Request {
            get { return _request; }
        }

        public Person Person {
            get { return _person; }
        }

        public NameValueCollection Custom {
            get { return _custom; }
        }
    }
}
