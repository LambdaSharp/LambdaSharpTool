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

namespace LambdaSharp.CloudFormation.Converter;

using HtmlAgilityPack;
using LambdaSharp.CloudFormation.Iam;

public class IamPermissionsConverter {

    //--- Constants ---
    private const string Prefix = "https://docs.aws.amazon.com/service-authorization/latest/reference/";

    //--- Fields ---
    public readonly HttpClient _httpClient;
    private readonly Action<string>? _logInfo;
    private readonly Action<string>? _logWarn;
    private readonly Action<Exception, string>? _logError;

    //--- Constructors ---
    public IamPermissionsConverter(
        HttpClient? httpClient,
        Action<string>? logInfo,
        Action<string>? logWarn,
        Action<Exception, string>? logError
    ) {
        _httpClient = httpClient ?? new HttpClient();
        _logInfo = logInfo;
        _logWarn = logWarn;
        _logError = logError;
    }

    //--- Methods ---
    public async Task<IamServiceAuthorizationCollection> GenerateIamSpecificationAsync() {

        // NOTE (2021-02-28, bjorg): this page contains links to all sub-pages with information
        //  about "Actions, Resources, and Condition Keys" for AWS services.
        var links = await DiscoverLinksAsync(new Uri($"{Prefix}reference_policies_actions-resources-contextkeys.html"));
        LogInfo($"discovered documentation {links.Count():N0} links");
        var result = new IamServiceAuthorizationCollection {
            Services = new List<IamServiceAuthorization>()
        };

        // batch links into operations (so we don't get throttled by AWS)
        var batches = links
            .Select((link, index) => (Link: link, Batch: index / 10))
            .GroupBy(tuple => tuple.Batch)
            .ToList();

        // process all discovered links concurrently in batches
        foreach(var batch in batches) {
            IamServiceAuthorization?[] results = (await Task.WhenAll(batch.Select(async entry => {
                try {
                    return await ProcessPageAsync(entry.Link);
                } catch(Exception e) {
                    LogError(e, $"failed processing page: {entry.Link}");
                    return null;
                }
            }))) ?? new IamServiceAuthorization?[0];
            result.Services.AddRange(results.OfType<IamServiceAuthorization>());
        }

        // return services collection
        result.Services = result.Services.OrderBy(service => service.Title).ToList();
        return result;
    }

    private async Task<IEnumerable<Uri>> DiscoverLinksAsync(Uri pageUri) {

        // download contents from URL
        var document = await FetchPageAsync(pageUri);

        // analyze each link
        return document.DocumentNode
            .SelectNodes("//a[@href]")
            .Select(a => a.Attributes["href"].Value)
            .Where(href => href != null)

            // only HTML links with "/list_" in them are relevant
            .Where(href => href.Contains("/list_") && href.EndsWith(".html", StringComparison.OrdinalIgnoreCase))

            // remove query parameters and fragments from link
            .Select(href => {
                var uriBuilder = new UriBuilder(new Uri(pageUri, href)) {
                    Fragment = null,
                    Query = null
                };
                return uriBuilder.Uri;
            })

            // link must point to AWS site
            .Where(url => url.ToString().StartsWith(Prefix))

            // remove duplicates
            .Distinct()
            .ToList();
    }

    private async Task<IamServiceAuthorization> ProcessPageAsync(Uri source) {
        LogInfo($"processing: {source}");
        var result = new IamServiceAuthorization {
            Source = source.ToString()
        };
        var document = await FetchPageAsync(source);

        // read resource title
        result.Title = document.DocumentNode.SelectSingleNode("//h1")?.InnerText;
        if(result.Title != null) {
            result.Title = result.Title
                .Replace("Actions, resources, and condition keys for", "")
                .Trim();
        }

        // read resource prefix
        result.Prefix = document.DocumentNode.SelectSingleNode("//code[@class='code']").InnerText.Trim() ?? throw new ArgumentException("missing 'Prefix' in document");

        // process tables
        var tables = document.DocumentNode.SelectNodes("//table");
        foreach(var table in tables) {
            var firstColumnTitle = table.SelectSingleNode(".//th")?.InnerText.Trim() ?? "<MISSING>";
            switch(firstColumnTitle) {
            case "Actions":

                // read 'Actions' from table
                ProcessActionsTable(table, result);
                break;
            case "Resource types":

                // read 'Resource Types' table
                ProcessResourceTypesTable(table, result);
                break;
            case "Condition keys":

                // read 'Condition Keys' table
                ProcessConditionKeysTable(table, result);
                break;
            default:
                LogWarn($"unrecognized table '{firstColumnTitle}' in '{source}'");
                break;
            }
        }
        return result;

        // local functions
        void ProcessActionsTable(HtmlNode table, IamServiceAuthorization result) {

            // NOTE (2021-02-28, bjorg): the Actions table has the following format:
            //
            //  |---------|-------------|--------------|----------------|----------------|-------------------|
            //  | Actions | Description | Access level | Resource types | Condition keys | Dependent actions |
            //  |---------|-------------|--------------|----------------|----------------|-------------------|
            //
            //  * 'Actions' contains a single word that is the suffix for given permission to an IAM role.
            //  * 'Description' is ignored.
            //  * 'Access level' is one of: 'List', 'Read', 'Write', 'Permissions management', or 'Tagging'.
            //  * 'Resource types' contains zero or many resource type names, optionally followed by '*' to indicate the resource type is required.
            //  * 'Condition keys' contains zero or more keys that can be specified in a policy statement's `Condition` element.
            //  * 'Dependent actions' is ignored.
            //
            //  See: https://docs.aws.amazon.com/service-authorization/latest/reference/reference_policies_actions-resources-contextkeys.html#actions_table

            result.Actions = null;
            string? action = null;
            string? accessLevel = null;
            foreach(var row in table.SelectNodes(".//tr").Skip(1)) {
                try {
                    HtmlNodeCollection resourceTypes;
                    var conditionKeys = new List<string>();
                    var depdendentActions = new List<string>();
                    var columnCount = row.SelectNodes("td").Count();
                    switch(columnCount) {
                    case 6:

                        // all columns are present for this row
                        action = row.SelectSingleNode("td[1]")?.InnerText.Trim().Split().First();
                        accessLevel = row.SelectSingleNode("td[3]")?.InnerText.Trim();
                        resourceTypes = row.SelectNodes("td[4]/p") ?? new HtmlNodeCollection(row);
                        conditionKeys = (row.SelectNodes("td[5]//a") ?? new HtmlNodeCollection(row))
                            .Select(node => node.InnerText.Trim())
                            .ToList();
                        depdendentActions = (row.SelectNodes("td[6]//p") ?? new HtmlNodeCollection(row))
                            .Select(node => node.InnerText.Trim())
                            .ToList();
                        break;
                    case 3:

                        // partial row reuses action and access-level values from previous row
                        resourceTypes = row.SelectNodes("td[1]/p") ?? new HtmlNodeCollection(row);
                        conditionKeys = (row.SelectNodes("td[2]//a") ?? new HtmlNodeCollection(row))
                            .Select(node => node.InnerText.Trim())
                            .ToList();
                        depdendentActions = (row.SelectNodes("td[3]//p") ?? new HtmlNodeCollection(row))
                            .Select(node => node.InnerText.Trim())
                            .ToList();
                        break;
                    case 5:
                        if(action == null) {
                            continue;
                        }

                        // partial row reuses action and access-level values from previous row
                        resourceTypes = row.SelectNodes("td[3]/p") ?? new HtmlNodeCollection(row);
                        conditionKeys = (row.SelectNodes("td[4]//a") ?? new HtmlNodeCollection(row))
                            .Select(node => node.InnerText.Trim())
                            .ToList();
                        depdendentActions = (row.SelectNodes("td[5]//p") ?? new HtmlNodeCollection(row))
                            .Select(node => node.InnerText.Trim())
                            .ToList();
                        break;
                    default:
                        LogWarn($"skipping unexpected actions row ({source}): {row.InnerHtml}");
                        continue;
                    }

                    // validate row
                    if((action == null) || (accessLevel == null)) {
                        LogWarn("missing required values, skipping actions row");
                        continue;
                    }

                    // clear out empty lists
                    if(!conditionKeys.Any()) {
                        conditionKeys = null;
                    }
                    if(!depdendentActions.Any()) {
                        depdendentActions = null;
                    }

                    // create a record for each resource type
                    if(result.Actions == null) {
                        result.Actions = new List<IamServiceAction>();
                    }
                    if(resourceTypes.Any()) {
                        foreach(var resourceType in resourceTypes.Select(node => node.InnerText.Trim())) {
                            result.Actions.Add(new IamServiceAction {
                                Action = action,
                                AccessLevel = accessLevel.Split().First(),
                                ResourceType = (resourceType != "")
                                    ? resourceType.TrimEnd('*')
                                    : (string?)null,
                                Required = (resourceType != "")
                                    ? resourceType.EndsWith("*", StringComparison.Ordinal)
                                    : (bool?)null,
                                ConditionKeys = conditionKeys,
                                DepdendentActions = depdendentActions
                            });
                        }
                    } else {
                        result.Actions.Add(new IamServiceAction {
                            Action = action,
                            AccessLevel = accessLevel.Split().First(),
                            ResourceType = null,
                            Required = null,
                            ConditionKeys = conditionKeys,
                            DepdendentActions = depdendentActions
                        });
                    }
                } catch(Exception e) {
                    LogError(e, "error encountered, skipping actions row");

                    // reset carry-over values
                    action = null;
                    accessLevel = null;
                }
            }
        }

        void ProcessResourceTypesTable(HtmlNode table, IamServiceAuthorization result) {

            // NOTE (2021-02-28, bjorg): the Resource Types table has the following format:
            //
            //  |----------------|-----|----------------|
            //  | Resource types | ARN | Condition keys |
            //  |----------------|-----|----------------|
            //
            //  * 'Resource types' contains the resource type name.
            //  * 'ARN' contains the ARN pattern (e.g. `arn:${Partition}:kendra:${Region}:${Account}:index/${IndexId}/data-source/${DataSourceId}`)
            //  * 'Condition keys' contains zero or more keys that can be specified in a policy statement's `Condition` element.
            //
            //  See: https://docs.aws.amazon.com/service-authorization/latest/reference/reference_policies_actions-resources-contextkeys.html#resources_table

            result.ResourcesTypes = null;
            foreach(var row in table.SelectNodes(".//tr").Skip(1)) {
                try {
                    var columnCount = row.SelectNodes("td").Count();
                    if(columnCount != 3) {
                        LogWarn($"skipping unexpected resource types row ({source}): {row.InnerHtml}");
                        continue;
                    }
                    var resourceType = row.SelectSingleNode("td[1]")?.InnerText.Trim().Split().First();
                    var arn = row.SelectSingleNode("td[2]")?.InnerText.Trim().Split().First();
                    var conditionKeys = (row.SelectNodes("td[3]//a") ?? new HtmlNodeCollection(row))
                            .Select(node => node.InnerText.Trim())
                            .ToList();

                    // validate row
                    if((resourceType == null) || (arn == null)) {
                        LogWarn("missing required values, skipping resource types row");
                        continue;
                    }

                    // clear out empty lists
                    if(!conditionKeys.Any()) {
                        conditionKeys = null;
                    }
                    if(result.ResourcesTypes == null) {
                        result.ResourcesTypes = new List<IamServiceResourceTypes>();
                    }
                    result.ResourcesTypes.Add(new IamServiceResourceTypes {
                        ResourceType = resourceType,
                        Arn = arn,
                        ConditionKeys = conditionKeys
                    });
                } catch(Exception e) {
                    LogError(e, "error encountered, skipping resources types row");
                }
            }
        }

        void ProcessConditionKeysTable(HtmlNode table, IamServiceAuthorization result) {

            // NOTE (2021-02-28, bjorg): the Condition Keys table has the following format:
            //
            //  |----------------|-------------|------|
            //  | Condition keys | Description | Type |
            //  |----------------|-------------|------|
            //
            //  * 'Condition keys' contains the condition key name.
            //  * 'Description' is ignored.
            //  * 'Type' is describes the value type for the condition key.
            //
            //  See: https://docs.aws.amazon.com/service-authorization/latest/reference/reference_policies_actions-resources-contextkeys.html#context_keys_table

            result.ConditionKeys = null;
            foreach(var row in table.SelectNodes(".//tr").Skip(1)) {
                try {
                    var columnCount = row.SelectNodes("td").Count();
                    if(columnCount != 3) {
                        LogWarn($"skipping unexpected condition keys row ({source}): {row.InnerHtml}");
                        continue;
                    }
                    var conditionKey = row.SelectSingleNode("td[1]")?.InnerText.Trim().Split().First();
                    var type = row.SelectSingleNode("td[3]")?.InnerText.Trim().Split().First();

                    // validate row
                    if((conditionKey == null) || (type == null)) {
                        LogWarn("missing required values, skipping condition keys row");
                        continue;
                    }
                    if(result.ConditionKeys == null) {
                        result.ConditionKeys = new List<IamServiceConditionKey>();
                    }
                    result.ConditionKeys.Add(new IamServiceConditionKey {
                        ConditionKey = conditionKey,
                        Type = type
                    });
                } catch(Exception e) {
                    LogError(e, "error encountered, skipping condition keys row");
                }
            }
        }
    }

    private async Task<HtmlDocument> FetchPageAsync(Uri pageUri) {
        var document = new HtmlDocument();
        var response = await _httpClient.GetAsync(pageUri);
        response.EnsureSuccessStatusCode();
        document.LoadHtml(await response.Content.ReadAsStringAsync());
        return document;
    }

    private void LogInfo(string message) => _logInfo?.Invoke(message);
    private void LogWarn(string message) => _logWarn?.Invoke(message);
    private void LogError(Exception exception, string message) => _logError?.Invoke(exception, message);
}
