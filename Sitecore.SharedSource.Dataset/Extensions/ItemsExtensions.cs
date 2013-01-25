using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;

namespace Sitecore.SharedSource.Dataset.Extensions
{
    public static class ItemsExtensions
    {
        public static IEnumerable<Item> WhereApprovedOrNotInWorkflow(this IEnumerable<Item> list)
        {
            return list.Where(delegate(Item i)
                    {
                        if (i.State == null)
                            return true;
                        return i.State.GetWorkflowState().FinalState;
                    });
        }
    }
}
