# Doing .NET Core development with CentOS 7 in Windows Terminal

## Install CentOS 7 for WSL
1. Download the CentOS 7 image for WSL from https://github.com/yuk7/CentWSL
1. Unzip the _CentOS7.zip_ into a permanent location.
1. Run _CentOS7.exe_ to extract rootfs and register it with WSL.

## Add CentOS 7 to Windows Terminal
1. Open _Windows Terminal_ settings.
1. Add the following snippet to _Windows Terminal_ settings.
    ```json
    {
        "guid": "{6c616a81-9e47-4e27-8bdb-54bcbf829bff}",
        "hidden": false,
        "name": "CentOS7",
        "commandline": "wsl.exe -d CentOS7",
        "startingDirectory" : "C:\\"
    }
    ```

## Install .NET Core 3.1 on CentOS 7
1. Open _CentOS 7_ in _Windows Terminal_.
1. Register the Microsoft package repository.
    ```bash
    rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
    ```
1. Install .NET Core 3.1
    ```
    yum install dotnet-sdk-3.1
    ```
