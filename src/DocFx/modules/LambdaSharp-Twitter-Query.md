---
title: LambdaSharp.Twitter.Query - LambdaSharp Module
description: Documentation for LambdaSharp.Twitter.Query module
keywords: module, twitter, query, documentation, overview
---

# Module: LambdaSharp.Twitter.Query
_Version:_ [!include[LAMBDASHARP_VERSION](../version.txt)]

## Overview

The `LambdaSharp.Twitter.Query` module conducts a Twitter search at regular intervals and publishes found tweets to a dedicated SNS topic.

This module requires a Twitter developer account. See the [Twitter Developer Documentation](https://developer.twitter.com/en/docs/basics/getting-started) for more information.

## Resource Types

This module defines no resource types.

## Parameters

<dl>

<dt><code>TwitterApiKey</code></dt>
<dd>

The <code>TwitterApiKey</code> parameter sets the API key for accessing Twitter.

<i>Required</i>: Yes

<i>Type:</i> Secret
</dd>

<dt><code>TwitterApiSecretKey</code></dt>
<dd>

The <code>TwitterApiSecretKey</code> parameter sets the secret API key for accessing Twitter. This parameter must either be encrypted with the default deployment tier KMS key, or the corresponding KMS key must be passed in via  the <code>Secrets</code> parameter.

<i>Required</i>: Yes

<i>Type:</i> Secret
</dd>

<dt><code>TwitterLanguageFilter</code></dt>
<dd>

The <code>TwitterLanguageFilter</code> parameter is a comma-delimited list of ISO 639-1 language filters for tweets (empty value disables filter). This parameter must either be encrypted with the default deployment tier KMS key, or the corresponding KMS key must be passed in via  the <code>Secrets</code> parameter.

<i>Required</i>: No (Default: en)

<i>Type:</i> String
</dd>

<dt><code>TwitterQuery</code></dt>
<dd>

The <code>TwitterQuery</code> parameter sets the query expression for finding tweets.

<i>Required</i>: Yes

<i>Type:</i> String
</dd>

<dt><code>TwitterQueryInterval</code></dt>
<dd>

The <code>TwitterQueryInterval</code> parameter sets the interval between queries (in minutes).

<i>Required</i>: No (Default: 60)

<i>Type:</i> Number (between 2 and 1,440)
</dd>

<dt><code>TwitterSentimentFilter</code></dt>
<dd>

The <code>TwitterSentimentFilter</code> parameter sets the sentiment filter (one of: SKIP, POSITIVE, NEUTRAL, NEGATIVE, MIXED, ALL).

<b>NOTE:</b> Analyzing tweets for sentiment incurs additional costs. Please check pricing for the [AWS Comprehend Sentiment Analysis API](https://aws.amazon.com/comprehend/pricing/).

<i>Required</i>: No (Default: SKIP)

<i>Type:</i> String

The <code>TwitterSentimentFilter</code> must have one of the following values:
<dl>
<dt>ALL</dt>
<dd>

Analyze and publish all tweets.
</dd>
<dt>MIXED</dt>
<dd>

Only publish tweets with mixed sentiment.
</dd>
<dt>NEGATIVE</dt>
<dd>

Only publish tweets with negative sentiment.
</dd>
<dt>NEUTRAL</dt>
<dd>

Only publish tweets with neutral sentiment.
</dd>
<dt>POSITIVE</dt>
<dd>

Only publish tweets with positive sentiment.
</dd>
<dt>SKIP</dt>
<dd>

Publish all tweets without analyzing them.
</dd>
</dl>

</dd>

</dl>

## Output Values

<dl>

<dt><code>TweetTopic</code></dt>
<dd>

The <code>TweetTopic</code> output contains the ARN of the SNS topic to which tweets that match the <code>TwitterQuery</code> are published to.

<i>Type:</i> AWS::SNS::Topic
</dd>

</dl>

## Examples

### Use LambdaSharp.Twitter.Query to invoke a Lambda function

```yaml
- Nested: TwitterNotify
  Module: LambdaSharp.Twitter.Query
  Parameters:
    TwitterApiKey: !Ref TwitterApiKey
    TwitterApiSecretKey: !Ref TwitterApiSecretKey
    TwitterQuery: LambdaSharp

- Resource: TwitterNotifyTopic
  Type: AWS::SNS::Topic
  Allow: Subscribe
  Value: !GetAtt TwitterNotify.Outputs.TweetTopic

- Function: NotifyFunction
  Memory: 256
  Timeout: 30
  Sources:
    - Topic: TwitterNotifyTopic
```
