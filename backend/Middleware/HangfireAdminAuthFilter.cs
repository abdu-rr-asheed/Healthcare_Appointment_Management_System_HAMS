using Hangfire.Dashboard;

namespace HAMS.API.Middleware
{
    /// <summary>
    /// Restricts access to the Hangfire dashboard to authenticated users
    /// with the Administrator role. Without this filter, the dashboard is
    /// publicly accessible and exposes all background job details.
    /// </summary>
    public class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Must be authenticated
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            // Must have the Administrator role
            return httpContext.User.IsInRole("Administrator");
        }
    }
}
