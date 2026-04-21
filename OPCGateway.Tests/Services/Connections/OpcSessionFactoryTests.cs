using Opc.Ua;
using OPCGateway.Controllers;
using OPCGateway.Services.Connections;

namespace OPCGateway.Tests.Services.Connections;

[TestFixture]
public class OpcSessionFactoryTests
{
    private ApplicationConfiguration _config;
    private EndpointDescriptionCollection _endpoints;
    private string _endpointUrl;

    [SetUp]
    public void SetUp()
    {
        _config = OpcSessionFactory.GetOpcConfig();
        _endpointUrl = "opc.tcp://localhost:4840";

        _endpoints =
        [
            new EndpointDescription
            {
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = SecurityPolicies.None,
            },
            new EndpointDescription
            {
                SecurityMode = MessageSecurityMode.Sign,
                SecurityPolicyUri = SecurityPolicies.Basic128Rsa15,
            },
            new EndpointDescription
            {
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
            },
        ];
    }

    [Test]
    public void SelectEndpoint_WithSpecificSecuritySettings_FindsMatchingEndpoint()
    {
        // Arrange
        var securityMode = SecurityMode.Sign;
        var securityPolicy = SecurityPolicy.Basic128Rsa15;

        // Act
        var selectedEndpoint = OpcSessionFactory.SelectEndpoint(_config, _endpoints, _endpointUrl, securityMode, securityPolicy);

        // Assert
        Assert.NotNull(selectedEndpoint);
        Assert.AreEqual(MessageSecurityMode.Sign, selectedEndpoint.SecurityMode);
        Assert.AreEqual(SecurityPolicies.Basic128Rsa15, selectedEndpoint.SecurityPolicyUri);
    }

    [TestCase(SecurityMode.Auto, SecurityPolicy.Auto)]
    [TestCase(null, null)]
    public void SelectEndpoint_WithAutoSecuritySettings_SelectsBestEndpoint(SecurityMode? securityMode, SecurityPolicy? securityPolicy)
    {
        // Arrange

        // Act
        var selectedEndpoint = OpcSessionFactory.SelectEndpoint(_config, _endpoints, _endpointUrl, securityMode, securityPolicy);

        // Assert
        Assert.NotNull(selectedEndpoint);
        Assert.AreEqual(MessageSecurityMode.SignAndEncrypt, selectedEndpoint.SecurityMode);
        Assert.AreEqual(SecurityPolicies.Basic256, selectedEndpoint.SecurityPolicyUri);
    }

    [Test]
    public void SelectEndpoint_WithNoMatchingEndpoint_ThrowsInvalidOperationException()
    {
        // Arrange
        var securityMode = SecurityMode.Sign;
        var securityPolicy = SecurityPolicy.Basic256;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            OpcSessionFactory.SelectEndpoint(_config, _endpoints, _endpointUrl, securityMode, securityPolicy));
    }
}
