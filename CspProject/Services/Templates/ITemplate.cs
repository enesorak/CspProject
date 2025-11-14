using DevExpress.Spreadsheet;

namespace CspProject.Services.Templates
{
    /// <summary>
    /// Tüm şablon sınıflarının uyması gereken sözleşme.
    /// </summary>
    public interface ITemplate
    {
        /// <summary>
        /// Şablonun "Templates" ekranında görünecek adı.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Şablonun "Templates" ekranında görünecek açıklaması.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Bu metot, boş bir Excel çalışma kitabını alıp,
        /// o şablona ait tüm stilleri, formülleri ve kuralları uygular.
        /// </summary>
        void Apply(IWorkbook workbook);
    }
}