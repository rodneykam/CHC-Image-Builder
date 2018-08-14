using System;
using System.IO;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CHC_Image_Builder
{
    class ImageConfiguration
    {
        public class OSImageInfo
        {
            public string Publisher { get; set; }
            public string Offer { get; set; }
            public string SKU { get; set; }
        }

        public class ImageInfo
        {
            public string Name { get; set; }
            public string OSType { get; set; }
            public OSImageInfo OSImage { get; set; }
        }

        public ImageInfo GetImageInfo()
        {
            var yamlFile = Path.Combine(Environment.CurrentDirectory, @"image.yaml");
            TextReader input = new StreamReader(yamlFile);

            var deserializer = new DeserializerBuilder()
                .Build();

            var imageInfo = deserializer.Deserialize<ImageInfo>(input);

            return imageInfo;
        }

        public void ShowImageInfo(ImageInfo imageInfo)
        {
            Console.WriteLine("Image Info");
            Console.WriteLine("-----------------------------");
            Console.WriteLine("Name: {0}", imageInfo.Name);
            Console.WriteLine("OSType: {0}", imageInfo.OSType);
            Console.WriteLine("OS Image:");
            Console.WriteLine("   Publisher: {0}", imageInfo.OSImage.Publisher);
            Console.WriteLine("   Offer: {0}", imageInfo.OSImage.Offer);
            Console.WriteLine("   SKU: {0}", imageInfo.OSImage.SKU);
        }
    }
}
