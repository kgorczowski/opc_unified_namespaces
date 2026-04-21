using Opc.Ua;
using Opc.Ua.Server;
using System.Security.Cryptography.X509Certificates;

namespace OPCGateway.OPCServerMock.Servers;

public abstract class BaseServerWithAuthentication : StandardServer
{
    protected override void OnServerStarting(ApplicationConfiguration configuration)
    {
        Console.WriteLine("The Server is starting.");
        base.OnServerStarting(configuration);
    }

    protected override void OnServerStarted(IServerInternal server)
    {
        base.OnServerStarted(server);
        server.SessionManager.ImpersonateUser += SessionManager_ImpersonateUser;
    }

    // Abstract method that derived classes must implement
    protected abstract INodeManager CreateCustomNodeManager(
        IServerInternal server,
        ApplicationConfiguration configuration);

    protected override MasterNodeManager CreateMasterNodeManager(
        IServerInternal server,
        ApplicationConfiguration configuration)
    {
        Utils.Trace("Creating the Node Managers.");
        List<INodeManager> nodeManagers =
        [

            // Create the custom node manager specific to this server instance
            CreateCustomNodeManager(server, configuration),
        ];

        return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
    }

    protected override ServerProperties LoadServerProperties()
    {
        ServerProperties properties = new()
        {
            ManufacturerName = "VM",
            ProductName = GetProductName(), // Make this customizable
            ProductUri = string.Empty,
            SoftwareVersion = Utils.GetAssemblySoftwareVersion(),
            BuildNumber = Utils.GetAssemblyBuildNumber(),
            BuildDate = Utils.GetAssemblyTimestamp(),
        };
        return properties;
    }

    // Virtual method to allow customization of product name
    protected virtual string GetProductName()
    {
        return "OPC MOCK SERVER";
    }

    private void SessionManager_ImpersonateUser(Session session, ImpersonateEventArgs args)
    {
        if (args.NewIdentity is UserNameIdentityToken userNameToken)
        {
            VerifyPassword(userNameToken.UserName, userNameToken.DecryptedPassword);
            args.Identity = new UserIdentity(userNameToken);
            Utils.Trace("UserName Token Accepted: {0}", args.Identity.DisplayName);
            return;
        }

        if (args.NewIdentity is AnonymousIdentityToken anonymousToken)
        {
            args.Identity = new UserIdentity(anonymousToken);
            Utils.Trace("Anonymous Token Accepted: {0}", args.Identity.DisplayName);
            return;
        }

        if (args.NewIdentity is X509IdentityToken x509Token)
        {
            VerifyCertificate(x509Token.Certificate);
            args.Identity = new UserIdentity(x509Token);
            Utils.Trace("X509 Certificate Token Accepted: {0}", args.Identity.DisplayName);
            return;
        }

        throw ServiceResultException.Create(StatusCodes.BadUserAccessDenied, "Login failed - credentials are needed ");
    }

    private void VerifyCertificate(X509Certificate2 certificate)
    {
        bool result = true;
        if (!result)
        {
            throw ServiceResultException.Create(
                StatusCodes.BadUserAccessDenied,
                "Login failed for certificate: {0}",
                certificate.Subject);
        }
    }

    private void VerifyPassword(string userName, string password)
    {
        bool result = true;
        if (!result)
        {
            throw ServiceResultException.Create(
                StatusCodes.BadUserAccessDenied,
                "Login failed for user: {0}",
                userName);
        }
    }
}