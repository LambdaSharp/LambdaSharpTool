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
using Amazon.KeyManagementService;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using MindTouch.LambdaSharp.ConfigSource;

namespace MindTouch.LambdaSharp {
    public class LambdaFunctionConfiguration {

        //--- Class Fields ---
        public static readonly LambdaFunctionConfiguration Instance = new LambdaFunctionConfiguration {
            SqsClient = new AmazonSQSClient(),
            SnsClient = new AmazonSimpleNotificationServiceClient(),
            KmsClient = new AmazonKeyManagementServiceClient(),
            EnvironmentSource = new LambdaSystemEnvironmentSource(),
            UtcNow = () => DateTime.UtcNow
        };

        //--- Properties ---
        public IAmazonKeyManagementService KmsClient { get; set; }
        public IAmazonSimpleNotificationService SnsClient { get; set; }
        public IAmazonSQS SqsClient { get; set; }
        public ILambdaConfigSource EnvironmentSource { get; set; }
        public Func<DateTime> UtcNow { get; set; }
    }
}
