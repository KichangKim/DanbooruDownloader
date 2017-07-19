using DanbooruDownloader.Sources;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DanbooruDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            IEnumerable<DownloadSource> sources = DownloadSource.GetAllSources();
            Dictionary<string, DownloadSource> sourceDictionary = new Dictionary<string, DownloadSource>();

            string defaultSourceName = null;

            foreach (DownloadSource source in sources)
            {
                sourceDictionary.Add(source.Name, source);

                if (source.IsDefault)
                {
                    defaultSourceName = source.Name;
                }
            }

            string sourceOptionArgumentTemplate = string.Join("|", sources.Select(source => source.Name).OrderBy(name => name).ToArray());

            CommandLineApplication commandLineApplication = new CommandLineApplication(true)
            {
                FullName = "Danbooru Downloader"
            };
            CommandArgument queryArgument = commandLineApplication.Argument("<query>", "Query for search.", false);
            CommandOption sourceOption = commandLineApplication.Option($"-s|--source <source> ", $"Source for downloading. This can be <{sourceOptionArgumentTemplate}>. Default is {defaultSourceName}.", CommandOptionType.SingleValue);
            CommandOption outputOption = commandLineApplication.Option("-o|--output <path>", "Output folder. Default is current folder.", CommandOptionType.SingleValue);
            CommandOption versionOption = commandLineApplication.VersionOption("-v|--version", PlatformServices.Default.Application.ApplicationVersion);
            CommandOption limitOption = commandLineApplication.Option("-l|--limit <limit>", "Limit posts count per page. It can't over 1000. Default is 1000.", CommandOptionType.SingleValue);
            CommandOption recalculateHashOption = commandLineApplication.Option("-r|--recalculate", "Recalculate MD5 hash.", CommandOptionType.NoValue);

            commandLineApplication.HelpOption("-?|-h|--help");
            commandLineApplication.OnExecute(() =>
            {
                string outputPath = outputOption.HasValue() ? outputOption.Value() : "output";
                string sourceName = sourceOption.HasValue() ? sourceOption.Value() : defaultSourceName;
                string query = queryArgument.Value;
                int limit = 1000;
                bool recalculateHash = recalculateHashOption.HasValue();
                
                if (limitOption.HasValue())
                {
                    if (!int.TryParse(limitOption.Value(), out limit))
                    {
                        Console.WriteLine("Invalid limit.");
                        commandLineApplication.ShowHint();
                        return -1;
                    }
                }
                
                if (string.IsNullOrEmpty(query))
                {
                    Console.WriteLine("Invalid query.");
                    commandLineApplication.ShowHint();
                    return -1;
                }

                if (!sourceDictionary.ContainsKey(sourceName))
                {
                    Console.WriteLine("Invalid source name : {0}", sourceName);
                    commandLineApplication.ShowHint();
                    return -1;
                }

                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                sourceDictionary[sourceName].Run(query, outputPath, limit, recalculateHash).Wait();
                
                return 0;
            });

            try
            {
                commandLineApplication.Execute(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}