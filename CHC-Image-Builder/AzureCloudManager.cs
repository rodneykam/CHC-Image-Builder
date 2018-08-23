using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.RepresentationModel;
using System.Configuration;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using static CHC_Image_Builder.ImageConfiguration;
using Microsoft.Azure.Management.Compute.Fluent.Models;

namespace CHC_Image_Builder
{
    class AzureCloudManager
    {
        private string _subscriptionId { get; set; }
        private string _clientId { get; set; }
        private string _clientKey { get; set; }
        private string _tenantId { get; set; }
        private Region _location = Region.USWest;
        private IAzure _azure { get; set; }
        private ImageInfo _imageInfo { get; set; }

        public AzureCloudManager()
        {
            _subscriptionId = ConfigurationManager.AppSettings["SubscriptionId"];
            _clientId = ConfigurationManager.AppSettings["ClientId"];
            _clientKey = ConfigurationManager.AppSettings["ClientKey"];
            _tenantId = ConfigurationManager.AppSettings["TenantId"];

            if (_subscriptionId == null || _clientId == null || _clientKey == null || _tenantId == null)
            {
                try
                {
                    var yamlFile = Path.Combine(Environment.CurrentDirectory, @"authorization.yaml");
                    TextReader input = new StreamReader(yamlFile);

                    var yaml = new YamlStream();
                    yaml.Load(input);

                    // Examine the stream
                    var mapping =
                        (YamlMappingNode)yaml.Documents[0].RootNode;

                    _subscriptionId = mapping.Children[new YamlScalarNode("SubscriptionId")].ToString();
                    _clientId = mapping.Children[new YamlScalarNode("ClientId")].ToString();
                    _clientKey = mapping.Children[new YamlScalarNode("ClientKey")].ToString();
                    _tenantId = mapping.Children[new YamlScalarNode("TenantId")].ToString();
                }
                catch (Exception e)
                {
                   Logger.log.Error(e);
                    throw;
                }
               Logger.log.Debug("Azure Authorization");
               Logger.log.Debug("-------------------");
               Logger.log.DebugFormat("  SubscriptionId: {0}", _subscriptionId);
               Logger.log.DebugFormat("  ClientId: {0}", _clientId);
               Logger.log.DebugFormat("  ClientKey: {0}........", _clientKey.Substring(0, 8));
               Logger.log.DebugFormat("  TenantId: {0}", _tenantId);
            }
        }
        private void Authenticate()
        {
            try
            {
                var credentials = SdkContext.AzureCredentialsFactory
                    .FromServicePrincipal(_clientId,
                            _clientKey,
                            _tenantId,
                            AzureEnvironment.AzureGlobalCloud);

                _azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithSubscription(_subscriptionId);
            }
            catch (Exception e)
            {
               Logger.log.Error(e);
                throw;
            }

        }

        public void CreateVMImage(ImageInfo imageInfo)
        {
            _imageInfo = imageInfo;
            Authenticate();    
            CreateVM();

        }
        public void CreateVM()
        {
            try 
            {
                var guidVM = Guid.NewGuid().ToString();
                _imageInfo.GroupName = guidVM + "-rg";
               Logger.log.InfoFormat("Creating Resource Group {0}", _imageInfo.GroupName);
                var resourceGroup = _azure.ResourceGroups.Define(_imageInfo.GroupName)
                    .WithRegion(_location)
                    .Create();

               Logger.log.Info("Creating public IP address...");
                var publicIPAddress = _azure.PublicIPAddresses.Define(guidVM + "-pubip")
                    .WithRegion(_location)
                    .WithExistingResourceGroup(_imageInfo.GroupName)
                    .WithStaticIP()
                    .Create();

               Logger.log.Info("Creating virtual network...");
                var subnetName = guidVM + "-subnet"; 
                var network = _azure.Networks.Define(guidVM + "-vnet")
                    .WithRegion(_location)
                    .WithExistingResourceGroup(_imageInfo.GroupName)
                    .WithAddressSpace("10.0.0.0/16")
                    .WithSubnet(subnetName, "10.0.0.0/24")
                    .Create();

               Logger.log.Info("Creating network interface...");
                var networkInterface = _azure.NetworkInterfaces.Define(guidVM + "-NIC")
                    .WithRegion(_location)
                    .WithExistingResourceGroup(_imageInfo.GroupName)
                    .WithExistingPrimaryNetwork(network)
                    .WithSubnet(subnetName)
                    .WithPrimaryPrivateIPAddressDynamic()
                    .Create();

                _imageInfo.VMName = guidVM.Substring(0,12) + "-vm";
                Logger.log.InfoFormat("Creating virtual machine...{0}", _imageInfo.VMName);
                _azure.VirtualMachines.Define(_imageInfo.VMName)
                    .WithRegion(_location)
                    .WithExistingResourceGroup(_imageInfo.GroupName)
                    .WithExistingPrimaryNetworkInterface(networkInterface)
                    .WithLatestWindowsImage(_imageInfo.OSImage.Publisher, _imageInfo.OSImage.Offer, _imageInfo.OSImage.SKU)
                    .WithAdminUsername("azureuser")
                    .WithAdminPassword("Azure12345678")
                    .WithComputerName(_imageInfo.VMName)
                    .WithSize(_imageInfo.OSImage.VMSizeType)
                    .Create();

               Logger.log.InfoFormat("Creating virtual machine...{0} Complete!", _imageInfo.VMName);

               Logger.log.InfoFormat("Deallocating and Generalize virtual machine...{0}", _imageInfo.VMName);
                var vm = _azure.VirtualMachines.GetByResourceGroup(_imageInfo.GroupName, _imageInfo.VMName);
                vm.Deallocate();
                vm.Generalize();

               Logger.log.Info("Getting information about the virtual machine...");
               Logger.log.Info("hardwareProfile");
               Logger.log.Info("   vmSize: " + vm.Size);
               Logger.log.Info("storageProfile");
               Logger.log.Info("  imageReference");
               Logger.log.Info("    publisher: " + vm.StorageProfile.ImageReference.Publisher);
               Logger.log.Info("    offer: " + vm.StorageProfile.ImageReference.Offer);
               Logger.log.Info("    sku: " + vm.StorageProfile.ImageReference.Sku);
               Logger.log.Info("    version: " + vm.StorageProfile.ImageReference.Version);
               Logger.log.Info("  osDisk");
               Logger.log.Info("    osType: " + vm.StorageProfile.OsDisk.OsType);
               Logger.log.Info("    name: " + vm.StorageProfile.OsDisk.Name);
               Logger.log.Info("    createOption: " + vm.StorageProfile.OsDisk.CreateOption);
               Logger.log.Info("    caching: " + vm.StorageProfile.OsDisk.Caching);
               Logger.log.Info("osProfile");
               Logger.log.Info("  computerName: " + vm.OSProfile.ComputerName);
               Logger.log.Info("  adminUsername: " + vm.OSProfile.AdminUsername);
               Logger.log.Info("  provisionVMAgent: " + vm.OSProfile.WindowsConfiguration.ProvisionVMAgent.Value);
               Logger.log.Info("  enableAutomaticUpdates: " + vm.OSProfile.WindowsConfiguration.EnableAutomaticUpdates.Value);
               Logger.log.Info("networkProfile");
                foreach (string nicId in vm.NetworkInterfaceIds)
                {
                   Logger.log.Info("  networkInterface id: " + nicId);
                }
               Logger.log.Info("disks");
                foreach (DiskInstanceView disk in vm.InstanceView.Disks)
                {
                   Logger.log.Info("  name: " + disk.Name);
                   Logger.log.Info("  statuses");
                    foreach (InstanceViewStatus stat in disk.Statuses)
                    {
                       Logger.log.Info("    code: " + stat.Code);
                       Logger.log.Info("    level: " + stat.Level);
                       Logger.log.Info("    displayStatus: " + stat.DisplayStatus);
                       Logger.log.Info("    time: " + stat.Time);
                    }
                }
               Logger.log.Info("VM general status");
               Logger.log.Info("  provisioningStatus: " + vm.ProvisioningState);
               Logger.log.Info("  id: " + vm.Id);
               Logger.log.Info("  name: " + vm.Name);
               Logger.log.Info("  type: " + vm.Type);
               Logger.log.Info("  location: " + vm.Region);
               Logger.log.Info("VM instance status");
                foreach (InstanceViewStatus stat in vm.InstanceView.Statuses)
                {
                   Logger.log.Info("  code: " + stat.Code);
                   Logger.log.Info("  level: " + stat.Level);
                   Logger.log.Info("  displayStatus: " + stat.DisplayStatus);
                }

            }
            catch (Exception e)
            {
                Logger.log.Error(e);
                throw;
            }

        }
    }


}
