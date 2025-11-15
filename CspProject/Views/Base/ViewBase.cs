// Views/ViewBase.cs (YENİ DOSYA)

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

        /// <summary>
        /// Database context for this view. Available after Loaded event.
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
            InitializeDbContext();
        }

        /// <summary>
        /// Called when view is unloaded. Override to add custom cleanup.
        /// Don't forget to call base.OnViewUnloaded()!
        /// </summary>
        protected virtual void OnViewUnloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        /// <summary>
        /// Initialize the database context. Called automatically on Loaded.
        /// Can be called manually if needed before Loaded event.
        /// </summary>
        protected virtual void InitializeDbContext()
        {
            if (DbContext != null)
                return; // Already initialized

            _scope = App.ServiceProvider.CreateScope();
            DbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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