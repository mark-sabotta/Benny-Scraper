﻿using Autofac;
using Benny_Scraper.BusinessLogic;
using Benny_Scraper.BusinessLogic.Interfaces;
using Benny_Scraper.BusinessLogic.Services.Interface;
using Benny_Scraper.DataAccess.DbInitializer;
using Microsoft.Extensions.Configuration;
using NLog;

namespace Benny_Scraper
{
    internal class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static IContainer Container { get; set; }
        // Added Task to Main in order to avoid "Program does not contain a static 'Main method suitable for an entry point"
        static async Task Main(string[] args)
        {
            var configuration = BuildConfiguration();
            // Pass the built configuration to the StartUp class
            var startUp = new StartUp(configuration);

            // Database Injections
            var builder = new ContainerBuilder();
            startUp.ConfigureServices(builder);

            Container = builder.Build();

            SetupLogger();

            if (args.Length > 0)
            {
                await RunAsync(args);
            }
            else
            {
                await RunAsync();
            }
        }

        private static async Task RunAsync()
        {
            using (var scope = Container.BeginLifetimeScope())
            {
                var logger = NLog.LogManager.GetCurrentClassLogger();
                logger.Info("Hello from NLog!");
                Logger.Info("Initializing Database");
                IDbInitializer dbInitializer = scope.Resolve<IDbInitializer>();
                dbInitializer.Initialize();
                Logger.Info("Database Initialized");

                IEpubGenerator epubGenerator = scope.Resolve<IEpubGenerator>();
                //epubGenerator.ValidateEpub(@"C:\Users\Emiya\Documents\BennyScrapedNovels\SUPREMACY GAMES\Read Supremacy Games\supremacy games.epub");

                INovelProcessor novelProcessor = scope.Resolve<INovelProcessor>();

                // Uri help https://www.dotnetperls.com/uri#:~:text=URI%20stands%20for%20Universal%20Resource,strings%20starting%20with%20%22http.%22
                Uri novelTableOfContentUri = new Uri("https://novelfull.com/supremacy-games.html");

                try
                {
                    await novelProcessor.ProcessNovelAsync(novelTableOfContentUri);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception when trying to process novel. {ex}");
                }
            }
        }

        // create private RunAsync that accepts args and then call it from Main, also make it so that args are used, accepting multiple arguments 'clear_database' should be an argument that will clear the database using the removeall method. Case statement should be used to check for the argument and then call the removeall method.
        private static async Task RunAsync(string[] args)
        {
            using (var scope = Container.BeginLifetimeScope())
            {
                var logger = NLog.LogManager.GetCurrentClassLogger();
                switch (args[0])
                {
                    case "clear_database":
                        INovelService novelService = scope.Resolve<INovelService>();
                        await novelService.RemoveAllAsync();
                        break;
                    default:
                        break;
                }
            }
        }

        private static void SetupLogger()
        {
            var config = new NLog.Config.LoggingConfiguration();
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = @"C:\logs\BennyScraper.log" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            NLog.LogManager.Configuration = config;
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            // Build the configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            return configuration;
        }
    }
}