﻿using Benny_Scraper.DataAccess.Data;
using Benny_Scraper.DataAccess.DbInitializer;
using Benny_Scraper.DataAccess.Repository;
using Benny_Scraper.DataAccess.Repository.IRepository;
using Benny_Scraper.Models;
using log4net;
using log4net.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

//[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]


namespace Benny_Scraper
{
    internal class Program
    {
        //private static readonly ILog logger = LogManager.GetLogger(typeof(Program));
        //private const string _connectionString = "Server=localhost;Database=Test;TrustServerCertificate=True;Trusted_Connection=True;";
        // Added Task to Main in order to avoid "Program does not contain a static 'Main method suitable for an entry point"
        private readonly IStartUpService _startUpService;
        static async Task Main(string[] args)
        {
            // Database Injections https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-usage
            using IHost host = Host.CreateDefaultBuilder(args)
               .ConfigureServices(services =>
               {
                   // Services here
                   new Startup().ConfigureServices(services);
               }).Build();

            ExemplifyServiceLifetime(host.Services, "Lifetime 1");
            ExemplifyServiceLifetime(host.Services, "Lifetime 2");

            await host.RunAsync();


            static void ExemplifyServiceLifetime(IServiceProvider hostProvider, string lifetime)
            {
                using IServiceScope serviceScope = hostProvider.CreateScope();
                
                IServiceProvider provider = serviceScope.ServiceProvider;                
                IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
                IDbInitializer dbInitializer = provider.GetRequiredService<IDbInitializer>();
                dbInitializer.Initialize();
                IStartUpService startUp1 = provider.GetRequiredService<IStartUpService>();

                //startUp.ReportServiceLifetimeDetails(
                //    $"{lifetime}: Call 1 to provider.GetRequiredService<ServiceLifetimeLogger>()");

                Console.WriteLine("...");
                var novel = new Novel
                {
                    Title = "Test",
                    Url = @"https://novelfull.com",
                    DateCreated = DateTime.Now,
                    SiteName = "novelfull",
                    ChapterName = "Test",
                    ChapterNumber = 1
                };

                //IStartUp startUp = new StartUp(unitOfWork);
                startUp1.CreateNovel(novel);
            }

            IDriverFactory driverFactory = new DriverFactory(); // Instantiating an interface https://softwareengineering.stackexchange.com/questions/167808/instantiating-interfaces-in-c
            //Task<IWebDriver> driver = driverFactory.CreateDriverAsync(1, false, "https://www.deviantart.com/blix-kreeg");
            //Task<IWebDriver> driver2 = driverFactory.CreateDriverAsync(1, false, "https://www.google.com");
            Task<IWebDriver> driver3 = driverFactory.CreateDriverAsync(1, false, "https://novelfull.com/paragon-of-sin.html");
            NovelPage novelPage = new NovelPage(driver3.Result);
            var title = novelPage.GetTitle(".title");
            var latestChapter = novelPage.GetLatestChapter(".l-chapters a span.chapter-text");
            List<string> chaptersUrl = novelPage.GetChapterUrls("list-chapter");
            string lastChapterUrl = novelPage.GetLastTableOfContentPageUrl("last");
            int lastChapterNumber = Regex.Match(lastChapterUrl, @"\d+").Success ? Convert.ToInt32(Regex.Match(lastChapterUrl, @"\d+").Value) : 0;
            List<string> chapters = novelPage.GetChaptersUsingPagitation(20, lastChapterNumber);
            var first = chapters.First();
            driverFactory.DisposeAllDrivers();
        }        
        
        private static void ConfigureLogger() {
            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<Program>();

                })
                .ConfigureLogging(logBuilder =>
                {
                    logBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    logBuilder.AddLog4Net("log4net.config");

                }).UseConsoleLifetime();
        }
    }
}