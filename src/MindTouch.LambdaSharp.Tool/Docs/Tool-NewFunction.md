![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - New Function Command

The `new function` command is used to add a function to an existing module. The command can either create a C# or a Javascript function using the `--language` option.

## Arguments

The `new function` command takes a single argument that specifies the function name.

```bash
lash new function MyNewFunction
```

## Options

<dl>

<dt><code>--namespace|-ns &lt;VALUE&gt;</code></dt>
<dd>(optional) Root namespace for project (default: same as function name)</dd>

<dt><code>--working-directory|-wd &lt;VALUE&gt;</code></dt>
<dd>(optional) New function project parent directory (default: current directory)</dd>

<dt><code>--framework|-f &lt;VALUE&gt;</code></dt>
<dd>(optional) Target .NET framework (default: 'netcoreapp2.1')</dd>

<dt><code>--language|-l &lt;LANGUAGE&gt;</code></dt>
<dd>(optional) Select programming language for generated code (default: csharp)</dd>

<dt><code>--use-project-reference</code></dt>
<dd>Reference LambdaSharp libraries using a project reference (default behavior when LAMBDASHARP environment variable is set)</dd>

<dt><code>--use-nuget-reference</code></dt>
<dd>Reference LambdaSharp libraries using nuget references</dd>

</dl>

## Examples

### Create a new C# function

__Using Powershell/Bash:__
```bash
dotnet lash new function MyNewFunction
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Create new LambdaSharp module or function
Created project file: MyNewFunction\MyNewFunction.csproj
Created function file: MyNewFunction\Function.cs

Done (duration: 00:00:00.0869566)
```

### Create a new Javascript function

__Using Powershell/Bash:__
```bash
dotnet lash new function --language javascript MyNewFunction
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Create new LambdaSharp module or function
Created function file: MyNewFunction\index.js

Done (duration: 00:00:00.1011796)
```
