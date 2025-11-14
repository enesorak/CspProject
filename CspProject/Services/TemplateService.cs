using System;
using System.Collections.Generic;
using System.Linq;
using CspProject.Services.Templates; // Bir önceki adımda oluşturduğumuz FmeaTemplate ve ITemplate burada

namespace CspProject.Services
{
    public class TemplateService
    {
        private readonly List<ITemplate> _templates;

        public TemplateService()
        {
            // Mevcut tüm şablonlarımızı burada listeliyoruz.
            _templates = new List<ITemplate>
            {
                new FmeaTemplate()
                // Gelecekte yeni bir şablon (örn: new RiskLogTemplate())
                // eklemek için sadece bu listeye ekleyeceğiz.
            };
        }

        /// <summary>
        /// "Templates" ekranı için tüm şablonların bir listesini döndürür.
        /// </summary>
        public IEnumerable<ITemplate> GetAvailableTemplates()
        {
            return _templates;
        }

        /// <summary>
        /// İsmine göre belirli bir şablonu bulur.
        /// </summary>
        public ITemplate? GetTemplateByName(string name)
        {
            return _templates.FirstOrDefault(t => t.Name == name);
        }
    }
}