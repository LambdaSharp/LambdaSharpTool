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
using MindTouch.Rollbar.Data;

namespace MindTouch.Rollbar.Builders {

    public class PayloadBuilder {

        //--- Fields ---
        private readonly RollbarConfiguration _configuration;
        private readonly DataBuilder _dataBuilder;

        //--- Contructors ---
        public PayloadBuilder(RollbarConfiguration configuration, DataBuilder dataBuilder) {
            if(configuration == null) {
                throw new ArgumentNullException("configuration");
            }
            if(dataBuilder == null) {
                throw new ArgumentNullException("dataBuilder");
            }
            _configuration = configuration;
            _dataBuilder = dataBuilder;
        }

        //--- Methods ---
        public Payload CreateFromException(Exception exception, string description, string level) {
            var data = _dataBuilder.CreateFromException(exception, description, level);
            return new Payload(_configuration.AccessToken, data);
        }

        public Payload CreateWithContext(Payload payload, Context context) {
            var data = _dataBuilder.CreateWithContext(payload.RollbarData, context);
            return new Payload(_configuration.AccessToken, data);
        }

        public Payload CreateWithFingerprintInput(Payload payload, string fingerprintInput) {
            var data = _dataBuilder.CreateWithFingerprintInput(payload.RollbarData, fingerprintInput);
            return new Payload(_configuration.AccessToken, data);
        }

        public Payload CreateWithContextAndFingerprintInput(Payload payload, Context context, string fingerprintInput) {
            var data = _dataBuilder.CreateWithContextAndFingerprintInput(payload.RollbarData, context, fingerprintInput);
            return new Payload(_configuration.AccessToken, data);
        }

        public Payload CreateFromMessage(string message, string level) {
            var data = _dataBuilder.CreateFromMessage(message, level);
            return new Payload(_configuration.AccessToken, data);
        }
    }
}
