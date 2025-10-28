using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace TutorLinkBe.Helper
{
    public class DebugRequirement : IAuthorizationRequirement
    {
    }

    public class DebugAuthorizationHandler : AuthorizationHandler<DebugRequirement>
    {
        private readonly ILogger<DebugAuthorizationHandler> _logger;

        public DebugAuthorizationHandler(ILogger<DebugAuthorizationHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DebugRequirement requirement)
        {
            _logger.LogInformation("Debug Authorization - User: {User}, IsAuthenticated: {IsAuthenticated}, Claims: {Claims}",
                context.User?.Identity?.Name,
                context.User?.Identity?.IsAuthenticated,
                string.Join(", ", context.User?.Claims?.Select(c => $"{c.Type}={c.Value}") ?? new List<string>()));

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}