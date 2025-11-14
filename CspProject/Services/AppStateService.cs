namespace CspProject.Services
{
    public static class AppStateService
    {
        // En son başarılı kontrolün zamanını tutacak statik bir özellik.
        // `DateTime?` olması, henüz hiç kontrol yapılmadığı durumu da tutabilmemizi sağlar.
        public static DateTime? LastCheckTime { get; set; }
    }
}