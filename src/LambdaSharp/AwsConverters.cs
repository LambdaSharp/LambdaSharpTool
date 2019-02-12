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

namespace LambdaSharp {

    public static class AwsConverters {

        //--- Class Methods ---
        public static string ConvertQueueArnToUrl(string arn) {

            // convert from 'arn:aws:sqs:us-east-2:123456789012:aa4-MyQueue-Z5NOSZO2PZE9'
            //  to 'https://sqs.us-east-2.amazonaws.com/123456789012/aa4-MyQueue-Z5NOSZO2PZE9'
            var parts = arn.Split(':');
            if((parts.Length != 6) || (parts[0] != "arn") || (parts[1] != "aws") || (parts[2] != "sqs")) {
                throw new ArgumentException("unexpected format", nameof(arn));
            }
            var region = parts[3];
            var accountId = parts[4];
            var queueName = parts[5];
            return $"https://sqs.{region}.amazonaws.com/{accountId}/{queueName}";
        }

        public static string ConvertBucketArnToName(string arn) {

            // convert from 'arn:aws:s3:::bucket_name'
            //  to 'bucket_name'
            var parts = arn.Split(':');
            if((parts.Length != 6) || (parts[0] != "arn") || (parts[1] != "aws") || (parts[2] != "s3")) {
                throw new ArgumentException("unexpected format", nameof(arn));
            }
            return parts[5];
        }

        public static string ConvertFunctionArnToName(string arn) {

            // convert from 'arn:aws:lambda:us-east-2:123456789012:function:function-name'
            //  to 'function-name'
            var parts = arn.Split(':');
            if((parts.Length != 7) || (parts[0] != "arn") || (parts[1] != "aws") || (parts[2] != "lambda") || (parts[5] != "function")) {
                throw new ArgumentException("unexpected format", nameof(arn));
            }
            return parts[6];
        }

        public static string ConvertDynamoDBArnToName(string arn) {

            // convert from 'arn:aws:dynamodb:us-east-2:123456789012:table/tablename'
            //  to 'tablename'
            var parts = arn.Split(':');
            if((parts.Length != 6) || (parts[0] != "arn") || (parts[1] != "aws") || (parts[2] != "dynamodb")) {
                throw new ArgumentException("unexpected format", nameof(arn));
            }
            var table = parts[5];
            const string TABLE_PREFIX = "table/";
            if(!table.StartsWith(TABLE_PREFIX, StringComparison.Ordinal)) {
                throw new ArgumentException("unexpected format", nameof(arn));
            }
            return table.Substring(TABLE_PREFIX.Length);
        }

        public static string ReadSqsQueueUrl(this LambdaConfig config, string key) {
            var value = config.ReadText(key);
            return (value.StartsWith("arn:", StringComparison.Ordinal))
                ? ConvertQueueArnToUrl(value)
                : value;
        }

        public static string ReadS3BucketName(this LambdaConfig config, string key) {
            var value = config.ReadText(key);
            return (value.StartsWith("arn:", StringComparison.Ordinal))
                ? ConvertBucketArnToName(value)
                : value;
        }

        public static string ReadLambdaFunctionName(this LambdaConfig config, string key) {
            var value = config.ReadText(key);
            return (value.StartsWith("arn:", StringComparison.Ordinal))
                ? ConvertFunctionArnToName(value)
                : value;
        }

        public static string ReadDynamoDBTableName(this LambdaConfig config, string key) {
            var value = config.ReadText(key);
            return (value.StartsWith("arn:", StringComparison.Ordinal))
                ? ConvertDynamoDBArnToName(value)
                : value;
        }
    }
}
