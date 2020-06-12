/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LambdaSharp.CloudFormation.Specification;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace LambdaSharp.CloudFormation.Converter {

    public class CloudFormationSpecificationConverter {

        //--- Constants ---
        private const string CFN_LINT_SOURCE = "https://github.com/aws-cloudformation/cfn-python-lint/archive/master.zip";

        //--- Class Fields ---
        private static Dictionary<string, string> RegionalSpecifications = new Dictionary<string, string> {
            ["af-south-1"] = "https://cfn-resource-specifications-af-south-1-prod.s3.af-south-1.amazonaws.com/latest/gzip/CloudFormationResourceSpecification.json",
            ["ap-east-1"] = "https://cfn-resource-specifications-ap-east-1-prod.s3.ap-east-1.amazonaws.com/latest/gzip/CloudFormationResourceSpecification.json",
            ["ap-northeast-1"] = "https://d33vqc0rt9ld30.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["ap-northeast-2"] = "https://d1ane3fvebulky.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["ap-northeast-3"] = "https://d2zq80gdmjim8k.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["ap-south-1"] = "https://d2senuesg1djtx.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["ap-southeast-1"] = "https://doigdx0kgq9el.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["ap-southeast-2"] = "https://d2stg8d246z9di.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["ca-central-1"] = "https://d2s8ygphhesbe7.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["cn-north-1"] = "https://cfn-resource-specifications-cn-north-1-prod.s3.cn-north-1.amazonaws.com.cn/latest/gzip/CloudFormationResourceSpecification.json",
            ["cn-northwest-1"] = "https://cfn-resource-specifications-cn-northwest-1-prod.s3.cn-northwest-1.amazonaws.com.cn/latest/gzip/CloudFormationResourceSpecification.json",
            ["eu-central-1"] = "https://d1mta8qj7i28i2.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["eu-north-1"] = "https://diy8iv58sj6ba.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["eu-south-1"] = "https://cfn-resource-specifications-eu-south-1-prod.s3.eu-south-1.amazonaws.com/latest/gzip/CloudFormationResourceSpecification.json",
            ["eu-west-1"] = "https://d3teyb21fexa9r.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["eu-west-2"] = "https://d1742qcu2c1ncx.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["eu-west-3"] = "https://d2d0mfegowb3wk.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["me-south-1"] = "https://cfn-resource-specifications-me-south-1-prod.s3.me-south-1.amazonaws.com/latest/gzip/CloudFormationResourceSpecification.json",
            ["sa-east-1"] = "https://d3c9jyj3w509b0.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["us-east-1"] = "https://d1uauaxba7bl26.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["us-east-2"] = "https://dnwj8swjjbsbt.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["us-west-1"] = "https://d68hl49wbnanq.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["us-west-2"] = "https://d201a2mn26r7lk.cloudfront.net/latest/gzip/CloudFormationResourceSpecification.json",
            ["us-gov-east-1"] = "https://s3.us-gov-east-1.amazonaws.com/cfn-resource-specifications-us-gov-east-1-prod/latest/gzip/CloudFormationResourceSpecification.json",
            ["us-gov-west-1"] = "https://s3.us-gov-west-1.amazonaws.com/cfn-resource-specifications-us-gov-west-1-prod/latest/gzip/CloudFormationResourceSpecification.json"
        };

        //--- Class Properties ---
        public static IEnumerable<string> Regions => RegionalSpecifications.Keys;

        //--- Fields ---
        public HttpClient _httpClient;
        private Dictionary<string, Dictionary<string, JsonPatchDocument<ExtendedCloudFormationSpecification>>> _globalExtendedSpecifications = new Dictionary<string, Dictionary<string, JsonPatchDocument<ExtendedCloudFormationSpecification>>>();

        //--- Constructors ---
        public CloudFormationSpecificationConverter(HttpClient? httpClient = null) {
            _httpClient = httpClient ?? new HttpClient();
        }

        //--- Methods ---
        public async Task<ProcessedCloudFormationSpecification> GenerateCloudFormationSpecificationAsync(string region) {
            if(!RegionalSpecifications.TryGetValue(region, out var url)) {
                throw new ArgumentException("unsupported region", nameof(region));
            }

            // download CloudFormation specificatio for region
            var response = await _httpClient.GetAsync(url);
            using var decompressionStream = new GZipStream(await response.Content.ReadAsStreamAsync(), CompressionMode.Decompress);
            using var decompressedMemoryStream = new MemoryStream();
            await decompressionStream.CopyToAsync(decompressedMemoryStream);
            var text = Encoding.UTF8.GetString(decompressedMemoryStream.ToArray());
            var spec = JsonConvert.DeserializeObject<ExtendedCloudFormationSpecification>(text);

            // apply all patches to original specification
            if(!_globalExtendedSpecifications.Any()) {
                await InitializeExtendedCloudFormationSpecificationsFromGitHubAsync();
            }
            var warnings = new List<string>();
            ApplyPatches("all");
            ApplyPatches(region);

            // strip all "Documentation" fields to reduce document size
            var json = JObject.FromObject(spec, new JsonSerializer {
                NullValueHandling = NullValueHandling.Ignore,
                Converters = {
                    new StringEnumConverter()
                }
            });

            // normalize order of fields
            json = OrderFields(json);

            // return final specification and warnings
            return new ProcessedCloudFormationSpecification(region, json, warnings);

            // local functions
            void ApplyPatches(string regionKey) {
                if(!_globalExtendedSpecifications.TryGetValue(regionKey, out var regionalExtendedSpecifications)) {
                    warnings.Add($"[{regionKey}] NOT FOUND");
                    return;
                }
                foreach(var patch in regionalExtendedSpecifications.OrderBy(kv => kv.Key)) {
                    var first = true;
                    patch.Value.ApplyTo(spec, error => {

                        // check if we need a warning header
                        if(first) {
                            warnings.Add($"[{regionKey}/{patch.Key}]");
                            first = false;
                        }
                        warnings.Add($"{error.ErrorMessage} {JsonConvert.SerializeObject(error.Operation, Formatting.None)}");
                    });
                }
            }

            JObject OrderFields(JObject value) {
                var result = new JObject();
                foreach(var property in value.Properties().ToList().OrderBy(property => property.Name)) {
                    result.Add(property.Name, (property.Value is JObject propertyValue)
                        ? OrderFields(propertyValue)
                        : property.Value
                    );
                }
                return result;
            }
        }

        public async Task InitializeExtendedCloudFormationSpecificationsFromGitHubAsync() {
            const string PREFIX = "cfn-python-lint-master/src/cfnlint/data/ExtendedSpecs/";
            var response = await _httpClient.GetAsync(CFN_LINT_SOURCE);
            using var zip = new ZipArchive(await response.Content.ReadAsStreamAsync());
            foreach(var entry in zip.Entries.Where(entry => entry.FullName.StartsWith(PREFIX, StringComparison.Ordinal) && entry.FullName.EndsWith(".json", StringComparison.Ordinal))) {

                // convert filepath into region-key pair
                var segments = entry.FullName.Substring(PREFIX.Length).Split('/', 2);
                var region = segments[0];
                var key = segments[1];

                // create entry for each file
                if(!_globalExtendedSpecifications.TryGetValue(region, out var regionalExtendedSpecifications)) {
                    regionalExtendedSpecifications = new Dictionary<string, JsonPatchDocument<ExtendedCloudFormationSpecification>>();
                    _globalExtendedSpecifications[region] = regionalExtendedSpecifications;
                }

                // unzip entry
                using var stream = entry.Open();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                var text = Encoding.UTF8.GetString(memoryStream.ToArray());

                // store extended specification document
                var patch = JsonConvert.DeserializeObject<JsonPatchDocument<ExtendedCloudFormationSpecification>>(text);
                regionalExtendedSpecifications.Add(key, patch);
            }
        }

        public async Task InitializeExtendedCloudFormationSpecificationsFromFolderAsync(string path) {
            var pathDelimiter = new[] { '/', '\\' };
            path = path.TrimEnd(pathDelimiter);
            foreach(var file in Directory.GetFiles(path, "*.json", SearchOption.AllDirectories)) {

                // convert filepath into region-key pair
                var segments = file.Substring(path.Length + 1).Split(pathDelimiter, 2);
                var region = segments[0];
                var key = segments[1];

                // create entry for each file
                if(!_globalExtendedSpecifications.TryGetValue(region, out var regionalExtendedSpecifications)) {
                    regionalExtendedSpecifications = new Dictionary<string, JsonPatchDocument<ExtendedCloudFormationSpecification>>();
                    _globalExtendedSpecifications[region] = regionalExtendedSpecifications;
                }

                // unzip entry
                using var stream = File.OpenRead(file);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                var text = Encoding.UTF8.GetString(memoryStream.ToArray());

                // store extended specification document
                var patch = JsonConvert.DeserializeObject<JsonPatchDocument<ExtendedCloudFormationSpecification>>(text);
                regionalExtendedSpecifications.Add(key, patch);
            }
        }
    }
}