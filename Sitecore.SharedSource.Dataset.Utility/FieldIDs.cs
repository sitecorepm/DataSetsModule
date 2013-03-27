using System;
using Sitecore.Data;

namespace Sitecore.SharedSource.Dataset.FieldIDs
{
    namespace Component
    {
        public static class DatasetRenderer
        {
            public static ID OnErrorContent = new ID("{D0229A22-38AF-43BA-8107-1D88CFD71F99}");
            public static ID Class = new ID("{7F95D711-378D-47BA-8DFC-FFEF643D0819}");
            public static ID Columns = new ID("{F0DB65E4-C6B2-4D37-8510-CF544FDAE221}");
            public static ID CustomFunctionHandler = new ID("{769DFEE1-C042-44E8-A866-DB210207B685}");
            public static ID DefaultContent = new ID("{54FB700D-82E5-439E-A929-C8C9C4075C20}");
            public static ID Select = new ID("{B628BC54-3491-4D93-8064-B725904D8105}");
            public static ID Distinct = new ID("{9E0FA529-597D-47BC-B19B-58F87940F6EA}");
            public static ID Header = new ID("{6C315440-57EC-49BE-85C3-CD25832E3A96}");
            public static ID MulticolumnCollation = new ID("{79C5C8A3-88CD-43C8-9893-A86363D3BD50}");
            public static ID ListItemFields = new ID("{0DD9DD83-33BC-4753-B614-5E2ACFC9412B}");
            public static ID SkipN = new ID("{4873E77A-C2FA-41C7-8C79-55E74DB97396}");
            public static ID ListItemTemplate = new ID("{CC103F58-7BA2-41E1-B1CF-11C9316A49A5}");
            public static ID TopN = new ID("{93356094-F555-433A-A4B0-B70801B7018A}");
            public static ID AltListItemTemplate = new ID("{A39C7623-88FF-4E26-AF20-13D7BCCD866A}");
            public static ID Filter = new ID("{64893C6F-CE2D-45B8-A9CC-2A2D2F6C3A45}");
            public static ID Footer = new ID("{A4EDAC28-8189-43B1-A496-346C1B3014AE}");
            public static ID SortBy = new ID("{13CF18E8-6BB4-4AB9-B1D1-9047BC24E00F}");
            public static ID RandomN = new ID("{2A19ADC9-D384-4D43-BCA4-DD4BEB23C393}");
            public static ID EveryNth = new ID("{1443E887-AE54-4151-8D83-85FC88EDEF9B}");
        }
        public static class DatasetRendererPager
        {
            public static ID PageQueryString = new ID("{B57D977F-6BB0-43CB-8755-6FE65CEB1B86}");
            public static ID ShowFirstLast = new ID("{B957A1EC-DC07-450B-A17A-58AF6AB72373}");
            public static ID FirstLinkText = new ID("{F7A9A50E-0B00-43D3-A008-B9D232C3DB7D}");
            public static ID MaxPages = new ID("{F904D152-D24D-43AA-9A56-E620793F4387}");
            public static ID LastLinkText = new ID("{BE399056-1DA3-4DD7-9D1A-B961078ED924}");
            public static ID PageSize = new ID("{00A896FE-1199-42EB-A3A9-22D5DCE71E5E}");
            public static ID ShowPreviousNext = new ID("{2E05C7C3-3022-494E-BB4A-98C4533BBD11}");
            public static ID PreviousLinkText = new ID("{20F1B843-8044-430E-8A92-3ABE21A4FEB2}");
            public static ID NextLinkText = new ID("{01AA67FC-7B45-40BC-B618-DD62FAA74C7F}");
        }
    }
    namespace Core
    {
        public static class SharepointCredentials
        {
            public static ID Username = new ID("{0E8325F5-D13A-495E-86EC-7D8EC6E77E11}");
            public static ID Password = new ID("{B9D87C8B-6C07-4C0D-BBA3-AB7FE829178D}");
            public static ID Domain = new ID("{0DA50530-7DF6-4E39-BE80-E69992D158EE}");
        }
        public static class SharepointSite
        {
            public static ID WebServiceAsmxUrl = new ID("{348D6D48-1500-4E98-820F-C0AD2A36EB64}");
        }
    }
    namespace Dataset
    {
        public static class BaseDataset
        {
            public static ID CacheTimeoutMinutes = new ID("{3DB01B40-E289-4798-BD14-D4AD089AD28A}");
            public static ID ExceptionCacheTimeoutMinutes = new ID("{722E69EF-76E1-47DD-975A-E0CEBDB081A2}");
            public static ID OnErrorUseCachedData = new ID("{74D379BF-7B8D-4DA1-9609-DF05DBFED193}");
            public static ID AsynchTimeout = new ID("{C0A4E414-7780-479B-8140-72074AF6323A}");
        }
        public static class ContextItemQuery
        {
            public static ID ItemTemplate = new ID("{C2112402-4730-42B0-9E28-770D783A8FF2}");
            public static ID AxesQuery = new ID("{ACD88CD3-5AC5-424E-A360-BC93628742DE}");
        }
        public static class DatabaseArguementsQuery
        {
            public static ID ConnectionStringName = new ID("{47D2594A-6DCA-4343-B7F3-8DA81A49415C}");
            public static ID QueryString = new ID("{76D0A48F-6925-4C26-9490-71101F8F08F9}");
            public static ID Query = new ID("{CE71A926-FCE1-4B97-AEE3-7AD60BB58186}");
        }
        public static class DatabaseQuery
        {
            public static ID ConnectionStringName = new ID("{6139D9FE-40E0-41EC-8A5F-E1CBC1CBEC7E}");
            public static ID Query = new ID("{3C65B697-371F-40FC-BC7D-6F4B30E3D615}");
        }
        public static class ItemsQuery
        {
            public static ID Target = new ID("{D0F3F0EA-5723-4B83-A5D0-D332A092867A}");
            public static ID Query = new ID("{4630A2C2-7D0D-4C75-999E-23DC7B25C870}");
        }
        public static class ItemsSubset
        {
            public static ID ItemTemplate = new ID("{F33E15F1-87C6-4DBE-9DF6-FAD575208E09}");
        }
        public static class SharepointQuery
        {
            public static ID SharepointSite = new ID("{04A51E26-5A73-4AC9-BE1C-1BEC17AC6FDB}");
            public static ID ListName = new ID("{FF2A9BD0-19E8-4307-A14F-C88D8BD997A1}");
            public static ID ViewName = new ID("{F4FC58BB-A865-405D-BB74-078E24AB8422}");
            public static ID RowLimit = new ID("{091EB95E-359B-4BCC-8E37-3F47FD64A4C5}");
            public static ID Query = new ID("{E17958A7-1E57-4D87-929B-77909BCB09C1}");
            public static ID ViewFields = new ID("{9EFAF7DA-BDCE-4366-835D-6E502C622BE4}");
            public static ID QueryOptions = new ID("{B06A27E9-3556-4A09-A50C-8242A5A0AA46}");
        }
        public static class StaticItemSet
        {
            public static ID Items = new ID("{FE1117CD-4B17-4BD1-8C1B-6D476DC9BE19}");
        }
        public static class XmlFeed
        {
            public static ID FeedUrl = new ID("{70A1DAC4-8D75-4E48-AF90-0D6E2402F022}");
            public static ID Username = new ID("{12B3AEF4-587C-4086-8C5E-F45410A2D92A}");
            public static ID Password = new ID("{91CF3F88-1B5B-44F5-B613-A6C4E034D842}");
            public static ID XPathQuery = new ID("{106702F8-DB5B-4E50-AD03-F16C3A088563}");
        }
    }
}
