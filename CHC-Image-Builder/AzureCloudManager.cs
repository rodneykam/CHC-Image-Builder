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
                    Program.log.Error(e);
                    throw;
                }
                Program.log.Debug("Azure Authorization");
                Program.log.Debug("-------------------");
                Program.log.DebugFormat("  SubscriptionId: {0}", _subscriptionId);
                Program.log.DebugFormat("  ClientId: {0}", _clientId);
                Program.log.DebugFormat("  ClientKey: {0}........", _clientKey.Substring(0, 8));
                Program.log.DebugFormat("  TenantId: {0}", _tenantId);
            }
        }
        private IAzure Authenticate()
        {
            IAzure azure;
            try{
                var credentials = SdkContext.AzureCredentialsFactory
                    .FromServicePrincipal(_clientId,
                            _clientKey,
                            _tenantId,
                            AzureEnvironment.AzureGlobalCloud);

                azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithSubscription(_subscriptionId);
            }
            catch (Exception e)
            {
                Program.log.Error(e);
                throw;
            }

            return azure;
        }

        public bool CreateVMImage(ImageInfo imageInfo)
        {
            var azure = Authenticate();

            try 
            {
                var guidVM = Guid.NewGuid().ToString();
                var groupName = guidVM + "-rg";
                Program.log.InfoFormat("Creating Resource Group {0}", groupName);
                var resourceGroup = azure.ResourceGroups.Define(groupName)
                    .WithRegion(_location)
                    .Create();

                Program.log.Info("Creating public IP address...");
                var publicIPAddress = azure.PublicIPAddresses.Define(guidVM + "-pubip")
                    .WithRegion(_location)
                    .WithExistingResourceGroup(groupName)
                    .WithStaticIP()
                    .Create();

                Program.log.Info("Creating virtual network...");
                var subnetName = guidVM + "-subnet"; 
                var network = azure.Networks.Define(guidVM + "-vnet")
                    .WithRegion(_location)
                    .WithExistingResourceGroup(groupName)
                    .WithAddressSpace("10.0.0.0/16")
                    .WithSubnet(subnetName, "10.0.0.0/24")
                    .Create();

                Program.log.Info("Creating network interface...");
                var networkInterface = azure.NetworkInterfaces.Define(guidVM + "-NIC")
                    .WithRegion(_location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetwork(network)
                    .WithSubnet(subnetName)
                    .WithPrimaryPrivateIPAddressDynamic()
                    .Create();

                var vmName = guidVM.Substring(0,12) + "-vm";
                Program.log.InfoFormat("Creating virtual machine...{0}", vmName);
                azure.VirtualMachines.Define(vmName)
                    .WithRegion(_location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetworkInterface(networkInterface)
                    .WithLatestWindowsImage(imageInfo.OSImage.Publisher, imageInfo.OSImage.Offer, imageInfo.OSImage.SKU)
                    .WithAdminUsername("azureuser")
                    .WithAdminPassword("Azure12345678")
                    .WithComputerName(vmName)
                    .WithSize(imageInfo.OSImage.VMSizeType)
                    .Create();

                Program.log.InfoFormat("Creating virtual machine...{0} Complete!", vmName);

                Program.log.InfoFormat("Deallocating virtual machine...{0}", vmName);
                var vm = azure.VirtualMachines.GetByResourceGroup(groupName, vmName);
                vm.Deallocate();

                Program.log.Info("Getting information about the virtual machine...");
                Program.log.Info("hardwareProfile");
                Program.log.Info("   vmSize: " + vm.Size);
                Program.log.Info("storageProfile");
                Program.log.Info("  imageReference");
                Program.log.Info("    publisher: " + vm.StorageProfile.ImageReference.Publisher);
                Program.log.Info("    offer: " + vm.StorageProfile.ImageReference.Offer);
                Program.log.Info("    sku: " + vm.StorageProfile.ImageReference.Sku);
                Program.log.Info("    version: " + vm.StorageProfile.ImageReference.Version);
                Program.log.Info("  osDisk");
                Program.log.Info("    osType: " + vm.StorageProfile.OsDisk.OsType);
                Program.log.Info("    name: " + vm.StorageProfile.OsDisk.Name);
                Program.log.Info("    createOption: " + vm.StorageProfile.OsDisk.CreateOption);
                Program.log.Info("    caching: " + vm.StorageProfile.OsDisk.Caching);
                Program.log.Info("osProfile");
                Program.log.Info("  computerName: " + vm.OSProfile.ComputerName);
                Program.log.Info("  adminUsername: " + vm.OSProfile.AdminUsername);
                Program.log.Info("  provisionVMAgent: " + vm.OSProfile.WindowsConfiguration.ProvisionVMAgent.Value);
                Program.log.Info("  enableAutomaticUpdates: " + vm.OSProfile.WindowsConfiguration.EnableAutomaticUpdates.Value);
                Program.log.Info("networkProfile");
                foreach (string nicId in vm.NetworkInterfaceIds)
                {
                    Program.log.Info("  networkInterface id: " + nicId);
                }
                Program.log.Info("disks");
                foreach (DiskInstanceView disk in vm.InstanceView.Disks)
                {
                    Program.log.Info("  name: " + disk.Name);
                    Program.log.Info("  statuses");
                    foreach (InstanceViewStatus stat in disk.Statuses)
                    {
                        Program.log.Info("    code: " + stat.Code);
                        Program.log.Info("    level: " + stat.Level);
                        Program.log.Info("    displayStatus: " + stat.DisplayStatus);
                        Program.log.Info("    time: " + stat.Time);
                    }
                }
                Program.log.Info("VM general status");
                Program.log.Info("  provisioningStatus: " + vm.ProvisioningState);
                Program.log.Info("  id: " + vm.Id);
                Program.log.Info("  name: " + vm.Name);
                Program.log.Info("  type: " + vm.Type);
                Program.log.Info("  location: " + vm.Region);
                Program.log.Info("VM instance status");
                foreach (InstanceViewStatus stat in vm.InstanceView.Statuses)
                {
                    Program.log.Info("  code: " + stat.Code);
                    Program.log.Info("  level: " + stat.Level);
                    Program.log.Info("  displayStatus: " + stat.DisplayStatus);
                }
            
            }
            catch (Exception e)
            {
                Program.log.Error(e);
                throw;
            }
            return true;
        }
    }


}
