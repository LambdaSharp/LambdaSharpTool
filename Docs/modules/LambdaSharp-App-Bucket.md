---
title: LambdaSharp.App.Bucket - LambdaSharp Module
description: Documentation for LambdaSharp.App.Bucket module
keywords: module, app, bucket, documentation, overview
---

# Module: LambdaSharp.App.Bucket
_Version:_ [!include[LAMBDASHARP_VERSION](../version.txt)]


## Overview

The _LambdaSharp.App.Bucket_ module is used by the `App` declaration to create an S3 bucket for deploying the app files. The S3 bucket can either be configured for public access as a website or for secure access from a CloudFront distribution.


## Resource Types

This module defines no resource types.


## Parameters

<dl>

<dt><code>CloudFrontOriginAccessIdentity</code></dt>
<dd>

The <code>CloudFrontOriginAccessIdentity</code> parameter configures the S3 bucket for secure access from a CloudFront distribution. When left empty, the S3 bucket is configured as a public website instead.

<i>Required</i>: Yes

<i>Type:</i> String
</dd>

<dt><code></code></dt>
<dd>

The <code>ContentEncoding</code> parameter sets the content encoding to apply to all files copied from the zip package. The value must be one of: <code>NONE</code>, <code>BROTLI</code>, <code>GZIP</code>, or <code>DEFAULT</code>.

The encoded files are annotated with a `Content-Encoding` header matching the compression algorithm.

<i>Required</i>: Yes

<i>Type</i>: String

The <code>ContentEncoding</code> parameter must have one of the following values:
<dl>

<dt><code>NONE</code></dt>
<dd>

No content encoding is performed and no <code>Content-Encoding</code> header is applied. Using no encoding is fastest to perform, but produces significantly larger files.
</dd>

<dt><code>BROTLI</code></dt>
<dd>

Content is encoded with <a href="https://en.wikipedia.org/wiki/Brotli">Brotli compression</a> using the optimal compression setting. Brotli compression takes longer to perform, but produces smaller files.

Note that Brotli encoding is only valid for <em>https://</em> connections.
</dd>

<dt><code>GZIP</code></dt>
<dd>

Content is encoded with <a href="https://en.wikipedia.org/wiki/Gzip">Gzip compression</a>. Gzip compression is faster than Brotli, but produces slightly larger files.
</dd>

<dt><code>DEFAULT</code></dt>
<dd>

The <code>DEFAULT</code> value defaults to <code>BROTLI</code> when a non-empty <code>CloudFrontOriginAccessIdentity</code> parameter is specified since CloudFront distributions are always served over <em>https://</em> connections. Otherwise, it defaults to <code>GZIP</code>, which is safe for connections over <em>https://</em> and <em>http://</em>.
</dd>

</dl>
</dd>

<dt><code>Package</code></dt>
<dd>

The <code>Package</code> parameter is the path to the packaged app files in the deployment bucket.

<i>Required</i>: Yes

<i>Type:</i> String
</dd>

</dl>


## Output Values

<dl>

<dt><code>Arn</code></dt>
<dd>

The <code>Arn</code> output contains the ARN of the S3 bucket.

<i>Type:</i> AWS::S3::Bucket
</dd>

<dt><code>DomainName</code></dt>
<dd>

The <code>DomainName</code> output contains the IPv4 DNS name of the S3 bucket.

<i>Type:</i> String
</dd>

<dt><code>WebsiteUrl</code></dt>
<dd>

The <code>WebsiteUrl</code> output contains the Amazon S3 website endpoint of the S3 bucket.

<i>Type:</i> String
</dd>

</dl>
