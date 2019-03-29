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
            // use Steeltoe to parse VCAP_APPLICATION env variables into config object
            var builder = new ConfigurationBuilder().AddCloudFoundry();
            var config = builder.Build();
            var opts = new CloudFoundryApplicationOptions();
            var appSection = config.GetSection(CloudFoundryApplicationOptions.CONFIGURATION_PREFIX);
            appSection.Bind(opts);

            // get external TCP route (format: ["fullyqualifieddomainname.com:80000"])
            var appRouteHostAndExternalPort = opts.ApplicationUris.FirstOrDefault().Split(':');
            var appRouteHost = appRouteHostAndExternalPort.ElementAtOrDefault(0);
            var appExternalPort = appRouteHostAndExternalPort.ElementAtOrDefault(1);
          
            if (appRouteHost == "" || appExternalPort == "")
            {
                throw new System.ArgumentException("Invalid VCAP_APPLICATION route or port");
            }

            // ensure external TCP port and internal listening $PORT are the same
            var appInternalPort = opts.Port.ToString();
            if (appInternalPort != appExternalPort)
            {
                throw new System.ArgumentException($"Internal listening port must match External Route port : {appInternalPort} != {appExternalPort}");
            }
            Console.WriteLine($"URI: {appRouteHost}:{appInternalPort}");


            // have endpoints listen on public URI
            var baseAddress = new Uri($"net.tcp://{appRouteHost}:{appInternalPort}/example/service");
            var svcHost = new ServiceHost(typeof(HelloWorld), baseAddress);

            // enable verbose errors
            ServiceDebugBehavior debug = svcHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            debug.IncludeExceptionDetailInFaults = true;

            var netTcpBinding = new NetTcpBinding();
            netTcpBinding.Security.Mode = SecurityMode.None;

            // use custom binding to reduce connection pool settings to work better in load-balanced scenario (https://stackoverflow.com/questions/9714426/disable-connection-pooling-for-wcf-net-tcp-bindings)
            BindingElementCollection bindingElementCollection = netTcpBinding.CreateBindingElements();
            TcpTransportBindingElement transport = bindingElementCollection.Find<TcpTransportBindingElement>();
            transport.ConnectionPoolSettings.IdleTimeout = TimeSpan.Zero;
            transport.ConnectionPoolSettings.LeaseTimeout = TimeSpan.Zero;
            transport.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint = 0;

            CustomBinding balancedTcpBinding = new CustomBinding();
            balancedTcpBinding.Elements.AddRange(bindingElementCollection.ToArray());
            balancedTcpBinding.Name = "NetTcpBinding";


            // add metadata endpoint
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            svcHost.Description.Behaviors.Add(smb);

            svcHost.AddServiceEndpoint(
                ServiceMetadataBehavior.MexContractName,
                balancedTcpBinding,
                "mex"
            );

            // add service endpoint
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

