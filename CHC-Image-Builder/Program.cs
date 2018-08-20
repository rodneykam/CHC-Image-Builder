using System;

namespace CHC_Image_Builder
{
    class Program
    {
        static void Main(string[] args)
        {
            var azureManager = new AzureCloudManager();
            var imageConfiguration = new ImageConfiguration();

            var info = imageConfiguration.GetImageInfo();
            imageConfiguration.ShowImageInfo(info);

            azureManager.ShowAuthorization();
            var azure = azureManager.Authenticate();
            
            var pause = Console.ReadLine();
        }
    }
}
