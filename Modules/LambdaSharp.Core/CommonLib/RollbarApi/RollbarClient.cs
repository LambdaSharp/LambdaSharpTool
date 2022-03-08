/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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

namespace LambdaSharp.Core.RollbarApi;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LambdaSharp.Serialization;
public class RollbarClientException : Exception {

    //--- Constructors ---
    public RollbarClientException(string message) : base(message) { }
}

public class RollbarResponse {

    //--- Properties ---
    [JsonPropertyName("err")]
    public int Error { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class RollbarCreateProjectRequest {

    //--- Properties ---
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class RollbarProject {

    //--- Properties ---
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("date_created")]
    [JsonConverter(typeof(JsonEpochSecondsDateTimeOffsetConverter))]
    public DateTimeOffset Created { get; set; }

    [JsonPropertyName("date_modified")]
    [JsonConverter(typeof(JsonEpochSecondsDateTimeOffsetConverter))]
    public DateTimeOffset Modified { get; set; }
}

public class RollbarProjectToken {

    //--- Properties ---
    [JsonPropertyName("project_id")]
    public int ProjectId { get; set; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("date_created")]
    [JsonConverter(typeof(JsonEpochSecondsDateTimeOffsetConverter))]
    public DateTimeOffset Created { get; set; }

    [JsonPropertyName("date_modified")]
    [JsonConverter(typeof(JsonEpochSecondsDateTimeOffsetConverter))]
    public DateTimeOffset Modified { get; set; }
}

public class RollbarClient {

    //--- Class Fields ---
    private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    //--- Class Methods ---
    private static string Serialize<T>(T value) => JsonSerializer.Serialize<T>(value, _serializerOptions);
    private static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _serializerOptions) ?? throw new InvalidDataException($"Data return null: {json}");

    //--- Fields ---
    private readonly HttpClient? _httpClient;
    private readonly string? _accountReadAccessToken;
    private readonly string? _accountWriteAccessToken;
    private readonly Action<string> _logInfo;

    //--- Constructors ---
    public RollbarClient(HttpClient? httpClient, string? accountReadAccessToken, string? accountWriteAccessToken, Action<string> logInfo) {
        _httpClient = httpClient;
        _accountReadAccessToken = accountReadAccessToken;
        _accountWriteAccessToken = accountWriteAccessToken;
        _logInfo = logInfo;
    }

    //--- Properties ---
    public bool HasTokens => (_accountReadAccessToken != null) && (_accountWriteAccessToken != null);
    private HttpClient HttpClient => _httpClient ?? throw new InvalidOperationException();

    //--- Methods ---
    public async Task SendRollbarPayloadAsync(Rollbar rollbar) {
        var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
            RequestUri = new Uri("https://api.rollbar.com/api/1/item/"),
            Method = HttpMethod.Post,
            Content = new StringContent(Serialize(rollbar), Encoding.UTF8, "application/json")
        });
        var result = Deserialize<RollbarResponse>(await httpResponse.Content.ReadAsStringAsync());
        if(!httpResponse.IsSuccessStatusCode) {
            throw new RollbarClientException($"SendRollbarPayloadAsync failed (Status: {httpResponse.StatusCode}, Message: {result.Message ?? "<null>"})");
        }
    }

    public async Task<RollbarProject> CreateProjectAsync(string projectName) {
        LogInfo($"create rollbar project {projectName}");
        var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
            RequestUri = new Uri("https://api.rollbar.com/api/1/projects/"),
            Method = HttpMethod.Post,
            Content = new StringContent(Serialize(new RollbarCreateProjectRequest {
                AccessToken = _accountWriteAccessToken,
                Name = projectName
            }), Encoding.UTF8, "application/json")
        });
        var result = Deserialize<RollbarResponse>(await httpResponse.Content.ReadAsStringAsync());
        if((httpResponse.StatusCode == (HttpStatusCode)422) && (result.Message == "Project with this name already exists")) {
            return await FindProjectByNameAsync(projectName) ?? throw new RollbarClientException($"could not find project: {projectName}");
        }
        if(!httpResponse.IsSuccessStatusCode) {
            throw new RollbarClientException($"http operation failed: {httpResponse.StatusCode}");
        }
        if(result.Error != 0) {
            throw new RollbarClientException($"rollbar operation failed (error {result.Error}): {result.Message}");
        }
        return Deserialize<RollbarProject>(Serialize(result.Result));
    }

    public async Task<IEnumerable<RollbarProject>> ListAllProjectsAsync() {
        LogInfo($"list all rollbar projects");
        var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
            RequestUri = new Uri($"https://api.rollbar.com/api/1/projects/?access_token={_accountReadAccessToken}"),
            Method = HttpMethod.Get
        });
        if(!httpResponse.IsSuccessStatusCode) {
            throw new RollbarClientException($"http operation failed: {httpResponse.StatusCode}");
        }
        var result = Deserialize<RollbarResponse>(await httpResponse.Content.ReadAsStringAsync());
        if(result.Error != 0) {
            throw new RollbarClientException($"rollbar operation failed (error {result.Error}): {result.Message}");
        }
        var list = Deserialize<List<RollbarProject>>(Serialize(result.Result));
        return list.Where(project => project.Name != null).ToArray();
    }

    public async Task<RollbarProject?> FindProjectByNameAsync(string projectName) {

        // Rollbar has a 32-character limit on project names
        if(projectName.Length > 32) {
            projectName = projectName.Substring(0, 32);
        }
        var allProjects = await ListAllProjectsAsync();
        return allProjects.FirstOrDefault(project => projectName.Equals(project.Name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<RollbarProject> GetProjectAsync(int projectId) {
        LogInfo($"get rollbar project {projectId}");
        var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
            RequestUri = new Uri($"https://api.rollbar.com/api/1/project/{projectId}?access_token={_accountReadAccessToken}"),
            Method = HttpMethod.Get
        });
        if(!httpResponse.IsSuccessStatusCode) {
            throw new RollbarClientException($"http operation failed: {httpResponse.StatusCode}");
        }
        var result = Deserialize<RollbarResponse>(await httpResponse.Content.ReadAsStringAsync());
        if(result.Error != 0) {
            throw new RollbarClientException($"rollbar operation failed (error {result.Error}): {result.Message}");
        }
        return Deserialize<RollbarProject>(Serialize(result.Result));
    }

    public async Task DeleteProjectAsync(int projectId) {
        LogInfo($"delete rollbar project {projectId}");
        var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
            RequestUri = new Uri($"https://api.rollbar.com/api/1/project/{projectId}?access_token={_accountWriteAccessToken}"),
            Method = HttpMethod.Delete
        });
        if(!httpResponse.IsSuccessStatusCode) {
            throw new RollbarClientException($"http operation failed: {httpResponse.StatusCode}");
        }
        var result = Deserialize<RollbarResponse>(await httpResponse.Content.ReadAsStringAsync());
        if(result.Error != 0) {
            throw new RollbarClientException($"rollbar operation failed (error {result.Error}): {result.Message}");
        }
    }

    public async Task<IEnumerable<RollbarProjectToken>> ListProjectTokensAsync(int projectId) {
        LogInfo($"list rollbar project tokens {projectId}");
        var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
            RequestUri = new Uri($"https://api.rollbar.com/api/1/project/{projectId}/access_tokens?access_token={_accountReadAccessToken}"),
            Method = HttpMethod.Get
        });
        if(!httpResponse.IsSuccessStatusCode) {
            throw new RollbarClientException($"http operation failed: {httpResponse.StatusCode}");
        }
        var result = Deserialize<RollbarResponse>(await httpResponse.Content.ReadAsStringAsync());
        if(result.Error != 0) {
            throw new RollbarClientException($"rollbar operation failed (error {result.Error}): {result.Message}");
        }
        return Deserialize<List<RollbarProjectToken>>(Serialize(result.Result));
    }

    private void LogInfo(string message) => _logInfo?.Invoke(message);
}