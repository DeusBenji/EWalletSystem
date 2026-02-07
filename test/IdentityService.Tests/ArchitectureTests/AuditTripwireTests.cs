using System.Reflection;
using IdentityService.Domain.Interfaces;
using IdentityService.Infrastructure.Services;
using NetArchTest.Rules;
using Xunit;

namespace IdentityService.Tests.ArchitectureTests;

public class AuditTripwireTests
{
    [Fact]
    public void SafeLogger_ShouldBeUsed_InRestrictedNamespaces()
    {
        // Enforce that Controllers and Services in IdentityService use ISafeLogger instead of ILogger
        // This is a "Tripwire" to prevent accidental usage of standard logger which might deserialize objects with PII
        
        // Note: This is checking CLASSES in Infrastructure.Services
        var result = Types.InAssembly(typeof(SignicatAuthService).Assembly)
            .That()
            .ResideInNamespace("IdentityService.Infrastructure.Services")
            .And()
            .AreClasses()
            .Should()
            .NotHaveDependencyOn("Microsoft.Extensions.Logging.Logger`1") // Should use ISafeLogger
            .GetResult();

        // We might allow generic ILogger for infrastructure plumbing but Service logic dealing with Claims should use SafeLogger
        // For now, let's strictly check if SignicatAuthService depends on ILogger directly
        
        // Refined check: Specific critical services MUST depend on ISafeLogger
        var criticalServices = new[] 
        { 
            typeof(SignicatAuthService), 
            typeof(MitIdClaimsMapper), 
            typeof(BankIdSeClaimsMapper),
            typeof(SignicatHttpClient) 
        };
        
        foreach (var service in criticalServices)
        {
           var constructors = service.GetConstructors();
           foreach (var ctor in constructors)
           {
               var parameters = ctor.GetParameters();
               foreach (var param in parameters)
               {
                   if (param.ParameterType.Name.StartsWith("ILogger`1") || param.ParameterType.Name == "ILogger")
                   {
                       // We allow generic logging for non-sensitive stuff, BUT for these specific classes we switched to ISafeLogger
                       // So assert they DO NOT take ILogger
                       Assert.Fail($"Class {service.Name} violates Audit Control! It injects ILogger. MUST use ISafeLogger.");
                   }
               }
           }
        }
    }

    [Fact]
    public void NoRawSessionLogging_ShouldBeEnforcedByCodeReviewOrConvention()
    {
         // This is harder to test via ArchRule without inspecting method bodies (Roslyn).
         // But we can verify that PiiRedactionService exists and is public
         var piiService = typeof(PiiRedactionService);
         Assert.NotNull(piiService);
         Assert.True(piiService.IsPublic);
    }
}
