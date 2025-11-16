// Views/ViewBase.cs - FIXED VERSION

using System.Windows;
using CspProject.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CspProject.Views.Base
{
    /// <summary>
    /// Base class for all views that need database access.
    /// Handles DbContext lifecycle and proper disposal.
    /// </summary>
    public abstract class ViewBase : UserControl, IDisposable
    {
        private IServiceScope? _scope;
        private bool _disposed;
        private bool _isInitialized;

        /// <summary>
        /// Database context for this view. Available after initialization.
        /// </summary>
        protected ApplicationDbContext? DbContext { get; private set; }

        protected ViewBase()
        {
            // Subscribe to lifecycle events
            Loaded += OnViewLoaded;
            Unloaded += OnViewUnloaded;
        }

        /// <summary>
        /// Called when view is loaded. Override to add custom initialization.
        /// Don't forget to call base.OnViewLoaded()!
        /// </summary>
        protected virtual void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            // ✅ Only initialize once
            if (!_isInitialized)
            {
                InitializeDbContext();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Called when view is unloaded. Override to add custom cleanup.
        /// Don't forget to call base.OnViewUnloaded()!
        /// </summary>
        protected virtual void OnViewUnloaded(object sender, RoutedEventArgs e)
        {
            // ✅ Don't dispose on unload - only when view is truly destroyed
            // This allows navigation back to the same view
        }

        /// <summary>
        /// Initialize the database context. Called automatically on first Loaded.
        /// Can be called manually if needed before Loaded event.
        /// </summary>
        protected virtual void InitializeDbContext()
        {
            if (DbContext != null)
                return; // Already initialized

            try
            {
                _scope = App.ServiceProvider.CreateScope();
                DbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                MessageBox.Show($"Failed to initialize database context: {ex.Message}", 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Override this to perform custom cleanup before disposal.
        /// Don't forget to call base.OnDisposing()!
        /// </summary>
        protected virtual void OnDisposing()
        {
            // Override in derived classes for custom cleanup
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Call custom cleanup
                    OnDisposing();

                    // Dispose managed resources
                    DbContext?.Dispose();
                    _scope?.Dispose();
                    
                    // Clear references
                    DbContext = null;
                    _scope = null;
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ViewBase()
        {
            Dispose(false);
        }
    }
}