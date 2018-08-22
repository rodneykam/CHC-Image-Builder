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
            public string VMSizeType { get; set; }
        }

        public class ImageInfo
        {
            public string Name { get; set; }
            public string OSType { get; set; }
            public string AdminUser { get; set; }
            public string AdminPW { get; set; }
            public OSImageInfo OSImage { get; set; }
        }

        public ImageInfo GetImageInfo()
        {
            var yamlFile = Path.Combine(Environment.CurrentDirectory, @"image.yaml");
            TextReader input = new StreamReader(yamlFile);

            var deserializer = new DeserializerBuilder()
                .Build();

           var imageInfo = deserializer.Deserialize<ImageInfo>(input);

            Program.log.Debug("Image Info");
            Program.log.Debug("-----------------------------");
            Program.log.DebugFormat("Name: {0}", imageInfo.Name);
            Program.log.DebugFormat("OSType: {0}", imageInfo.OSType);
            Program.log.DebugFormat("AdminUser: {0}", imageInfo.AdminUser);
            Program.log.DebugFormat("AdminPW: {0}", imageInfo.AdminPW);
            Program.log.DebugFormat("OS Image:");
            Program.log.DebugFormat("   Publisher: {0}", imageInfo.OSImage.Publisher);
            Program.log.DebugFormat("   Offer: {0}", imageInfo.OSImage.Offer);
            Program.log.DebugFormat("   SKU: {0}", imageInfo.OSImage.SKU);
            Program.log.DebugFormat("   VMSizeType: {0}", imageInfo.OSImage.VMSizeType);

            return imageInfo;
        }
    }
}
