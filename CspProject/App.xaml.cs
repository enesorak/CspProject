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

        
        // --- DEPENDENCY INJECTION KURULUMU ---
        public static IServiceProvider ServiceProvider { get; private set; }

     

        public App()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
          

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();


     
            var splashScreenViewModel = new DXSplashScreenViewModel
            {
                
                Title = "Compliant Spreadsheet Platform",  
                Subtitle = "",
                Status = "Initializing...",
                IsIndeterminate = true,
                Logo = new Uri("pack://application:,,,/CspProject;component/Images/csp_logo-bg-none-13.png"),
                Copyright = $"Copyright © {DateTime.Now.Year} TOMCO. All rights reserved. v{AppVersion}"
            };
            
            SplashScreenManager.CreateFluent(splashScreenViewModel
            ).ShowOnStartup();
     
        }
       
        private void ConfigureServices(IServiceCollection services)
        {
            // "Ne zaman birisi ApplicationDbContext isterse, ona yeni bir tane ver"
            // Scoped, her pencere veya işlem için bir tane oluşturulmasını sağlar, en iyi yöntem budur.
            services.AddScoped<ApplicationDbContext>();

            // Gelecekte EmailService gibi diğer servisleri de buraya ekleyebiliriz.
            // services.AddTransient<EmailService>();
            
          
        }
        
        // OnStartup metodunu 'async void' olarak değiştiriyoruz.
        // Bu, en üst seviye olay yöneticileri için standart bir yaklaşımdır.
        protected override async void OnStartup(StartupEventArgs e)
        {
            #region Sentry

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            SentrySdk.Init(o =>
            {
                // Tells which project in Sentry to send events to:
                o.Dsn = "https://a98df4d25514031308051bd110811bb5@o4508148841709568.ingest.de.sentry.io/4510001337729104";
                // When configuring for the first time, to see what the SDK is doing:
                o.Debug = true;

                o.Release = AppVersion;
                o.AttachStacktrace = true;
                
                
                o.SetBeforeSend((sentryEvent, hint) =>
                {
                    sentryEvent.User = new SentryUser
                    {
                        Username = Environment.UserName,
                        Id = Environment.MachineName,
                         
                    };
                    
                    // Extra context
                    sentryEvent.SetExtra("OS", Environment.OSVersion.ToString());
                    sentryEvent.SetExtra("CLR", Environment.Version.ToString());
                    sentryEvent.SetExtra("MachineName", Environment.MachineName);
                    sentryEvent.SetExtra("ProcessorCount", Environment.ProcessorCount);
                    
                    return sentryEvent;
                });
            });

            SentrySdk.AddBreadcrumb("Application starting", "app.lifecycle");
            SentrySdk.CaptureMessage("CspProject application started");
            
            #endregion
       
            
            SetupGlobalExceptionHandling();
            
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
         
            // bu kodu test ediyoruz bakalım !
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(
                XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
            
          

            base.OnStartup(e);
            

         
            var notificationService = new NotificationService
            {
                UseWin8NotificationsIfAvailable = true,
                PredefinedNotificationTemplate = NotificationTemplate.ShortHeaderAndLongText,
                CustomNotificationScreen = NotificationScreen.Primary,
                CustomNotificationPosition = NotificationPosition.TopRight,
                ApplicationId = "CspProject" // 2. ApplicationId'yi burada ata!
            };
            

            // 3. Bu yapılandırılmış servisi kaydet - İki yöntemle de:
             ServiceContainer.Default.RegisterService("NotificationService", notificationService);
    
        
            try
            {
                await PerformStartupAsync();
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                if (DXSplashScreen.IsActive) DXSplashScreen.Close();
                MessageBox.Show($"A fatal error occurred during startup: {ex.Message}", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            } 
        }
        
        
        private async Task PerformStartupAsync()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                await dbContext.Database.EnsureCreatedAsync();
                
                await EnsureDefaultTemplatesExist();
                var currentUser = await dbContext.Users.FirstOrDefaultAsync();

                if (currentUser == null)
                {
                    await Dispatcher.InvokeAsync(ShowWelcomeWindow);
                }
                else
                {
                    // Ana pencereyi hemen göster
                    await Dispatcher.InvokeAsync(() => ShowMainWindow(currentUser, dbContext));
                    
                    // E-posta kontrolünü arka planda, kullanıcıyı engellemeden başlat
                    _ = Task.Run(async () => 
                    {
                        using (var backgroundScope = ServiceProvider.CreateScope())
                        {
                            var backgroundDbContext = backgroundScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                            var receiverService = new EmailReceiverService(backgroundDbContext);
                            await receiverService.CheckForApprovalEmailsAsync();
                            AppStateService.LastCheckTime = DateTime.Now;
                        }
                    });
                }
            }
        }
        
        private void ShowMainWindow(User currentUser, ApplicationDbContext dbContext)
        {
            // Yükleme ekranını burada, ana pencere gösterilmeden hemen önce kapatıyoruz.
            if (DXSplashScreen.IsActive) DXSplashScreen.Close();
            
            var mainWindow = new MainWindow(currentUser, dbContext);
            this.MainWindow = mainWindow;
            mainWindow.Show();
        }
        
        private void SetupGlobalExceptionHandling()
        {
            // 1. AppDomain exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                
                // WithScope yerine ConfigureScope kullan
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("exception_source", "AppDomain.UnhandledException");
                    scope.Level = SentryLevel.Fatal;
                });
                
                SentrySdk.CaptureException(exception);
            };

            // 2. WPF UI thread exceptions  
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

            // 3. Task exceptions
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
            var tempWindow = new Window { Content = welcomeView, Width = 500, Height = 400, WindowStartupLocation = WindowStartupLocation.CenterScreen };
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

            // If you want to avoid the application from crashing:
            e.Handled = true;
        }
        
        // --- YENİ EKLENEN METOD ---
        /// <summary>
        /// 'Templates' klasörünün var olduğundan ve varsayılan şablonları içerdiğinden emin olur.
        /// </summary>
        private async Task EnsureDefaultTemplatesExist()
        {
            try
            {
                string exePath = AppDomain.CurrentDomain.BaseDirectory;
                string templateDirectory = Path.Combine(exePath, "Templates");

                // 1. Templates klasörü yoksa oluştur.
                if (!Directory.Exists(templateDirectory))
                {
                    Directory.CreateDirectory(templateDirectory);
                }

                // 2. Varsayılan FMEA şablonunun dosya yolunu belirle.
                string fmeaTemplatePath = Path.Combine(templateDirectory, "FMEA_Template.xlsx");

                // 3. Eğer bu dosya YOKSA, oluştur.
                if (!File.Exists(fmeaTemplatePath))
                {
                    // Yeni oluşturduğumuz metodu kullanarak şablonu byte dizisi olarak al.
                    byte[] templateBytes = FmeaTemplateGenerator.GenerateFmeaTemplateBytes();
            
                    // Bu byte dizisini bir dosya olarak diske yaz.
                    await File.WriteAllBytesAsync(fmeaTemplatePath, templateBytes);
                }
            }
            catch (Exception ex)
            {
                // Yetki hatası vb. durumlarda Sentry'e bildir.
                SentrySdk.CaptureException(ex);
                MessageBox.Show($"Could not create or verify default templates: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
    }
}