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

            // net.tcp://192.168.28.6:10040/example/service
            var baseAddress = new Uri("net.tcp://tcp.beaumont.cf-app.com:1111/example/service");
            //var baseAddress = new Uri("http://tcp.beaumont.cf-app.com:1111/example/service");

            var svcHost = new ServiceHost(typeof(WcfEntryPoint), baseAddress);

            ServiceThrottlingBehavior throttlingBehavior = new ServiceThrottlingBehavior();
            throttlingBehavior.MaxConcurrentCalls = Int32.MaxValue;
            throttlingBehavior.MaxConcurrentInstances = Int32.MaxValue;
            throttlingBehavior.MaxConcurrentSessions = Int32.MaxValue;
            svcHost.Description.Behaviors.Add(throttlingBehavior);

            ServiceDebugBehavior debug = svcHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            debug.IncludeExceptionDetailInFaults = true;

            //var netTcpBinding = new NetHttpBinding();

            var netTcpBinding = new NetTcpBinding(SecurityMode.None);
            //netTcpBinding.CloseTimeout = new TimeSpan(0, 15, 0);
            //netTcpBinding.ReceiveTimeout = new TimeSpan(0, 15, 0);
            //netTcpBinding.MaxReceivedMessageSize = 2147483647;
            //netTcpBinding.MaxBufferSize = 2147483647;
            //netTcpBinding.HostNameComparisonMode = HostNameComparisonMode.WeakWildcard;
            //netTcpBinding.Security.Mode = SecurityMode.None;
            //netTcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
            //netTcpBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.None;
            //netTcpBinding.Security.Message.ClientCredentialType = MessageCredentialType.None;
            //netTcpBinding.OpenTimeout = TimeSpan.FromMinutes(2);
            //netTcpBinding.SendTimeout = TimeSpan.FromMinutes(2);
            //netTcpBinding.ReceiveTimeout = TimeSpan.FromMinutes(10);

            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            //smb.HttpGetEnabled = true;
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
            //Console.ReadLine();
            svcHost.Close();
        }
    }
}

