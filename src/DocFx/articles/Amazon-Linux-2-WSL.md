# Doing .NET Core development with Amazon Linux 2 in Windows Terminal

## Install Amazon Linux 2 for WSL
1. Download the Amazon Linux 2 image for WSL from https://github.com/yosukes-dev/AmazonWSL
1. Unzip the _Amazon2.zip_ into a permanent location.
1. Run _Amazon.exe_ to extract rootfs and register it with WSL.

## Add Amazon Linux 2 to Windows Terminal
1. Open _Windows Terminal_ settings.
1. Add the following snippet to _Windows Terminal_ settings.
    ```json
    {
        "guid": "{3dffc929-1f2e-44cc-8253-9635e0298f6b}",
        "hidden": false,
        "name": "Amazon Linux 2",
        "commandline": "wsl.exe -d Amazon2"
    }
    ```

## Install .NET Core 3.1 on Amazon Linux 2
1. Open the _Amazon Linux 2_ in _Windows Terminal_.
1. Register the Microsoft package repository.
    ```bash
    rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
    ```
1. Install .NET Core 3.1
    ```
    yum install dotnet-sdk-3.1
    ```
## TODO: mount C:\ drive
Need to figure out how to mount the C:\ drive on the Amazon Linux 2 in WSL.