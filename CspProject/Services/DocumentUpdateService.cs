namespace CspProject.Services;

public class DocumentUpdateService
{
    // Singleton deseni: Servisin tek bir örneğinin olmasını sağlar
    private static readonly DocumentUpdateService _instance = new DocumentUpdateService();
    public static DocumentUpdateService Instance => _instance;

    private DocumentUpdateService() { }

    // Bir doküman güncellendiğinde bu olay tetiklenecek.
    // int parametresi, güncellenen dokümanın ID'sini taşıyacak.
    public event EventHandler<int>? DocumentUpdated;

    // Arka plan servisinin bu metodu çağırarak olayı tetiklemesini sağlar.
    public void NotifyDocumentUpdated(int documentId)
    {
        DocumentUpdated?.Invoke(this, documentId);
    }
}