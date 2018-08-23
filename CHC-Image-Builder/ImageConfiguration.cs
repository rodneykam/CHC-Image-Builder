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
            public string ImageName { get; set; }
            public string VMName { get; set; }
            public string GroupName { get; set; }
            public OSImageInfo OSImage { get; set; }
        }

        public ImageInfo GetImageInfo()
        {
            var yamlFile = Path.Combine(Environment.CurrentDirectory, @"image.yaml");
            TextReader input = new StreamReader(yamlFile);

            var deserializer = new DeserializerBuilder()
                .Build();

           var imageInfo = deserializer.Deserialize<ImageInfo>(input);

            Logger.log.Debug("Image Info");
            Logger.log.Debug("-----------------------------");
            Logger.log.DebugFormat("Name: {0}", imageInfo.Name);
            Logger.log.DebugFormat("OSType: {0}", imageInfo.OSType);
            Logger.log.DebugFormat("AdminUser: {0}", imageInfo.AdminUser);
            Logger.log.DebugFormat("AdminPW: {0}", imageInfo.AdminPW);
            Logger.log.DebugFormat("ImageName: {0}", imageInfo.ImageName);
            Logger.log.DebugFormat("OS Image:");
            Logger.log.DebugFormat("   Publisher: {0}", imageInfo.OSImage.Publisher);
            Logger.log.DebugFormat("   Offer: {0}", imageInfo.OSImage.Offer);
            Logger.log.DebugFormat("   SKU: {0}", imageInfo.OSImage.SKU);
            Logger.log.DebugFormat("   VMSizeType: {0}", imageInfo.OSImage.VMSizeType);

            return imageInfo;
        }
    }
}
