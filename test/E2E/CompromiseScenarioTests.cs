using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace E2ETests
{
    /// <summary>
    /// Compromise scenario tests.
    /// Tests panic button, device binding, and recovery flows.
    /// </summary>
    public class CompromiseScenarioTests : IClassFixture<E2ETestFixture>
    {
        private readonly E2ETestFixture _fixture;

        public CompromiseScenarioTests(E2ETestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Test_PanicButton_WipeAndRecover()
        {
            // Arrange - Issue credential
            var policyId = "age_over_18";
            var credentialId = await _fixture.IssueCredential(policyId);

            // Verify credential exists
            var credentials = await _fixture.ListCredentials();
            credentials.Should().ContainSingle(c => c.Id == credentialId);

            // Generate proof to verify it works
            var proofBefore = await _fixture.GenerateProof(policyId);
            proofBefore.Should().NotBeNull();

            // Act - Activate panic button
            await _fixture.ActivatePanicButton();

            // Assert - All data wiped
            var credentialsAfterPanic = await _fixture.ListCredentials();
            credentialsAfterPanic.Should().BeEmpty("all credentials should be wiped");

            var deviceSecretExists = await _fixture.DeviceSecretExists();
            deviceSecretExists.Should().BeFalse("device secret should be wiped");

            var circuitsCached = await _fixture.GetCachedCircuits();
            circuitsCached.Should().BeEmpty("cached circuits should be wiped");

            // Verify audit log
            var auditLog = await _fixture.GetAuditLog();
            auditLog.Should().Contain(e => 
                e.EventType == "PANIC_BUTTON_ACTIVATED" &&
                e.Trigger == "user_initiated"
            );

            // Recovery - Re-authenticate and issue new credential
            var newCredentialId = await _fixture.IssueCredential(policyId);
            newCredentialId.Should().NotBe(credentialId, "new credential should have different ID");

            // Verify proof generation works with new credential
            var proofAfter = await _fixture.GenerateProof(policyId);
            proofAfter.Should().NotBeNull();

            var validationResult = await _fixture.ValidateProof(proofAfter);
            validationResult.Valid.Should().BeTrue("proof should be valid after recovery");
        }

        [Fact]
        public async Task Test_StolenCredential_DeviceBindingPreventsUse()
        {
            // Arrange - Issue credential on Device A
            var policyId = "age_over_18";
            var deviceA = "device-a-fingerprint";
            var credentialId = await _fixture.IssueCredential(policyId, deviceId: deviceA);

            // Get encrypted credential data
            var encryptedCredential = await _fixture.GetEncryptedCredential(credentialId);
            encryptedCredential.DeviceTag.Should().NotBeNullOrEmpty();

            // Act - Attacker extracts credential and tries to use on Device B
            var deviceB = "device-b-fingerprint";
            
            // Simulate loading credential on different device
            var loadException = await Record.ExceptionAsync(async () =>
            {
                await _fixture.LoadCredentialOnDevice(
                    encryptedCredential: encryptedCredential,
                    deviceId: deviceB
                );
            });

            // Assert - Device binding prevents use
            loadException.Should().NotBeNull("loading on different device should fail");
            loadException.Message.Should().Contain("device tag mismatch");

            // Verify proof generation fails
            var proofException = await Record.ExceptionAsync(async () =>
            {
                await _fixture.GenerateProof(policyId, deviceId: deviceB);
            });

            proofException.Should().NotBeNull("proof generation should fail on wrong device");
        }

        [Fact]
        public async Task Test_CredentialTheft_CannotDecryptWithoutDeviceSecret()
        {
            // Arrange
            var policyId = "age_over_18";
            var credentialId = await _fixture.IssueCredential(policyId);
            var encryptedCredential = await _fixture.GetEncryptedCredential(credentialId);

            // Act - Attacker tries to decrypt without device secret
            var decryptException = await Record.ExceptionAsync(async () =>
            {
                await _fixture.DecryptCredential(
                    encryptedCredential: encryptedCredential,
                    withoutDeviceSecret: true
                );
            });

            // Assert
            decryptException.Should().NotBeNull("decryption without device secret should fail");
            decryptException.Should().BeOfType<CryptographicException>();
        }

        [Fact]
        public async Task Test_MultipleDevices_EachHasOwnDeviceSecret()
        {
            // Arrange - Same user, two devices
            var policyId = "age_over_18";
            var deviceA = "device-a";
            var deviceB = "device-b";

            // Act - Issue credentials on both devices
            var credentialIdA = await _fixture.IssueCredential(policyId, deviceId: deviceA);
            var credentialIdB = await _fixture.IssueCredential(policyId, deviceId: deviceB);

            var encryptedA = await _fixture.GetEncryptedCredential(credentialIdA);
            var encryptedB = await _fixture.GetEncryptedCredential(credentialIdB);

            // Assert - Different device tags
            encryptedA.DeviceTag.Should().NotBe(encryptedB.DeviceTag, 
                "different devices should have different tags");

            // Verify Device A cannot use Device B's credential
            var crossDeviceException = await Record.ExceptionAsync(async () =>
            {
                await _fixture.LoadCredentialOnDevice(
                    encryptedCredential: encryptedB,
                    deviceId: deviceA
                );
            });

            crossDeviceException.Should().NotBeNull("cross-device credential use should fail");
        }

        [Fact]
        public async Task Test_PanicButton_AuditLogDoesNotContainPII()
        {
            // Arrange
            var policyId = "age_over_18";
            await _fixture.IssueCredential(policyId);

            // Act
            await _fixture.ActivatePanicButton();

            // Assert - Check audit log for PII
            var auditLog = await _fixture.GetAuditLog();
            var panicEvent = auditLog.Should().ContainSingle(e => 
                e.EventType == "PANIC_BUTTON_ACTIVATED"
            ).Subject;

            // Verify no PII in log
            panicEvent.Data.Should().NotContain("birthdate");
            panicEvent.Data.Should().NotContain("name");
            panicEvent.Data.Should().NotContain("ssn");
            panicEvent.Data.Should().NotContain("address");

            // Should only contain metadata
            panicEvent.Data.Should().Contain("credentialsWiped");
            panicEvent.Data.Should().Contain("deviceSecretWiped");
            panicEvent.Data.Should().Contain("trigger");
        }

        [Fact]
        public async Task Test_CompromisedExtension_OldCredentialsInvalid()
        {
            // Scenario: Extension is compromised, user gets new device

            // Arrange - Original device
            var policyId = "age_over_18";
            var originalDevice = "original-device";
            var credentialId = await _fixture.IssueCredential(policyId, deviceId: originalDevice);

            var proof = await _fixture.GenerateProof(policyId, deviceId: originalDevice);
            var validBefore = await _fixture.ValidateProof(proof);
            validBefore.Valid.Should().BeTrue();

            // Act - Simulate compromise: User activates panic on OLD device
            await _fixture.ActivatePanicButton(deviceId: originalDevice);

            // User sets up NEW device
            var newDevice = "new-device";
            var newCredentialId = await _fixture.IssueCredential(policyId, deviceId: newDevice);

            // Assert - Old credential no longer works
            var oldProofException = await Record.ExceptionAsync(async () =>
            {
                await _fixture.GenerateProof(policyId, credentialId: credentialId);
            });
            oldProofException.Should().NotBeNull("old credential should be wiped");

            // New credential works fine
            var newProof = await _fixture.GenerateProof(policyId, deviceId: newDevice);
            var validAfter = await _fixture.ValidateProof(newProof);
            validAfter.Valid.Should().BeTrue();
        }
    }
}
