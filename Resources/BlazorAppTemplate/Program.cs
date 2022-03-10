using LambdaSharp.App;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<%%ROOTNAMESPACE%%.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// initialize LambdaSharp dependencies
builder.AddLambdaSharp<Program>();

// run application
await builder.Build().RunAsync();
