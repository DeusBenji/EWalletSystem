using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using ValidationService.Protocol;

namespace E2ETests
{
    /// <summary>
    /// Version matrix compatibility tests.
    /// Tests compatibility between different versions of Validator, Extension, and SDK.
    /// </summary>
    public class VersionMatrixTests : IClassFixture<E2ETestFixture>
    {
        private readonly E2ETestFixture _fixture;
        private readonly HttpClient _validationClient;

        public VersionMatrixTests(E2ETestFixture fixture)
        {
            _fixture = fixture;
            _validationClient = _fixture.CreateValidationClient();
        }

        [Fact]
        public async Task Test_CompatibleVersions_ValidatorV1_ExtensionV1_2_SDKV1_Success()
        {
            // Arrange
            var policyId = "age_over_18";
            var extensionVersion = "1.2.0";
            var sdkVersion = "1.0.0";

            // Act
            var result = await _fixture.GenerateAndValidateProof(
                policyId: policyId,
                extensionVersion: extensionVersion,
                sdkVersion: sdkVersion
            );

            // Assert
            result.Valid.Should().BeTrue("compatible versions should work together");
            result.ErrorCode.Should().BeNull();
        }

        [Fact]
        public async Task Test_BreakingChange_ValidatorV1_ExtensionV2_GracefulFailure()
        {
            // Arrange
            var policyId = "age_over_18";
            var extensionVersion = "2.0.0"; // Breaking change
            var sdkVersion = "1.0.0";

            // Simulate breaking protocol change in extension
            var proofEnvelope = await _fixture.GenerateProof(
                policyId: policyId,
                extensionVersion: extensionVersion,
                protocolVersion: "2.0" // Breaking protocol version
            );

            // Act
            var response = await _validationClient.PostAsJsonAsync("/api/validate", proofEnvelope);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            error.Code.Should().Be("UNSUPPORTED_PROTOCOL_VERSION");
            error.Message.Should().Contain("protocol version 2.0 is not supported");
        }

        [Fact]
        public async Task Test_DowngradeRejection_PolicyRequiresV1_2_ExtensionUsesV1_1_Rejected()
        {
            // Arrange
            var policyId = "high_security_policy";
            
            // Set minimum version to 1.2
            MinimumPolicyVersions.Minimums[policyId] = "1.2.0";

            // Generate proof with version 1.1 (downgrade attempt)
            var proofEnvelope = await _fixture.GenerateProof(
                policyId: policyId,
                policyVersion: "1.1.0" // Downgrade attempt
            );

            // Act
            var result = await _fixture.ValidateProof(proofEnvelope);

            // Assert
            result.Valid.Should().BeFalse("downgrade should be rejected");
            result.ErrorCode.Should().Be("ANTI_DOWNGRADE_VIOLATION");
            result.ErrorMessage.Should().Contain("policy version 1.1.0 is below minimum required version 1.2.0");
        }

        [Fact]
        public async Task Test_ForwardCompatibility_ValidatorV1_ExtensionV1_3_Success()
        {
            // Arrange - Test that newer extension versions work with validator
            var policyId = "age_over_18";
            var extensionVersion = "1.3.0"; // Newer, but backwards compatible

            // Act
            var result = await _fixture.GenerateAndValidateProof(
                policyId: policyId,
                extensionVersion: extensionVersion
            );

            // Assert
            result.Valid.Should().BeTrue("forward compatible versions should work");
        }

        [Fact]
        public async Task Test_CircuitVersionMismatch_Rejected()
        {
            // Arrange
            var policyId = "age_over_18";
            var circuitVersion = "0.9.0"; // Old circuit version

            // Generate proof with outdated circuit
            var proofEnvelope = await _fixture.GenerateProof(
                policyId: policyId,
                circuitVersion: circuitVersion
            );

            // Act
            var result = await _fixture.ValidateProof(proofEnvelope);

            // Assert
            result.Valid.Should().BeFalse("outdated circuit should be rejected");
            result.ErrorCode.Should().Be("ANTI_DOWNGRADE_VIOLATION");
        }

        [Fact]
        public async Task Test_MultipleVersionUpgrades_AllCompatible()
        {
            // Test version progression: 1.0 -> 1.1 -> 1.2
            var policyId = "age_over_18";

            // Version 1.0
            var result1 = await _fixture.GenerateAndValidateProof(policyId, policyVersion: "1.0.0");
            result1.Valid.Should().BeTrue();

            // Upgrade to 1.1
            var result2 = await _fixture.GenerateAndValidateProof(policyId, policyVersion: "1.1.0");
            result2.Valid.Should().BeTrue();

            // Upgrade to 1.2
            var result3 = await _fixture.GenerateAndValidateProof(policyId, policyVersion: "1.2.0");
            result3.Valid.Should().BeTrue();

            // Verify cannot downgrade back to 1.0
            MinimumPolicyVersions.Minimums[policyId] = "1.2.0";
            var result4 = await _fixture.GenerateAndValidateProof(policyId, policyVersion: "1.0.0");
            result4.Valid.Should().BeFalse();
        }
    }

    public class ErrorResponse
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
