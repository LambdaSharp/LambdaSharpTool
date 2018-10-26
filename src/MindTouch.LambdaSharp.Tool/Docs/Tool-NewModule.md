![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp Tool - New Module Command

The `new module` command is used to create a new module file.

## Arguments

The `new module` command takes a single argument that specifies the module name.

```bash
lash new module MyNewModule
```

## Options

<dl>

<dt><code>--working-directory|-wd &lt;PATH&gt;</code></dt>
<dd>(optional) New module directory (default: current directory)</dd>

</dl>

## Examples

### Create a new module in the current folder

__Using Powershell/Bash:__
```bash
dotnet lash new module MyNewModule
```

Output:
```
MindTouch LambdaSharp Tool (v0.4) - Create new LambdaSharp module or function
Created module file: Module.yml

Done (duration: 00:00:00.0168295)
```
