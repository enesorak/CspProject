using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Services;
using CspProject.Views;
using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CspProject
{
    public partial class App : Application
    {
        public static string AppVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
            }
        }

        public static IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // ✅ Show splash screen on startup
            var splashScreenViewModel = new DXSplashScreenViewModel
            {
                Title = "Compliant Spreadsheet Platform",
                Subtitle = "",
                Status = "Initializing...",
                IsIndeterminate = true,
                Logo = new Uri("pack://application:,,,/CspProject;component/Images/csp_logo-bg-none-13.png"),
                Copyright = $"Copyright © {DateTime.Now.Year} TOMCO. All rights reserved. v{AppVersion}"
            };

            SplashScreenManager.CreateFluent(splashScreenViewModel).ShowOnStartup();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // ✅ Register ApplicationDbContext as scoped service
            // Each window or operation will get its own instance
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite("Data Source=csp_database.db");
                options.EnableSensitiveDataLogging(false); // Disable in production
                options.EnableDetailedErrors(false); // Disable in production
            }, ServiceLifetime.Scoped);

            // ✅ Register other services here as needed
            // services.AddTransient<EmailService>();
            // services.AddScoped<TemplateService>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            #region Sentry Initialization

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            SentrySdk.Init(o =>
            {
                o.Dsn = "https://a98df4d25514031308051bd110811bb5@o4508148841709568.ingest.de.sentry.io/4510001337729104";
                o.Debug = false; // Set to false in production
                o.Release = AppVersion;
                o.AttachStacktrace = true;
                o.TracesSampleRate = 1.0; // Performance monitoring
                o.ProfilesSampleRate = 1.0; // Profiling

                o.SetBeforeSend((sentryEvent, hint) =>
                {
                    sentryEvent.User = new SentryUser
                    {
                        Username = Environment.UserName,
                        Id = Environment.MachineName,
                    };

                    // Add extra context
                    sentryEvent.SetExtra("OS", Environment.OSVersion.ToString());
                    sentryEvent.SetExtra("CLR", Environment.Version.ToString());
                    sentryEvent.SetExtra("MachineName", Environment.MachineName);
                    sentryEvent.SetExtra("ProcessorCount", Environment.ProcessorCount);
                    sentryEvent.SetExtra("WorkingSet", Environment.WorkingSet / 1024 / 1024 + " MB");

                    return sentryEvent;
                });
            });

            SentrySdk.AddBreadcrumb("Application starting", "app.lifecycle");

            #endregion

            SetupGlobalExceptionHandling();

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            // Set language property for framework elements
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            base.OnStartup(e);

            // ✅ Register notification service
            var notificationService = new NotificationService
            {
                UseWin8NotificationsIfAvailable = true,
                PredefinedNotificationTemplate = NotificationTemplate.ShortHeaderAndLongText,
                CustomNotificationScreen = NotificationScreen.Primary,
                CustomNotificationPosition = NotificationPosition.TopRight,
                ApplicationId = "CspProject"
            };

            ServiceContainer.Default.RegisterService("NotificationService", notificationService);

            try
            {
                // ✅ Track startup performance with Sentry
                var transaction = SentrySdk.StartTransaction("app-startup", "app.lifecycle");

                try
                {
                    await PerformStartupAsync();
                    transaction.Finish(SpanStatus.Ok);
                }
                catch (Exception ex)
                {
                    transaction.Finish(ex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                if (DXSplashScreen.IsActive) DXSplashScreen.Close();
                MessageBox.Show($"A fatal error occurred during startup: {ex.Message}",
                    "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private async Task PerformStartupAsync()
        {
            // ✅ Track database initialization performance
            var dbSpan = SentrySdk.GetSpan()?.StartChild("db.init");

            try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // ✅ Set database command timeout
                    dbContext.Database.SetCommandTimeout(30);

                    await dbContext.Database.EnsureCreatedAsync();

                    dbSpan?.Finish(SpanStatus.Ok);

                    // ✅ Ensure default templates exist
                    var templateSpan = SentrySdk.GetSpan()?.StartChild("templates.init");
                    await EnsureDefaultTemplatesExist();
                    templateSpan?.Finish(SpanStatus.Ok);

                    var currentUser = await dbContext.Users.FirstOrDefaultAsync();

                    if (currentUser == null)
                    {
                        // Show welcome window for first-time users
                        await Dispatcher.InvokeAsync(ShowWelcomeWindow);
                    }
                    else
                    {
                        // Show main window for existing users
                        await Dispatcher.InvokeAsync(() => ShowMainWindow(currentUser, dbContext));

                        // ✅ Check for approval emails in background (non-blocking)
                        _ = Task.Run(async () =>
                        {
                            var emailSpan = SentrySdk.GetSpan()?.StartChild("email.check");
                            try
                            {
                                using (var backgroundScope = ServiceProvider.CreateScope())
                                {
                                    var backgroundDbContext = backgroundScope.ServiceProvider
                                        .GetRequiredService<ApplicationDbContext>();
                                    var receiverService = new EmailReceiverService(backgroundDbContext);
                                    await receiverService.CheckForApprovalEmailsAsync();
                                    AppStateService.LastCheckTime = DateTime.Now;
                                }
                                emailSpan?.Finish(SpanStatus.Ok);
                            }
                            catch (Exception ex)
                            {
                                emailSpan?.Finish(ex);
                                SentrySdk.CaptureException(ex);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                dbSpan?.Finish(ex);
                throw;
            }
        }

        private void ShowMainWindow(User currentUser, ApplicationDbContext dbContext)
        {
            // Close splash screen before showing main window
            if (DXSplashScreen.IsActive) DXSplashScreen.Close();

            var mainWindow = new MainWindow(currentUser, dbContext);
            this.MainWindow = mainWindow;
            mainWindow.Show();
        }

        private void SetupGlobalExceptionHandling()
        {
            // 1. Handle AppDomain unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;

                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("exception_source", "AppDomain.UnhandledException");
                    scope.Level = SentryLevel.Fatal;
                });

                SentrySdk.CaptureException(exception);
            };

            // 2. Handle WPF UI thread exceptions
            DispatcherUnhandledException += (sender, args) =>
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("exception_source", "Dispatcher.UnhandledException");
                    scope.Level = SentryLevel.Error;
                });

                SentrySdk.CaptureException(args.Exception);

                args.Handled = true;

                MessageBox.Show(
                    "An unexpected error occurred. The error has been reported.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            };

            // 3. Handle Task unobserved exceptions
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("exception_source", "TaskScheduler.UnobservedTaskException");
                    scope.Level = SentryLevel.Error;
                });

                SentrySdk.CaptureException(args.Exception);
                args.SetObserved();
            };
        }

        private void ShowWelcomeWindow()
        {
            if (DXSplashScreen.IsActive) DXSplashScreen.Close();

            var welcomeView = new WelcomeView();
            var tempWindow = new Window
            {
                Content = welcomeView,
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            welcomeView.UserCreated += (s, newUser) =>
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    ShowMainWindow(newUser, dbContext);
                }
                tempWindow.Close();
            };

            tempWindow.Show();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            SentrySdk.CaptureException(e.Exception);
            e.Handled = true; // Prevent application crash
        }

        /// <summary>
        /// Ensures that the 'Templates' folder exists and contains default templates.
        /// </summary>
        private async Task EnsureDefaultTemplatesExist()
        {
            try
            {
                string exePath = AppDomain.CurrentDomain.BaseDirectory;
                string templateDirectory = Path.Combine(exePath, "Templates");

                if (!Directory.Exists(templateDirectory))
                {
                    Directory.CreateDirectory(templateDirectory);
                }

                string fmeaTemplatePath = Path.Combine(templateDirectory, "FMEA_Template.xlsx");

                if (!File.Exists(fmeaTemplatePath))
                {
                    // ✅ Run template creation in thread pool
                    await Task.Run(() =>
                    {
                        var prevCulture = Thread.CurrentThread.CurrentCulture;
                        try
                        {
                            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                            var workbook = new DevExpress.Spreadsheet.Workbook();
                            FmeaTemplateGenerator.Apply(workbook);

                            using (var fileStream = new FileStream(fmeaTemplatePath, FileMode.Create))
                            {
                                workbook.SaveDocument(fileStream, DevExpress.Spreadsheet.DocumentFormat.Xlsx);
                            }
                        }
                        finally
                        {
                            Thread.CurrentThread.CurrentCulture = prevCulture;
                        }
                    });

                    SentrySdk.AddBreadcrumb("FMEA template created", "templates");
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                // ✅ Don't crash the app if template creation fails
                MessageBox.Show($"Could not create default templates: {ex.Message}",
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}