using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.RepresentationModel;
using System.Configuration;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace CHC_Image_Builder
{
    class AzureCloudManager
    {
        private string _subscriptionId { get; set; }
        private string _clientId { get; set; }
        private string _clientKey { get; set; }
        private string _tenantId { get; set; }

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
                    Console.WriteLine("Error reading Authorization File");
                    throw;
                }
            }
        }
        public void ShowAuthorization()
        {
            Console.WriteLine("\nAzure Authorization");
            Console.WriteLine("-------------------");
            Console.WriteLine("  SubscriptionId: {0}", _subscriptionId);
            Console.WriteLine("  ClientId: {0}", _clientId);
            Console.WriteLine("  ClientKey: {0}........", _clientKey.Substring(0,8));
            Console.WriteLine("  TenantId: {0}", _tenantId);
        }

        public IAzure Authenticate()
        {
            var credentials = SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(_clientId,
                        _clientKey,
                        _tenantId,
                        AzureEnvironment.AzureGlobalCloud);

            var azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(_subscriptionId);

            return azure;
        }
    }


}
