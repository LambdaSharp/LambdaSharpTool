/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
 * lambdasharp.net
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
using System.IO;
using System.Text;

namespace LambdaSharp {

    /// <summary>
    /// The static <see cref="AwsConverters"/> class provides convenience methods for
    /// converting <i>Amazon Resource Name</i> (ARN) into an alternative representation more
    /// suitable for their respective clients.
    /// </summary>
    public static class AwsConverters {

        //--- Extension Methods ---

        /// <summary>
        /// Read an SQS queue ARN from <see cref="LambdaConfig"/> and convert it to a queue URL.
        /// </summary>
        /// <param name="config">The <see cref="LambdaConfig"/> instance.</param>
        /// <param name="key">The key to read.</param>
        /// <returns>SQS queue URL.</returns>
        public static string ReadSqsQueueUrl(this LambdaConfig config, string key) {
            var value = config.ReadText(key);
            return (value.StartsWith("arn:", StringComparison.Ordinal))
                ? ConvertQueueArnToUrl(value)
                : value;
        }

        /// <summary>
        /// Read an S3 bucket ARN from <see cref="LambdaConfig"/> and convert it to a bucket name.
        /// </summary>
        /// <param name="config">The <see cref="LambdaConfig"/> instance.</param>
        /// <param name="key">The key to read.</param>
        /// <returns>S3 bucket name.</returns>
        public static string ReadS3BucketName(this LambdaConfig config, string key) {
            var value = config.ReadText(key);
            return (value.StartsWith("arn:", StringComparison.Ordinal))
                ? ConvertBucketArnToName(value)
                : value;
        }

        /// <summary>
        /// Read a Lambda function ARN from <see cref="LambdaConfig"/> and convert it to a function name.
        /// </summary>
        /// <param name="config">The <see cref="LambdaConfig"/> instance.</param>
        /// <param name="key">The key to read.</param>
        /// <returns>Lambda function name.</returns>
        public static string ReadLambdaFunctionName(this LambdaConfig config, string key) {
            var value = config.ReadText(key);
            return (value.StartsWith("arn:", StringComparison.Ordinal))
                ? ConvertFunctionArnToName(value)
                : value;
        }

        /// <summary>
        /// Read a DynamoDB table ARN from <see cref="LambdaConfig"/> and convert it to a table name.
        /// </summary>
        /// <param name="config">The <see cref="LambdaConfig"/> instance.</param>
        /// <param name="key">The key to read.</param>
        /// <returns>DynamoDB table name.</returns>
        public static string ReadDynamoDBTableName(this LambdaConfig config, string key) {
            var value = config.ReadText(key);
            return (value.StartsWith("arn:", StringComparison.Ordinal))
                ? ConvertDynamoDBArnToName(value)
                : value;
        }

        /// <summary>
        /// The <see cref="ToStream(string)"/> extension method converts a string value to a UTF-8 encoded memory stream.
        /// </summary>
        /// <param name="value">The <see cref="string"/> value.</param>
        /// <returns>The UTF-8 encoded stream.</returns>
        public static Stream ToStream(this string value) => new MemoryStream(Encoding.UTF8.GetBytes(value));

        //--- Class Methods ---

        /// <summary>
        /// Convert an SQS queue ARN into a queue URL.
        /// </summary>
        /// <param name="arn">SQS queue ARN.</param>
        /// <returns>SQS queue URL.</returns>
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

        /// <summary>
        /// Convert an S3 bucket ARN into a bucket name.
        /// </summary>
        /// <param name="arn">S3 bucket ARN.</param>
        /// <returns>S3 bucket name.</returns>
        public static string ConvertBucketArnToName(string arn) {

            // convert from 'arn:aws:s3:::bucket_name'
            //  to 'bucket_name'
            var parts = arn.Split(':');
            if((parts.Length != 6) || (parts[0] != "arn") || (parts[1] != "aws") || (parts[2] != "s3")) {
                throw new ArgumentException("unexpected format", nameof(arn));
            }
            return parts[5];
        }

        /// <summary>
        /// Convert a Lambda function ARN into a function name.
        /// </summary>
        /// <param name="arn">Lambda function ARN.</param>
        /// <returns>Lambda function name.</returns>
        public static string ConvertFunctionArnToName(string arn) {

            // convert from 'arn:aws:lambda:us-east-2:123456789012:function:function-name'
            //  to 'function-name'
            var parts = arn.Split(':');
            if((parts.Length != 7) || (parts[0] != "arn") || (parts[1] != "aws") || (parts[2] != "lambda") || (parts[5] != "function")) {
                throw new ArgumentException("unexpected format", nameof(arn));
            }
            return parts[6];
        }

        /// <summary>
        /// Convert a DynamoDB table ARN into a table name.
        /// </summary>
        /// <param name="arn">DynamoDB table ARN.</param>
        /// <returns>DynamoDB table name.</returns>
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
    }
}
