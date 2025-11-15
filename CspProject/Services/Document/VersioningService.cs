// Services/VersioningService.cs

namespace CspProject.Services.Document
{
    public static class VersioningService
    {
        public static string IncrementMajorVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return "1.0.0";
            try
            {
                var parts = version.Split('.').Select(int.Parse).ToList();
                if (parts.Count == 3)
                {
                    parts[0]++; // Ana versiyonu artır
                    parts[1] = 0; // Alt versiyonu sıfırla
                    parts[2] = 0; // Yama versiyonunu sıfırla
                    return string.Join(".", parts);
                }
            }
            catch { /* Hatalı format için geri dönüş */ }
            return "1.0.0";
        }

        public static string IncrementMinorVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return "0.1.0";
            try
            {
                var parts = version.Split('.').Select(int.Parse).ToList();
                if (parts.Count == 3)
                {
                    parts[1]++; // Alt versiyonu artır
                    parts[2] = 0; // Yama versiyonunu sıfırla
                    return string.Join(".", parts);
                }
            }
            catch { /* Hatalı format için geri dönüş */ }
            return "0.1.0";
        }

        public static string IncrementPatchVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return "0.0.1";
            try
            {
                var parts = version.Split('.').Select(int.Parse).ToList();
                if (parts.Count == 3)
                {
                    parts[2]++; // Yama versiyonunu artır
                    return string.Join(".", parts);
                }
            }
            catch { /* Hatalı format için geri dönüş */ }
            return "0.0.1";
        }
    }
}