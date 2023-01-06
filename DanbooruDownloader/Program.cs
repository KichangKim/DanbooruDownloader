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

                CommandArgument outputPathArgument = command.Argument("path", "Output path.", false);
                CommandOption startIdOption = command.Option("-s|--start-id <id>", "Starting Id. Default is 1.", CommandOptionType.SingleValue);
                CommandOption parallelDownloadsOption = command.Option("-p|--parallel-downloads <value>", "Number of images to download simultaneously. Default is 5.", CommandOptionType.SingleValue);
                CommandOption ignoreHashCheckOption = command.Option("-i|--ignore-hash-check", "Ignore hash check.", CommandOptionType.NoValue);
                CommandOption includeDeletedOption = command.Option("-d|--deleted", "Include deleted posts.", CommandOptionType.NoValue);


                command.OnExecute(() =>
                {
                    string path = outputPathArgument.Value;
                    long startId = 1;
                    int parallelDownloads = 5;

                    bool ignoreHashCheck = ignoreHashCheckOption.HasValue();
                    bool includeDeleted = includeDeletedOption.HasValue();

                    if (startIdOption.HasValue() && !long.TryParse(startIdOption.Value(), out startId))
                    {
                        Console.WriteLine("Invalid start id.");
                        return -2;
                    }
                    if (parallelDownloadsOption.HasValue() && !int.TryParse(parallelDownloadsOption.Value(), out parallelDownloads))
                    {
                        Console.WriteLine("Invalid number of parallel downloads.");
                        return -2;
                    }

                    DumpCommand.Run(path, startId, parallelDownloads, ignoreHashCheck, includeDeleted).Wait();

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
