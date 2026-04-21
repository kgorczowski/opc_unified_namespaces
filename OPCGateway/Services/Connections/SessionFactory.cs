using Opc.Ua;
using Opc.Ua.Client;

namespace OPCGateway.Services.Connections;

public static class SessionFactory
{
    private static Func<ApplicationConfiguration, ConfiguredEndpoint, bool, string, uint, IUserIdentity, string[]?, Task<Session>>? _createSessionFunc;

    public static void SetCreateSessionFunc(Func<ApplicationConfiguration, ConfiguredEndpoint, bool, string, uint, IUserIdentity, string[]?, Task<Session>> createSessionFunc)
    {
        _createSessionFunc = createSessionFunc;
    }

    public static void ResetCreateSessionFunc()
    {
        _createSessionFunc = null;
    }

    public static Task<Session> CreateSession(ApplicationConfiguration config, ConfiguredEndpoint endpoint, bool updateBeforeConnect, string sessionName, uint sessionTimeout, IUserIdentity identity, string[]? preferredLocales)
    {
        if (_createSessionFunc != null)
        {
            return _createSessionFunc(config, endpoint, updateBeforeConnect, sessionName, sessionTimeout, identity, preferredLocales);
        }

        return Session.Create(config, endpoint, updateBeforeConnect, sessionName, sessionTimeout, identity, preferredLocales);
    }
}