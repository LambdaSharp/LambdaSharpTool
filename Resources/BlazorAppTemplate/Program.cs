using System.Threading.Tasks;
using LambdaSharp.App;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace %%ROOTNAMESPACE%% {

    public class Program {

        //--- Class Methods ---
        public static async Task Main(string[] args) {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            // initialize LambdaSharp dependencies
            builder.AddLambdaSharp<Program>();

            // run application
            var host = builder.Build();
            await host.RunAsync();
        }
    }
}
