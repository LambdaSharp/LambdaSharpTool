![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# Twitter Notifier
_Version_: 1.0-DEV

## Overview

This module runs a Twitter search query every hour and sends any found tweets to an email address over an SNS topic.

## Prerequisite

1. The [LambdaSharp CLI](https://github.com/LambdaSharp/LambdaSharpTool)
1. Twitter developer account with a Consumer Key (API Key) and a Consumer Secret (API Secret). [See setup instructions here.](https://developer.twitter.com/en/docs/basics/getting-started)

## Parameters

<dl>

<dt><code>TwitterApiKey</code></dt>
<dd>

The <code>TwitterApiKey</code> parameter sets the encrypted Twitter API key.

<i>Required</i>: Yes

<i>Type:</i> Secret
</dd>

<dt><code>TwitterApiSecretKey</code></dt>
<dd>

The <code>TwitterApiSecretKey</code> parameter sets the encrypted Twitter secret API key.

<i>Required</i>: Yes

<i>Type:</i> Secret
</dd>

<dt><code>TwitterLanguageFilter</code></dt>
<dd>

The <code>TwitterLanguageFilter</code> parameter sets the language filter for tweets (empty value disables filter).

<i>Required</i>: Yes

<i>Type:</i> String
</dd>

<dt><code>TwitterQuery</code></dt>
<dd>

The <code>TwitterQuery</code> parameter sets search query for finding tweets

<i>Required</i>: Yes

<i>Type:</i> String
</dd>

<dt><code>NotificationEmail</code></dt>
<dd>

The <code>NotificationEmail</code> parameter sets the notification email for found tweets.

<i>Required</i>: Yes

<i>Type:</i> String
</dd>

</dl>
