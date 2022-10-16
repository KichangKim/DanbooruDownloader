using DanbooruDownloader.Commands;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.PlatformAbstractions;
using System;

namespace DanbooruDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLineApplication application = new CommandLineApplication(true)
            {
                FullName = "Danbooru Downloader",
            };

            application.HelpOption("-?|-h|--help");

            CommandOption versionOption = application.VersionOption("-v|--version", PlatformServices.Default.Application.ApplicationVersion);

            application.Command("dump", command =>
            {
                command.Description = "Download entire images on the server of specified source.";
                command.HelpOption("-h|--help");

                var outputPathArgument = command.Argument("path", "Output path.", false);
                var startIdOption = command.Option("-s|--start-id <id>", "Starting Id. Default is 1.", CommandOptionType.SingleValue);
                var endIdOption = command.Option("-e|--end-id <id>", "Ending Id. Default is 0 (unlimited).).", CommandOptionType.SingleValue);
                var ignoreHashCheckOption = command.Option("-i|--ignore-hash-check", "Ignore hash check.", CommandOptionType.NoValue);
                var includeDeletedOption = command.Option("-d|--deleted", "Include deleted posts.", CommandOptionType.NoValue);
                var usernameOption = command.Option("--username", "Username of Danbooru account.", CommandOptionType.SingleValue);
                var apikeyOption = command.Option("--api-key", "API key of Danbooru account.", CommandOptionType.SingleValue);

                command.OnExecute(() =>
                {
                    string path = outputPathArgument.Value;
                    long startId = 1;
                    long endId = 0;
                    bool ignoreHashCheck = ignoreHashCheckOption.HasValue();
                    bool includeDeleted = includeDeletedOption.HasValue();

                    if (startIdOption.HasValue() && !long.TryParse(startIdOption.Value(), out startId))
                    {
                        Console.WriteLine("Invalid start id.");
                        return -2;
                    }

                    if (endIdOption.HasValue() && !long.TryParse(endIdOption.Value(), out endId))
                    {
                        Console.WriteLine("Invalid end id.");
                        return -2;
                    }

                    if (!usernameOption.HasValue() || !apikeyOption.HasValue())
                    {
                        Console.WriteLine("You must specify username and api key.");
                        return -2;
                    }

                    var username = usernameOption.Value();
                    var apikey = apikeyOption.Value();

                    DumpCommand.Run(path, startId, endId, ignoreHashCheck, includeDeleted, username, apikey).Wait();

                    return 0;
                });
            });

            application.OnExecute(() =>
            {
                application.ShowHint();

                return 0;
            });

            try
            {
                int exitCode = application.Execute(args);

                if (exitCode == -2)
                {
                    application.ShowHint();
                }

                Environment.ExitCode = exitCode;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Environment.ExitCode = -1;
            }
        }
    }
}
