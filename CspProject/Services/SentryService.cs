using Sentry;

namespace CspProject.Services
{
    public static class SentryService
    {
        /// <summary>
        /// Kullanıcı aksiyonlarını izler
        /// </summary>
        public static void TrackUserAction(string action, string category, 
            Dictionary<string, object>? extras = null)
        {
            SentrySdk.AddBreadcrumb(action, category);
            
            if (extras != null)
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    foreach (var extra in extras)
                    {
                        scope.SetExtra(extra.Key, extra.Value);
                    }
                });
            }
        }

        /// <summary>
        /// Performance ölçümü başlatır
        /// </summary>
        public static ITransactionTracer StartPerformanceTracking(string operation, string type)
        {
            return SentrySdk.StartTransaction(operation, type);
        }

        /// <summary>
        /// Hata yakalama ile birlikte kullanıcı context'i ekler
        /// </summary>
        public static void CaptureExceptionWithContext(Exception ex, 
            string? userId = null, 
            string? username = null,
            Dictionary<string, object>? extras = null)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                if (!string.IsNullOrEmpty(userId) || !string.IsNullOrEmpty(username))
                {
                    scope.User = new SentryUser
                    {
                        Id = userId,
                        Username = username
                    };
                }

                if (extras != null)
                {
                    foreach (var extra in extras)
                    {
                        scope.SetExtra(extra.Key, extra.Value);
                    }
                }
            });

            SentrySdk.CaptureException(ex);
        }

        /// <summary>
        /// Sessiz hata yakalama (kullanıcıya gösterme)
        /// </summary>
        public static void CaptureSilentException(Exception ex, string context)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("silent_error", "true");
                scope.SetExtra("context", context);
            });
            
            SentrySdk.CaptureException(ex);
        }
    }
}