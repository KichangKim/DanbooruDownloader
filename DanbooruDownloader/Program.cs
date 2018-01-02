using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DanbooruDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            IDictionary<string, DataSource> sources = DataSource.GetAllAsDictionary();

            CommandLineApplication application = new CommandLineApplication(true)
            {
                FullName = "Danbooru Downloader",
            };

            application.HelpOption("-?|-h|--help");

            string defaultDataSourceName = sources.Values.Where(p => p.IsDefault).Select(p => p.Name).FirstOrDefault() ?? "none";
            
            string dataSourceOptionTemplate = string.Join(" | ", sources.Select(source => source.Key).OrderBy(key => key).ToArray());

            CommandOption sourceNameOption = application.Option($"-s|--source <name> ", $"Source for downloading. This can be <{dataSourceOptionTemplate}>. Default is {defaultDataSourceName}.", CommandOptionType.SingleValue);

            CommandOption outputPathOption = application.Option("-o|--output <path>", "Output folder. Default is current folder.", CommandOptionType.SingleValue);

            CommandOption ignoreHashCalculationOption = application.Option("-i|--ignore-hash", "Ignore MD5 hash calculation for checking updated posts.", CommandOptionType.NoValue);

            CommandOption versionOption = application.VersionOption("-v|--version", PlatformServices.Default.Application.ApplicationVersion);
            
            application.Command("tag", command =>
            {
                command.Description = "Download images by tag searching.";
                command.HelpOption("-h|--help");

                CommandArgument queryArgument = command.Argument("tags", "Search tags for Album mode.", true);

                CommandOption startPageOption = command.Option("-p|--page <number>", "Starting page number. Default is 1.", CommandOptionType.SingleValue);

                command.OnExecute(() =>
                {
                    (DataSourceContext context, DataSource source) = GetDataSourceContextAndSource(
                        sourceNameOption: sourceNameOption,
                        outputPathOption: outputPathOption,
                        ignoreHashCalculationOption: ignoreHashCalculationOption,
                        defaultDataSourceName: defaultDataSourceName,
                        sources: sources
                        );
                    
                    if (!long.TryParse(startPageOption.HasValue() ? startPageOption.Value() : "1", out context.StartPageForTag))
                    {
                        Console.WriteLine("Invalid starting page number.");
                        return -2;
                    }

                    context.QueryForTag = string.Join(" ", queryArgument.Values);

                    if (string.IsNullOrEmpty(context.QueryForTag))
                    {
                        Console.WriteLine("Invalid query.");
                        return -2;
                    }

                    context.Mode = DataSourceMode.Tag;

                    source.Run(context).Wait();

                    return 0;
                });
            });

            application.Command("dump", command =>
            {
                command.Description = "Download entire images on the server of specified source.";
                command.HelpOption("-h|--help");

                CommandOption startIdOption = command.Option("-i|--id <index>", "Starting Id. Default is 1.", CommandOptionType.SingleValue);

                command.OnExecute(() =>
                {
                    (DataSourceContext context, DataSource source) = GetDataSourceContextAndSource(
                        sourceNameOption: sourceNameOption,
                        outputPathOption: outputPathOption,
                        ignoreHashCalculationOption: ignoreHashCalculationOption,
                        defaultDataSourceName: defaultDataSourceName,
                        sources: sources
                        );
                    
                    if (!long.TryParse(startIdOption.HasValue() ? startIdOption.Value() : "1", out context.StartIdForDump))
                    {
                        Console.WriteLine("Invalid starting Id.");
                        return -2;
                    }

                    string outputPath = outputPathOption.HasValue() ? outputPathOption.Value() : ".";
                    bool recalculateHash = ignoreHashCalculationOption.HasValue();

                    context.Mode = DataSourceMode.Dump;

                    source.Run(context).Wait();
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

        static (DataSourceContext context, DataSource source) GetDataSourceContextAndSource(CommandOption sourceNameOption, CommandOption outputPathOption, CommandOption ignoreHashCalculationOption, string defaultDataSourceName, IDictionary<string, DataSource> sources)
        {
            DataSourceContext context = new DataSourceContext();

            context.OutputPath = outputPathOption.HasValue() ? outputPathOption.Value() : ".";
            context.IgnoreHashCalculation = ignoreHashCalculationOption.HasValue();

            string sourceName = sourceNameOption.HasValue() ? sourceNameOption.Value() : defaultDataSourceName;

            if (!sources.ContainsKey(sourceName))
            {
                throw new ArgumentException($"Invalid provider name : {sourceName}");
            }

            context.SourceName = sourceName;

            return (context, sources[sourceName]);
        }
    }
}
