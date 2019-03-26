using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Microsoft.Extensions.Configuration;
using System.ServiceModel.Channels;
using System.Threading;

namespace SelfHost
{
    [ServiceContract]
    public interface IHelloWorld
    {
        [OperationContract]
        string Hello();
    }


    public class HelloWorld : IHelloWorld
    {
        public string Hello()
        {
            Console.WriteLine("Hello World!");
            return Environment.GetEnvironmentVariable("CF_INSTANCE_INDEX");
        }

    }

    class Program
    {
        static void Main()
        {
            var builder = new ConfigurationBuilder().AddCloudFoundry();
            var config = builder.Build();
            var opts = new CloudFoundryApplicationOptions();
            var appSection = config.GetSection(CloudFoundryApplicationOptions.CONFIGURATION_PREFIX);
            appSection.Bind(opts);

            var appRouteHostAndExternalPort = opts.ApplicationUris.FirstOrDefault().Split(':');
            var appRouteHost = appRouteHostAndExternalPort.ElementAtOrDefault(0);
            var appExternalPort = appRouteHostAndExternalPort.ElementAtOrDefault(1);
            var appInternalPort = opts.Port.ToString();

            if (appRouteHost == "" || appExternalPort == "")
            {
                throw new System.ArgumentException("Invalid VCAP_APPLICATION route or port");
            }

            if (appInternalPort != appExternalPort)
            {
                throw new System.ArgumentException($"Internal listening port must match External Route port : {appInternalPort} != {appExternalPort}");
            }
            Console.WriteLine($"URI: {appRouteHost}:{appInternalPort}");


            var baseAddress = new Uri($"net.tcp://{appRouteHost}:{appInternalPort}/example/service");

            var svcHost = new ServiceHost(typeof(HelloWorld), baseAddress);

            ServiceDebugBehavior debug = svcHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            debug.IncludeExceptionDetailInFaults = true;

            var netTcpBinding = new NetTcpBinding();
            netTcpBinding.Security.Mode = SecurityMode.None;

            BindingElementCollection bindingElementCollection = netTcpBinding.CreateBindingElements();
            TcpTransportBindingElement transport = bindingElementCollection.Find<TcpTransportBindingElement>();
            transport.ConnectionPoolSettings.IdleTimeout = TimeSpan.Zero;
            transport.ConnectionPoolSettings.LeaseTimeout = TimeSpan.Zero;
            transport.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint = 0;

            CustomBinding balancedTcpBinding = new CustomBinding();
            balancedTcpBinding.Elements.AddRange(bindingElementCollection.ToArray());
            balancedTcpBinding.Name = "NetTcpBinding";


            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            svcHost.Description.Behaviors.Add(smb);

            svcHost.AddServiceEndpoint(
                ServiceMetadataBehavior.MexContractName,
                balancedTcpBinding,
                "mex"
            );

            svcHost.AddServiceEndpoint(
                typeof(IHelloWorld),
                balancedTcpBinding,
                "IHelloWorld"
            );

            svcHost.Open();
            Console.WriteLine($"svcHost is {svcHost.State}.  Press enter to close.");

            Thread.Sleep(Timeout.Infinite);
            svcHost.Close();
        }
    }
}

