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
        void HelloWorld();

    }

    [ServiceContract]
    public interface IWriteMe
    {
        [OperationContract]
        void WriteMe(string text);
    }

    public partial class WcfEntryPoint : IHelloWorld
    {
        public void HelloWorld()
        {
            Console.WriteLine("Hello World!");
        }

    }

    public partial class WcfEntryPoint : IWriteMe
    {

        public void WriteMe(string text)
        {
            Console.WriteLine($"WriteMe: {text}");
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

            if (appRouteHost == "" || appExternalPort == "")
            {
                throw new System.ArgumentException("Invalid VCAP_APPLICATION route or port");
            }

            if (appExternalPort != opts.Port.ToString())
            {
                throw new System.ArgumentException($"Route External port must match internal port: {appExternalPort} != {opts.Port}");
            }
            Console.WriteLine($"URI: {appRouteHost}:{appExternalPort}");


            var baseAddress = new Uri($"net.tcp://{appRouteHost}:{appExternalPort}/example/service");

            var svcHost = new ServiceHost(typeof(WcfEntryPoint), baseAddress);

            ServiceThrottlingBehavior throttlingBehavior = new ServiceThrottlingBehavior();
            throttlingBehavior.MaxConcurrentCalls = Int32.MaxValue;
            throttlingBehavior.MaxConcurrentInstances = Int32.MaxValue;
            throttlingBehavior.MaxConcurrentSessions = Int32.MaxValue;
            svcHost.Description.Behaviors.Add(throttlingBehavior);

            ServiceDebugBehavior debug = svcHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            debug.IncludeExceptionDetailInFaults = true;

            var netTcpBinding = new NetTcpBinding(SecurityMode.None);

            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            svcHost.Description.Behaviors.Add(smb);

            svcHost.AddServiceEndpoint(
                ServiceMetadataBehavior.MexContractName,
                netTcpBinding,
                "mex"
            );

            svcHost.AddServiceEndpoint(
                typeof(IHelloWorld),
                netTcpBinding,
                "IHelloWorld"
            );

            svcHost.Open();
            Console.WriteLine($"svcHost is {svcHost.State}.  Press enter to close.");

            Thread.Sleep(Timeout.Infinite);
            svcHost.Close();
        }
    }
}

