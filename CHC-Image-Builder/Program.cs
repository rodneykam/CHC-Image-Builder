using System;

namespace CHC_Image_Builder
{
    class Program
    {
        static void Main(string[] args)
        {
            var imageConfiguration = new ImageConfiguration();

            var info = imageConfiguration.GetImageInfo();
            imageConfiguration.ShowImageInfo(info);

            var pause = Console.ReadLine();
        }
    }
}
