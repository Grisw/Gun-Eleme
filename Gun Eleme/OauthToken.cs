using Fiddler;
using JumpKick.HttpLib;
using JumpKick.HttpLib.Builder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Gun_Eleme {
    public class OauthToken {
        public string Url { get; set; }
        public dynamic RequestBody { get; set; }
        public string UserName { get; set; }

        private static JavaScriptSerializer _jsSerializer;
        private static JavaScriptSerializer jsSerializer
        {
            get
            {
                if (_jsSerializer == null)
                    _jsSerializer = new JavaScriptSerializer();
                return _jsSerializer;
            }
        }

        public static void GetOauthTokens(Action<int, OauthToken> onResult) {
            if (FiddlerApplication.IsStarted()) {
                onResult(0, null);
                return;
            }

            FiddlerApplication.Startup(0, FiddlerCoreStartupFlags.Default);
            Proxy oSecureEndpoint = FiddlerApplication.CreateProxyEndpoint(0, true, "localhost");

            SessionStateHandler handler = null;
            handler = (Session oS) => {
                if (oS.fullUrl.StartsWith("https://restapi.ele.me/marketing/promotion/weixin/") && oS.RequestMethod == "POST") {
                    OauthToken token = new OauthToken();
                    token.Url = oS.fullUrl;
                    token.RequestBody = jsSerializer.Deserialize<dynamic>(oS.GetRequestBodyAsString());
                    token.UserName = token.RequestBody["weixin_username"];

                    FiddlerApplication.BeforeRequest -= handler;
                    if (null != oSecureEndpoint) oSecureEndpoint.Dispose();
                    FiddlerApplication.Shutdown();

                    onResult(1, token);
                } else if (oS.fullUrl.StartsWith("http://close.local")) {
                    FiddlerApplication.BeforeRequest -= handler;
                    if (null != oSecureEndpoint) oSecureEndpoint.Dispose();
                    FiddlerApplication.Shutdown();
                }
            };

            FiddlerApplication.BeforeRequest += handler;
        }

        public void Save(string name) {
            File.WriteAllText(name, jsSerializer.Serialize(new { Url = Url, RequestBody = jsSerializer.Serialize(RequestBody), UserName = UserName }));
        }

        public static OauthToken Load(string name) {
            if (!File.Exists(name))
                return null;
            dynamic obj = jsSerializer.Deserialize<dynamic>(File.ReadAllText(name));
            OauthToken token = new OauthToken();
            token.Url = obj["Url"];
            token.RequestBody = jsSerializer.Deserialize<dynamic>(obj["RequestBody"]);
            token.UserName = obj["UserName"];
            return token;
        }

        public void Go(ElemeLuckyMoney eleme, Action<int> onResult, Action onExpired) {
            RequestBody["group_sn"] = eleme.Sn;
            string body = jsSerializer.Serialize(RequestBody);
            RequestBuilder request = null;
            request = Http.Post(Url)
                .Body("json", body)
                .Headers(new Header("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.95 Safari/537.36 MicroMessenger/6.5.2.501 NetType/WIFI WindowsWechat QBCore/3.43.691.400 QQBrowser/9.0.2524.400"))
                .OnSuccess((result) => {
                    try {
                        dynamic ret = jsSerializer.Deserialize<dynamic>(result);
                        onResult(eleme.LuckyNum - ret["promotion_records"].Length);
                    } catch {
                        onExpired();
                    }
                }).OnFail((resp)=> {
                    Thread.Sleep(500);
                    request.Go();
                });
            request.Go();
        }
    }
}
