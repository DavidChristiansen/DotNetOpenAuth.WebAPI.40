﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DotNetOpenAuth.WebAPI.ClientSample.Controllers {
    using System.Net;

    using DotNetOpenAuth.Messaging;
    using DotNetOpenAuth.OAuth2;

    public class HomeController : Controller {
        public IAuthorizationState Authorization { get; private set; }
        public UserAgentClient Client { get; set; }

        public HomeController() {
            var authServer = new AuthorizationServerDescription() {
                AuthorizationEndpoint = new Uri("http://localhost:49810/OAuth/Authorise"),
                TokenEndpoint = new Uri("http://localhost:49810/OAuth/Token"),
            };
            this.Client = new UserAgentClient(authServer, "samplewebapiconsumer", "samplesecret");
            this.Authorization = new AuthorizationState {Callback = new Uri("http://localhost:18529/")};
        }
        public ActionResult Index() {
            if (string.IsNullOrEmpty(Request.QueryString["code"])) return View();
            try {
                this.Client.ProcessUserAuthorization(Request.Url, this.Authorization);
                var valueString = string.Empty;
                if (!string.IsNullOrEmpty(this.Authorization.AccessToken)) {
                    valueString = CallAPI(this.Authorization);
                }
                ViewBag.Values = valueString;
#pragma warning disable 0168
            } catch (ProtocolException ex) {
#pragma warning restore 0168
            }
            return View();
        }

        private string CallAPI(IAuthorizationState authorization) {
            var webClient = new WebClient();
            webClient.Headers["Content-Type"] = "application/json";
            webClient.Headers["X-JavaScript-User-Agent"] = "Demo";
            this.Client.AuthorizeRequest(webClient, this.Authorization);
            var valueString = webClient.DownloadString("http://localhost:49810/api/values");
            return valueString;
        }

        public JsonResult GetValues() {
            bool isOK = false;
            bool requiresAuth = false;
            string redirectURL = "";
            if (Session["AccessToken"] == null) {
                this.Authorization.Scope.AddRange(OAuthUtilities.SplitScopes("http://localhost:49810/api/values"));
                Uri authorizationUrl = this.Client.RequestUserAuthorization(this.Authorization);
                requiresAuth = true;
                redirectURL = authorizationUrl.AbsoluteUri;
                isOK = true;
            } else {
                requiresAuth = false;
            }
            return new JsonResult() {
                Data = new {
                    OK = isOK,
                    RequiresAuth = requiresAuth,
                    RedirectURL = redirectURL
                }
            };
        }
    }
}
