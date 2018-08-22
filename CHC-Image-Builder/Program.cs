using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;

namespace CHC_Image_Builder
{
    class Program
    {
        public static readonly log4net.ILog log =
                log4net.LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {

            XmlDocument log4netConfig = new XmlDocument();
            var logPath = Path.Combine(Environment.CurrentDirectory, @"log4net.config");
            log4netConfig.Load(File.OpenRead(logPath));

            var repo = log4net.LogManager.CreateRepository(
                Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));

            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Console.WriteLine("Application - Main is invoked");

            var azureManager = new AzureCloudManager();
            var imageConfiguration = new ImageConfiguration();

            var info = imageConfiguration.GetImageInfo();
            var status = azureManager.CreateVMImage(info);

            Console.WriteLine("Application - Main has completed");
            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
        }

    }
}
