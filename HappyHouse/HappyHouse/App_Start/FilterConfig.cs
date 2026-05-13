using System.Web.Mvc;
using HappyHouse.App_Start;

namespace HappyHouse
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());

            // Trim strings after model binding for all actions (skips [AllowHtml] properties)
            filters.Add(new HappyHouse.App_Start.TrimModelStringsFilter());
        }
    }
}
