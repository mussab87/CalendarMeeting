using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace App.Web.TagHelpers
{
    [HtmlTargetElement("li", Attributes = "asp-active-route")]
    public class ActiveRouteTagHelper : TagHelper
    {
        [HtmlAttributeName("asp-active-route")]
        public string ActiveRoute { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var currentPath = output.Attributes["asp-active-route"]?.Value?.ToString()?.ToLower();
            var requestPath = context.Items["RequestPath"]?.ToString()?.ToLower();
            
            if (!string.IsNullOrEmpty(currentPath) && !string.IsNullOrEmpty(requestPath))
            {
                if (requestPath.StartsWith(currentPath, StringComparison.OrdinalIgnoreCase))
                {
                    var existingClass = output.Attributes["class"]?.Value?.ToString();
                    var newClass = string.IsNullOrEmpty(existingClass) ? "menu-item-here" : $"{existingClass} menu-item-here";
                    output.Attributes.SetAttribute("class", newClass);
                }
            }
        }
    }
}
