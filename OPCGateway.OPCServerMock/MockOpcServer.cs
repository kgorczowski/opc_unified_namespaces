using Opc.Ua;
using Opc.Ua.Configuration;
using OPCGateway.OPCServerMock.MockData;
using OPCGateway.OPCServerMock.Servers;

namespace OPCGateway.OPCServerMock;

public class MockOpcServer
{
    private ApplicationInstance? _application1;
    private ApplicationInstance? _application2;

    public async Task StartAsync()
    {
        // Create configurations for both servers
        var config1 = CreateServerConfig(
            "OPC UA Server 1",
            "4841",
            "/app/certificates/server1");

        var config2 = CreateServerConfig(
            "OPC UA Server 2",
            "4842",
            "/app/certificates/server2");

        // Prepare Nodes
        NodesParser.CreateNodesDictionaries(
            Server1DynamicReadData.GetOpcParametersMock(),
            WriteData.GetOpcParametersMock());

        // Start Server 1
        _application1 = new ApplicationInstance(config1);
        await _application1.CheckApplicationInstanceCertificate(false, 0);
        await config1.Validate(ApplicationType.Server);
        var server1 = new Server1WithAuthentication();
        await _application1.Start(server1);
        Console.WriteLine("Server 1 started on port 4841");
        await Task.Delay(1000); // Wait 1 second before starting the next server

        // Start Server 2
        _application2 = new ApplicationInstance(config2);
        await _application2.CheckApplicationInstanceCertificate(false, 0);
        await config2.Validate(ApplicationType.Server);
        var server2 = new Server2WithAuthentication();
        await _application2.Start(server2);
        Console.WriteLine("Server 2 started on port 4842");

        Console.WriteLine("Both servers are running. Press Enter to exit.");
    }

    public void Stop()
    {
        _application1?.Stop();
        _application2?.Stop();
    }

    private static ApplicationConfiguration CreateServerConfig(
        string serverName,
        string port,
        string certificatePath)
    {
        return new ApplicationConfiguration()
        {
            ApplicationName = serverName,
            ApplicationUri = Utils.Format(@"urn:{0}:{1}", System.Net.Dns.GetHostName(), serverName),
            ApplicationType = ApplicationType.Server,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = $"{certificatePath}/MachineDefault",
                    SubjectName = Utils.Format(@"CN={0}, DC={1}", serverName, System.Net.Dns.GetHostName()),
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = $"{certificatePath}/UA Certificate Authorities",
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = $"{certificatePath}/UA Applications",
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = $"{certificatePath}/RejectedCertificates",
                },
                AutoAcceptUntrustedCertificates = true,
                RejectSHA1SignedCertificates = false,
            },
            TransportConfigurations = [],
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses = [$"opc.tcp://0.0.0.0:{port}/OpcMockServer"],
                MinRequestThreadCount = 5,
                MaxRequestThreadCount = 100,
                MaxQueuedRequestCount = 200,
                SecurityPolicies =
                [
                    new ServerSecurityPolicy
                    {
                        SecurityMode = MessageSecurityMode.Sign,
                        SecurityPolicyUri = SecurityPolicies.Basic128Rsa15,
                    },
                    new ServerSecurityPolicy
                    {
                        SecurityMode = MessageSecurityMode.SignAndEncrypt,
                        SecurityPolicyUri = SecurityPolicies.Aes128_Sha256_RsaOaep,
                    },
                ],
                UserTokenPolicies =
                [
                    new UserTokenPolicy(UserTokenType.Anonymous),
                    new UserTokenPolicy(UserTokenType.UserName),
                    new UserTokenPolicy(UserTokenType.Certificate),
                ],
            },
            TraceConfiguration = new TraceConfiguration(),
        };
    }
}