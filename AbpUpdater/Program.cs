using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.Cli.Args;

namespace AbpUpdater
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            var parser = new CommandLineArgumentParser();
            var commandLine = parser.Parse(args);

            switch (commandLine.Command.ToLowerInvariant())
            {
                case "update":
                    await UpdateCommandAsync(commandLine.Options);
                    break;
                default:
                    System.Console.WriteLine($"Unkown command {commandLine.Command}");
                    break;
            }
        }

        private static async Task UpdateCommandAsync(AbpCommandLineOptions options)
        {
            var path = options.GetOrNull("p", "path") ?? Directory.GetCurrentDirectory();

            var connections = new Dictionary<string, string>();
            var connOptions = options.GetOrNull("c", "connections");
            if (connOptions != null)
            {
                foreach (var conn in connOptions.Split(",", StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = conn.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length != 2)
                    {
                        Console.WriteLine("Connection option format error.");
                        return;
                    }
                    connections.Add(kv[0], kv[1]);
                }
            }

            var dbprovider = options.GetOrNull("d", "dbprovider") ?? Updater.DefaultEFProvider;

            Console.WriteLine("Start update ....");
            var u = new Updater();

            bool replaceReference = options.ContainsKey("r");
            var ver = options.GetOrNull("r") ?? "0.0.0";
            await u.UpdatePackages(path, connections, ver, replaceReference, dbprovider);
            Console.WriteLine("Update command finished!");
        }
    }
}
