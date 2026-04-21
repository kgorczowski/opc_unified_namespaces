using System.Security.Cryptography.X509Certificates;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using OPCGateway.Controllers;

namespace OPCGateway.Services.Connections;

public class OpcSessionFactory : IOpcSessionFactory
{
    public static ApplicationConfiguration GetOpcConfig()
    {
        return new ApplicationConfiguration()
        {
            ApplicationName = "OPCGateway",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = CertificateStoreType.X509Store,
                    StorePath = "CurrentUser\\My",
                    SubjectName = "CN=OPCGateway Client",
                },
                AutoAcceptUntrustedCertificates = true,
                AddAppCertToTrustedStore = true,
            },
            TransportQuotas = new TransportQuotas { OperationTimeout = 600000 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
        };
    }

    public static EndpointDescription SelectEndpoint(ApplicationConfiguration config, EndpointDescriptionCollection endpoints, string endpointUrl, SecurityMode? securityMode, SecurityPolicy? securityPolicy)
    {
        EndpointDescription? selectedEndpoint;

        if ((securityMode.HasValue && securityMode != SecurityMode.Auto) || (securityPolicy.HasValue && securityPolicy != SecurityPolicy.Auto))
        {
            // Manually select the endpoint that matches the desired security settings
            selectedEndpoint = endpoints.FirstOrDefault(e =>
                (!securityMode.HasValue || securityMode == SecurityMode.Auto || e.SecurityMode == OpcUtilities.ConvertSecurityMode(securityMode.Value)) &&
                (!securityPolicy.HasValue || securityPolicy == SecurityPolicy.Auto || e.SecurityPolicyUri == OpcUtilities.ConvertSecurityPolicy(securityPolicy.Value)));

            if (selectedEndpoint == null)
            {
                throw new InvalidOperationException("No matching endpoint found with the specified security settings.");
            }
        }
        else
        {
            // Automatically select the best endpoint
            selectedEndpoint = CoreClientUtils.SelectEndpoint(config, endpointUrl, useSecurity: true, 15000);
        }

        return selectedEndpoint;
    }

    public async Task<Session> CreateSessionAsync(string endpointUrl, string? username, string? password, SecurityMode? securityMode, SecurityPolicy? securityPolicy, UserTokenType authentication, string? certificatePath, string? certificatePassword)
    {
        ApplicationConfiguration config = GetOpcConfig();

        await config.Validate(ApplicationType.Client);

        var application = new ApplicationInstance
        {
            ApplicationName = "OPCGateway",
            ApplicationType = ApplicationType.Client,
            ApplicationConfiguration = config,
        };

        await application.CheckApplicationInstanceCertificate(false, 0);

        var endpointConfiguration = EndpointConfiguration.Create(config);

        var discoveryClient = DiscoveryClient.Create(config, new Uri(endpointUrl));
        var endpoints = discoveryClient.GetEndpoints(null);

        EndpointDescription? selectedEndpoint = SelectEndpoint(config, endpoints, endpointUrl, securityMode, securityPolicy);

        var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

        UserIdentity userIdentity;
        if (authentication == UserTokenType.Anonymous)
        {
            userIdentity = new UserIdentity();
        }
        else if (authentication == UserTokenType.UserName)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Username and password must be provided for UserName authentication.");
            }

            userIdentity = new UserIdentity(username, password);
        }
        else if (authentication == UserTokenType.Certificate)
        {
            var certificate = await LoadClientCertificate(config.SecurityConfiguration, certificatePath!, certificatePassword);
            userIdentity = new UserIdentity(certificate);
        }
        else
        {
            throw new InvalidOperationException("Unsupported authentication method.");
        }

        var session = await SessionFactory.CreateSession(
            config,
            endpoint,
            true,
            "OPCGateway Session",
            60000,
            userIdentity,
            null);

        return session;
    }

    private static async Task<X509Certificate2> LoadClientCertificate(SecurityConfiguration securityConfig, string certificatePath, string? password = null)
    {
        if (!string.IsNullOrEmpty(certificatePath))
        {
            return string.IsNullOrEmpty(password)
                ? new X509Certificate2(certificatePath)
                : new X509Certificate2(certificatePath, password);
        }

        var cert = await securityConfig.ApplicationCertificate.Find(true);

        if (!cert.HasPrivateKey)
        {
            throw new InvalidOperationException("Client certificate does not have a private key and cannot be used for authentication.");
        }

        if (cert == null)
        {
            throw new InvalidOperationException("Client certificate not found.");
        }

        return cert;
    }
}