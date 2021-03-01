using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp.CloudFormation.Converter;
using LambdaSharp.Schedule;
using LambdaSharp.Serialization;

namespace LambdaSharp.Cloud.UpdateIamSpecFunction {

    public sealed class Function : ALambdaScheduleFunction {

        //--- Fields ---
        private string _destinationBucketName;
        private IamPermissionsConverter _converter;
        private IAmazonS3 _s3Client;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read function settings
            _destinationBucketName = config.ReadS3BucketName("DestinationBucket");

            // initialize clients
            _converter = new IamPermissionsConverter(
                HttpClient,
                logInfo: message => LogInfo(message),
                logWarn: message => LogWarn(message),
                logError: (exception, message) => LogError(exception, message)
            );
            _s3Client = new AmazonS3Client();
        }

        public override async Task ProcessEventAsync(LambdaScheduleEvent schedule) {

            // generate extended IAM specification for region
            LogInfo($"fetching latest IAM specification");
            var specification = await _converter.GenerateIamSpecificationAsync();

            // serialize IAM specification into a brotli compressed stream
            var compressedJsonSpecificationStream = new MemoryStream();
            using(var brotliStream = new BrotliStream(compressedJsonSpecificationStream, CompressionLevel.Optimal, leaveOpen: true)) {
                await JsonSerializer.SerializeAsync(brotliStream, specification, new JsonSerializerOptions {
                    IgnoreNullValues = true,
                    IncludeFields = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                brotliStream.Flush();
            }
            compressedJsonSpecificationStream.Position = 0;

            // compute MD5 of new compressed IAM specification
            byte[] newMD5Hash;
            using(var md5 = MD5.Create()) {
                newMD5Hash = md5.ComputeHash(compressedJsonSpecificationStream);
                compressedJsonSpecificationStream.Position = 0;
            }
            var newETag = $"\"{string.Concat(newMD5Hash.Select(x => x.ToString("x2")))}\"";
            LogInfo($"compressed IAM specification ETag is {newETag} (size: {compressedJsonSpecificationStream.Length:N0} bytes)");

            // update compressed IAM specification in S3
            LogInfo($"uploading new IAM specification");
            await _s3Client.PutObjectAsync(new PutObjectRequest {
                BucketName = _destinationBucketName,
                Key = $"AWS/IamSpecification.json.br",
                InputStream = compressedJsonSpecificationStream,
                MD5Digest = Convert.ToBase64String(newMD5Hash),
                Headers = {
                    ContentEncoding = "br",
                    ContentType = "application/json; charset=utf-8",
                    ContentMD5 = newETag
                }
            });
            LogInfo($"done");
        }
    }
}
