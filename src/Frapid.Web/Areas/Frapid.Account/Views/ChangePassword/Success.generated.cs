﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using Frapid.Account;
    
    #line 1 "..\..\Views\ChangePassword\Success.cshtml"
    using Frapid.i18n;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/ChangePassword/Success.cshtml")]
    public partial class _Views_ChangePassword_Success_cshtml : System.Web.Mvc.WebViewPage<dynamic>
    {
        public _Views_ChangePassword_Success_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 2 "..\..\Views\ChangePassword\Success.cshtml"
  
    ViewBag.Title = "Change Password";
    Layout = ViewBag.LayoutPath + ViewBag.Layout;

            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 6 "..\..\Views\ChangePassword\Success.cshtml"
 if (CultureManager.IsRtl())
{

            
            #line default
            #line hidden
WriteLiteral("    <link");

WriteLiteral(" rel=\"Stylesheet\"");

WriteAttribute("href", Tuple.Create(" href=\"", 177), Tuple.Create("\"", 225)
            
            #line 8 "..\..\Views\ChangePassword\Success.cshtml"
, Tuple.Create(Tuple.Create("", 184), Tuple.Create<System.Object, System.Int32>(Href("~/assets/css/master-page-rtl.css")
            
            #line default
            #line hidden
, 184), false)
);

WriteLiteral("/>\r\n");

            
            #line 9 "..\..\Views\ChangePassword\Success.cshtml"
}
else
{

            
            #line default
            #line hidden
WriteLiteral("    <link");

WriteLiteral(" rel=\"Stylesheet\"");

WriteAttribute("href", Tuple.Create(" href=\"", 268), Tuple.Create("\"", 312)
            
            #line 12 "..\..\Views\ChangePassword\Success.cshtml"
, Tuple.Create(Tuple.Create("", 275), Tuple.Create<System.Object, System.Int32>(Href("~/assets/css/master-page.css")
            
            #line default
            #line hidden
, 275), false)
);

WriteLiteral("/>\r\n");

            
            #line 13 "..\..\Views\ChangePassword\Success.cshtml"
}

            
            #line default
            #line hidden
WriteLiteral("\r\n<div");

WriteLiteral(" class=\"ui attached padded left aligned segment\"");

WriteLiteral(" style=\"min-height: 400px;\"");

WriteLiteral(">\r\n    <div");

WriteLiteral(" class=\"ui container\"");

WriteLiteral(">\r\n        <div");

WriteLiteral(" class=\"ui huge header\"");

WriteLiteral(">Password was changed</div>\r\n        <div");

WriteLiteral(" class=\"ui positive message\"");

WriteLiteral(">Your password was changed successfully.</div>\r\n\r\n        <div");

WriteLiteral(" class=\"ui small basic buttons\"");

WriteLiteral(">\r\n            <a");

WriteLiteral(" href=\"/\"");

WriteLiteral(" class=\"ui basic button\"");

WriteLiteral(">Home</a>\r\n            <a");

WriteLiteral(" href=\"/helpdesk\"");

WriteLiteral(" class=\"ui basic button\"");

WriteLiteral(">Customer Portal</a>\r\n        </div>\r\n    </div>\r\n</div>");

        }
    }
}
#pragma warning restore 1591
