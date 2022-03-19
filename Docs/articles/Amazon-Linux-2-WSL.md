---
title: LambdaSharp with Amazon Linux 2 in Windows Terminal
description: Tutorial on how to setup WSL with Amazon Linux 2
keywords: tutorial, wsl, linux, terminal
---

# Doing LambdaSharp development with Amazon Linux 2 in Windows Terminal

LambdaSharp builds [_ReadyToRun_](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-core-3-0#readytorun-images)  packages when used on _Amazon Linux 2_. This can be achieved by either using an EC2 Linux instance, AWS CodeBuild, or the _Windows Subsystem for Linux_ (WSL). This article describes how to enable and configure WSL for building LambdaSharp modules.

The benefit of _ReadyToRun_ packages is reduced cold start time for Lambda functions, as the assemblies contain pre-compiled native code. However, the pre-compiled native code does not replace the original .NET code. Therefore, _ReadyToRun_ packages are larger than their unoptimized counterparts.

## Install Amazon Linux 2 for WSL

1. [Enable WSL on Windows 10.](https://docs.microsoft.com/en-us/windows/wsl/install-win10)
1. Download the _Amazon Linux 2_ image for WSL from https://github.com/yosukes-dev/AmazonWSL
1. Unzip the _Amazon2.zip_ into a permanent location.
1. Run _Amazon.exe_ to extract _rootfs_ and register it with WSL.

## Add Amazon Linux 2 to Windows Terminal

[Windows Terminal](https://www.microsoft.com/en-us/p/windows-terminal-preview/9n0dx20hk701) is a terminal emulator for Windows 10 written by Microsoft. It includes support for the Command Prompt, PowerShell, WSL, SSH, and more. The following settings make it trivial to open a `bash` shell directly in _Amazon Linux 2_.

1. Open _Windows Terminal_ settings.
1. Add the following snippet to _Windows Terminal_ settings. The icon path is assuming _Amazon2.zip_ was extracted into _C:\Amazon2_ folder.
    ```json
    {
        "guid": "{3dffc929-1f2e-44cc-8253-9635e0298f6b}",
        "hidden": false,
        "name": "Amazon Linux 2",
        "commandline": "wsl.exe -d Amazon2",
        "startingDirectory" : "C:\\",
        "icon": "C:\\Amazon2\\assets\\AWS-icon.png"
    }
    ```

## Install LambdaSharp on Amazon Linux 2

The following steps install .NET Core 3.1, some utilities, and LambdaSharp.

1. Open _Amazon Linux 2_ in _Windows Terminal_.
1. Register the Microsoft package repository.
    ```bash
    rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
    ```
1. Install .NET 6, .NET Core 3.1, and misc. required utilities
    ```bash
    yum install -y dotnet-sdk-6.0 dotnet-runtime-6.0 dotnet-sdk-3.1 dotnet-runtime-3.1 git zip
    ```
1. Install LambdaSharp
    ```bash
    dotnet tool install --global LambdaSharp.Tool
    ```

## (Optional) Install VS Code Remote

Visual Studio Code supports [remote development](https://code.visualstudio.com/docs/remote/remote-overview), which allows files to be edited from the VS Code in Windows, while all commands are executed on _Amazon Linux 2_.

1. Open _Amazon Linux 2_ in _Windows Terminal_.
1. Install utilities required by _VS Code Remote_ extension
    ```bash
    yum install -y wget glibc libgcc libstdc++ python ca-certificates tar
    ```
1. Invoke `code` command to trigger the installation of the _VS Code Remote_ extension
    ```bash
    code
    ```
1. Click _Allow Access_ when prompted by _Windows Defender_.
