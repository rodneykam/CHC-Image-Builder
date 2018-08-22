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
