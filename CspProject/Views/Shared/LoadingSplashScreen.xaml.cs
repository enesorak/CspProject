using DevExpress.Xpf.Core;

namespace CspProject.Views.Shared
{
    public partial class LoadingSplashScreen : UserControl, ISplashScreen
    {
        
        
  
        
        
        public LoadingSplashScreen()
        {
            InitializeComponent();
        }
        
        // Bu metodu metin güncellemesi için kullanıyoruz.
        public void Progress(object state)
        {
            if (state is string message)
            {
                LoadingMessageTextBlock.Text = message;
            }
        }

        public void CloseSplashScreen()
        {
            // Bu, DXSplashScreen.Close() tarafından otomatik olarak yönetilir.
        }

        // --- YENİ EKLENEN, GEREKLİ METODLAR ---
        // Bu metodlar, yüzdeli bir ilerleme çubuğu kullanmadığımız için şimdilik boş kalacak.
        // Ancak ISplashScreen arayüzü tarafından zorunlu kılındıkları için eklenmeleri gerekir.

        public void SetProgressState(bool isIndeterminate)
        {
            // Bu metodu kullanmadığımız için içi boş kalabilir.
            // İleride yüzdeli bir ilerleme çubuğu eklemek istersek burayı doldururuz.
        }
        
        // Bu metod, yüzdeli ilerleme çubuğunun değerini ayarlamak içindir.
        public void Progress(double value)
        {
            // Bu metodu kullanmadığımız için içi boş kalabilir.
        }
        // --- EKLENEN METODLARIN SONU ---
    }
}