using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;

namespace Sitecore.SharedSource.Dataset.Extensions
{
    public static class TemplateItemExtensions
    {
        /// <summary>
        /// Recursive method to check if the referenced template inherits
        /// from a template called "templateName"
        /// </summary>
        /// <param name="template">The template you are checking the base templates of</param>
        /// <param name="templateName">The name of the inherited template</param>
        /// <returns>True if "template" inherits from a template named "templateName". Otherwise false.</returns>
        public static bool InheritsFrom(this TemplateItem template, string templateName)
        {
            if (template.Name == templateName)
                return true;
            else
            {
                foreach (TemplateItem t in template.BaseTemplates)
                {
                    if (t.ID != Sitecore.TemplateIDs.StandardTemplate)
                    {
                        if (t.InheritsFrom(templateName))
                            return true;
                    }
                }
                
                return false;
            }
        }

        public static bool InheritsFrom(this TemplateItem template, TemplateItem baseTemplate)
        {
            if (template.ID == baseTemplate.ID)
                return true;
            else
            {
                foreach (TemplateItem t in template.BaseTemplates)
                {
                    if (t.ID != Sitecore.TemplateIDs.StandardTemplate)
                    {
                        if (t.InheritsFrom(baseTemplate))
                            return true;
                    }
                }

                return false;
            }
        }
    }
}
