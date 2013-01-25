using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.SharedSource.Dataset.Extensions;
using Sitecore.SharedSource.Dataset.FieldIDs;

namespace Sitecore.SharedSource.Dataset.Pipelines.GetSharepointCredentials
{
    public class DefaultProcessor : IPipelineProcessor<GetSharepointCredentialsArgs>
    {
        public void Process(GetSharepointCredentialsArgs args)
        {
            var ss = args.SharepointQueryItem.GetReferenceField(FieldIDs.Dataset.SharepointQuery.SharepointSite).TargetItem;
            var username = ss[FieldIDs.Core.SharepointCredentials.Username];
            var password = ss[FieldIDs.Core.SharepointCredentials.Password];
            var domain = ss[FieldIDs.Core.SharepointCredentials.Domain];

            if (!string.IsNullOrEmpty(username))
            {
                if (string.IsNullOrEmpty(domain))
                    args.Credentials = new NetworkCredential(username, password);
                else
                    args.Credentials = new NetworkCredential(username, password, domain);
            }
        }
    }
}
