using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Text;
using Sitecore.Web;

using Sitecore.Workflows;
using Sitecore.Data;

namespace Sitecore.SharedSource.Dataset.Extensions
{
    public static class ItemExtensions
    {
        private static string _rootSiteTemplateName = "Site";

        // Recursive method that walks up the parent tree to find the
        // site root of the current item.
        public static Item ParentSiteItem(this Item current_item)
        {
            return current_item.ParentOfType(_rootSiteTemplateName);
        }
        public static Item ParentOfType(this Item item, string templateName)
        {
            if (item == null || item.TemplateName == templateName || item.Template.InheritsFrom(templateName))
                return item;
            else
                return item.Parent.ParentOfType(templateName);
        }

        public static string RenderField(this Item i, Sitecore.Data.ID id)
        {
            return i.RenderField(id.ToString());
        }
        public static string RenderField(this Item i, Sitecore.Data.ID id, string vcParameters)
        {
            return i.RenderField(id.ToString(), vcParameters);
        }

        public static string RenderField(this Item i, string vcFieldName)
        {
            return i.RenderField(vcFieldName, null);
        }
        public static string RenderField(this Item i, string vcFieldName, string vcParameters)
        {
            if (String.IsNullOrEmpty(vcParameters))
                return Sitecore.Web.UI.WebControls.FieldRenderer.Render(i, vcFieldName);
            else
                return Sitecore.Web.UI.WebControls.FieldRenderer.Render(i, vcFieldName, vcParameters);
        }

        /// <summary>
        /// Use FieldCoalesce to pass a list of fields (by name or id) on an item and 
        /// return the first one whose VALUE is NOT NULL or EMPTY.
        /// </summary>
        public static Field FieldCoalesce(this Item i, Sitecore.Data.ID[] fields)
        {
            return FieldCoalesce<Sitecore.Data.ID>(i, fields);
        }
        public static Field FieldCoalesce(this Item i, string[] fields)
        {
            return FieldCoalesce<string>(i, fields);
        }
        private static Field FieldCoalesce<T>(Item i, T[] fields)
        {
            Field f = null;

            foreach (T field in fields)
            {
                object o = (object)field;

                if (o is string)
                    f = i.Fields[(string)o];
                else if (o is Sitecore.Data.ID)
                    f = i.Fields[(Sitecore.Data.ID)o];

                if (f != null && !string.IsNullOrEmpty(f.Value))
                    break;
                else
                    f = null;
            }

            return f;
        }

        /// <summary>
        /// Filter the returned list of children to include only those whose
        /// template matches one of the supplied templates
        /// </summary>
        /// <param name="i"></param>
        /// <param name="templateFilter"></param>
        /// <returns></returns>
        public static List<Item> GetChildren(this Item i, TemplateItem[] filters)
        {
            List<Item> list = new List<Item>();
            if (filters.Length > 0)
                list.AddRange(i.GetChildren().InnerChildren.Where(x => filters.Any(f=>f.ID == x.TemplateID)));
            else
                list.AddRange(i.GetChildren().InnerChildren);
            return list;
        }

        public static ReferenceField GetReferenceField(this Item i, Sitecore.Data.ID id)
        {
            return (ReferenceField)i.Fields[id];
        }
        public static ReferenceField GetReferenceField(this Item i, string vcFieldName)
        {
            return (ReferenceField)i.Fields[vcFieldName];
        }

        public static MultilistField GetMultilistField(this Item i, Sitecore.Data.ID id)
        {
            return (MultilistField)i.Fields[id];
        }
        public static MultilistField GetMultilistField(this Item i, string vcFieldName)
        {
            return (MultilistField)i.Fields[vcFieldName];
        }

        public static Field GetField(this Item i, Sitecore.Data.ID id)
        {
            return new Field(id, i);
        }
        public static Field GetField(this Item i, string fieldName)
        {
            var id = Sitecore.Data.Managers.TemplateManager.GetFieldId(fieldName, i.TemplateID, i.Database);
            return GetField(i, id);
        }

        //public static GroupOfLinks GetGroupOfLinksField(this Item i, Sitecore.Data.ID id)
        //{
        //    return GroupOfLinks.Create(i[id]);
        //}

        ///// <summary>
        ///// This gets the children of the current item taking into account SiteSectionGroups
        ///// </summary>
        ///// <param name="navItem"></param>
        ///// <returns></returns>
        //public static Item[] GetNavItemChildren(this Item navItem)
        //{
        //    Item[] children;
        //    if (navItem.Fields.Contains(IDs.SiteSectionGroup.SiteSections))
        //        children = navItem.GetMultilistField(IDs.SiteSectionGroup.SiteSections).GetItems();
        //    else
        //        children = navItem.GetChildren().ToArray();
        //    return children;
        //}

        

        public static IWorkflow GetWorkflow(this Item item)
        {
            IWorkflowProvider workflowProvider = item.Database.WorkflowProvider;
            if ((workflowProvider != null) && (workflowProvider.GetWorkflows().Length > 0))
            {
                return workflowProvider.GetWorkflow(item);
            }
            return null;
        }

        /// <summary>
        /// credit: http://briancaos.wordpress.com/2011/01/14/create-and-publish-items-in-sitecore/
        /// </summary>
        public static void PublishItem(this Sitecore.Data.Items.Item item, bool deep)
        {
            // The publishOptions determine the source and target database,
            // the publish mode and language, and the publish date
            var publishOptions = new Sitecore.Publishing.PublishOptions(item.Database,
                                                     Database.GetDatabase("web"),
                                                     Sitecore.Publishing.PublishMode.SingleItem,
                                                     item.Language,
                                                     System.DateTime.Now);  // Create a publisher with the publishoptions
            Sitecore.Publishing.Publisher publisher = new Sitecore.Publishing.Publisher(publishOptions);

            // Choose where to publish from
            publisher.Options.RootItem = item;

            // Publish children as well?
            publisher.Options.Deep = deep;

            // Do the publish!
            publisher.Publish();
        }

    }
}
