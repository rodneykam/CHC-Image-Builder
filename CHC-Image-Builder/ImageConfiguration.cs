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

            Program.log.Info("Image Info");
            Program.log.Info("-----------------------------");
            Program.log.Info(string.Format("Name: {0}", imageInfo.Name));
            Program.log.Info(string.Format("OSType: {0}", imageInfo.OSType));
            Program.log.Info(string.Format("OS Image:"));
            Program.log.Info(string.Format("   Publisher: {0}", imageInfo.OSImage.Publisher));
            Program.log.Info(string.Format("   Offer: {0}", imageInfo.OSImage.Offer));
            Program.log.Info(string.Format("   SKU: {0}", imageInfo.OSImage.SKU));

            return imageInfo;
        }
    }
}
