using System;
using System.IO;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CHC_Image_Builder
{
    class ImageConfiguration
    {
        public class ImageInfo
        {
            public string Name { get; set; }
            public string OSType { get; set; }
            public string Location { get; set; }
            public string AdminUser { get; set; }
            public string AdminPW { get; set; }
            public string ImageName { get; set; }
            public string ImageGroup { get; set; }
            public string ComputerName { get; set; }
            public string VMName { get; set; }
            public string VMPublisher { get; set; }
            public string VMOffer { get; set; }
            public string VMSKU { get; set; }
            public string VMSizeType { get; set; }
        }

        public ImageInfo GetImageInfo()
        {
            var yamlFile = Path.Combine(Environment.CurrentDirectory, @"App_Data\Image.yaml");
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
            Logger.log.DebugFormat("ImageName: {0}", imageInfo.ImageGroup);
            Logger.log.DebugFormat("ImageName: {0}", imageInfo.ComputerName);
            Logger.log.DebugFormat("Publisher: {0}", imageInfo.VMPublisher);
            Logger.log.DebugFormat("Offer: {0}", imageInfo.VMOffer);
            Logger.log.DebugFormat("SKU: {0}", imageInfo.VMSKU);
            Logger.log.DebugFormat("VMSizeType: {0}", imageInfo.VMSizeType);

            return imageInfo;
        }
    }
}
