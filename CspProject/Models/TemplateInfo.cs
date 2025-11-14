namespace CspProject.Models
{
    public class TemplateInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        
        // "BuiltIn" (Dahili) mi yoksa "File" (Dosya) mı olduğunu belirtecek.
        public string TemplateType { get; set; } 
        
        // Eğer tip "File" ise, dosya yolunu burada tutacağız.
        // Eğer tip "BuiltIn" ise, "FMEA_TEMPLATE" gibi özel bir anahtar tutacağız.
        public string Identifier { get; set; } 
    }
}