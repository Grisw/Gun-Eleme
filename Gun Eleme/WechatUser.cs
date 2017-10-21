﻿using JumpKick.HttpLib;
using Microsoft.JScript;
using Microsoft.JScript.Vsa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows.Threading;

namespace Gun_Eleme {
    public class WechatUser : User {

        public string UserName { get; set; }
        public string Uin { get; set; }
        public string Sid { get; set; }
        public string Skey { get; set; }
        public string DeviceID { get; set; }
        public string PassTicket { get; set; }
        public dynamic SyncKey { get; set; }

        private string uuid { get; set; }

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
        
        public override void GetQrcode(Action<bool, string> onCompleted) {
            Http.Get("https://login.wx2.qq.com/jslogin?appid=wx782c26e4c19acffb&redirect_uri=https%3A%2F%2Fwx2.qq.com%2Fcgi-bin%2Fmmwebwx-bin%2Fwebwxnewloginpage&fun=new&lang=zh_CN")
                .OnSuccess((result) => {
                    Match match = Regex.Match(result, "uuid = \"([^\"]+)\"");
                    if (match.Success && match.Groups.Count > 1) {
                        string uuid = match.Groups[1].Value;
                        this.uuid = uuid;
                        onCompleted(true, "https://login.weixin.qq.com/qrcode/" + uuid);
                    } else {
                        onCompleted(false, null);
                    }
                }).OnFail((exception) => {
                    onCompleted(false, null);
                }).Go();
        }

        public override void CheckLogin(Action<bool> onCompleted) {
            checkLogin(1, onCompleted);
        }

        private void checkLogin(int tip, Action<bool> onCompleted) {
            string r = Eval.JScriptEvaluate("~new Date();", vsaEngine).ToString();
            Http.Get("https://login.wx2.qq.com/cgi-bin/mmwebwx-bin/login?loginicon=true&uuid=" + uuid + "&tip=" + tip + "&r=" + r)
                .OnSuccess((result) => {
                    Match match = Regex.Match(result, "code=(\\d+);");
                    if (match.Success && match.Groups.Count > 1 && match.Groups[1].Value.Equals("200")) {
                        match = Regex.Match(result, "redirect_uri=\"([^\"]+)\"");
                        if (match.Success && match.Groups.Count > 1) {
                            string redirectUri = match.Groups[1].Value;
                            Http.Get(redirectUri + "&fun=new&version=v2")
                                .OnSuccess((result_1) => {
                                    Random random = new Random();
                                    DeviceID = "e" + ("" + random.Next(0, 9999999) + random.Next(0, 99999999)).PadLeft(15, '0');
                                    Match resultMatch = Regex.Match(result_1, "<wxuin>([^<]+)</wxuin>");
                                    if (resultMatch.Success && resultMatch.Groups.Count > 1)
                                        Uin = resultMatch.Groups[1].Value;
                                    resultMatch = Regex.Match(result_1, "<wxsid>([^<]+)</wxsid>");
                                    if (resultMatch.Success && resultMatch.Groups.Count > 1)
                                        Sid = resultMatch.Groups[1].Value;
                                    resultMatch = Regex.Match(result_1, "<skey>([^<]+)</skey>");
                                    if (resultMatch.Success && resultMatch.Groups.Count > 1)
                                        Skey = resultMatch.Groups[1].Value;
                                    resultMatch = Regex.Match(result_1, "<pass_ticket>([^<]+)</pass_ticket>");
                                    if (resultMatch.Success && resultMatch.Groups.Count > 1) {
                                        PassTicket = resultMatch.Groups[1].Value;
                                        Dictionary<string, string> dic = new Dictionary<string, string>();
                                        Http.Post("https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxinit?r=" + Eval.JScriptEvaluate("~new Date();", vsaEngine).ToString() + "&lang=zh_CN&pass_ticket=" + PassTicket)
                                            .Body("json", jsSerializer.Serialize(
                                                new {
                                                    BaseRequest = new {
                                                        Uin = Uin,
                                                        Sid = Sid,
                                                        Skey = Skey,
                                                        DeviceID = DeviceID
                                                    }
                                                }
                                            )).OnSuccess((result_2) => {
                                                dynamic ret = jsSerializer.Deserialize<dynamic>(result_2);
                                                SyncKey = ret["SyncKey"];
                                                UserName = ret["User"]["NickName"];
                                                onCompleted(true);
                                            }).Go();
                                    }
                                }).Go();
                        }
                    } else if(match.Success && match.Groups.Count > 1 && (match.Groups[1].Value.Equals("201") || match.Groups[1].Value.Equals("408"))) {
                        Thread.Sleep(1000);
                        checkLogin(0, onCompleted);
                    }else {
                        onCompleted(false);
                    }
                }).Go();
        }
        
        public override void Sync(Action<ElemeLuckyMoney> onReceived, Action onExpired) {
            StringBuilder synckey = new StringBuilder();
            foreach(dynamic o in SyncKey["List"]) {
                synckey.Append(o["Key"] + "_" + o["Val"] + "|");
            }
            if (synckey.Length > 0)
                synckey.Remove(synckey.Length - 1, 1);
            string synccheck = "https://webpush.wx2.qq.com/cgi-bin/mmwebwx-bin/synccheck?r=" + Eval.JScriptEvaluate("~new Date();", vsaEngine).ToString() + "&skey=" + HttpUtility.UrlEncode(Skey) + "&sid=" + HttpUtility.UrlEncode(Sid) + "&uin=" + Uin + "&deviceid=" + DeviceID + "&synckey=" + HttpUtility.UrlEncode(synckey.ToString());
            Http.Get(synccheck)
                .OnSuccess((result) => {
                    Match match = Regex.Match(result, "retcode:\"(\\d+)\"");
                    if(match.Success && match.Groups.Count > 1 && match.Groups[1].Value == "0") {
                        match = Regex.Match(result, "selector:\"(\\d+)\"");
                        if (match.Success && match.Groups.Count > 1 && match.Groups[1].Value != "0") {
                            Http.Post("https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxsync?sid=" + Sid + "&skey=" + Skey + "&lang=zh_CN&pass_ticket=" + PassTicket)
                                .Body("json", jsSerializer.Serialize(
                                        new {
                                            BaseRequest = new {
                                                Uin = Uin,
                                                Sid = Sid,
                                                Skey = Skey,
                                                DeviceID = DeviceID
                                            },
                                            SyncKey = SyncKey,
                                            rr = Eval.JScriptEvaluate("~new Date();", vsaEngine).ToString()
                                        }))
                                .OnSuccess((header, result_1) => {
                                    if (!header.AllKeys.Contains("Set-Cookie")) {
                                        onExpired();
                                        return;
                                    }
                                    dynamic obj = jsSerializer.Deserialize<dynamic>(result_1);
                                    SyncKey = obj["SyncKey"];
                                    dynamic[] msgList = obj["AddMsgList"];
                                    foreach (dynamic msg in msgList) {
                                        if (!string.IsNullOrEmpty(msg["Url"]) && ((string)msg["Url"]).StartsWith("https://h5.ele.me/hongbao")) {
                                            ElemeLuckyMoney eleme = new ElemeLuckyMoney();
                                            eleme.Url = msg["Url"];
                                            Match match1 = Regex.Match(msg["Url"], "sn=([^&]+)&");
                                            if (match1.Success && match1.Groups.Count > 1) {
                                                eleme.Sn = match1.Groups[1].Value;
                                            }
                                            match1 = Regex.Match(msg["Url"], "lucky_number=(\\d+)&");
                                            if (match1.Success && match1.Groups.Count > 1) {
                                                eleme.LuckyNum = int.Parse(match1.Groups[1].Value);
                                            }
                                            onReceived(eleme);
                                        } else if (!string.IsNullOrEmpty(msg["Content"])) {
                                            MatchCollection matchCollection = Regex.Matches(msg["Content"], "https://h5\\.ele\\.me/hongbao/#hardware_id=&amp;is_lucky_group=True&amp;lucky_number=(\\d+)&amp;track_id=&amp;platform=\\d+&amp;sn=([^&]+)&amp;theme_id=\\d+&amp;device_id=");
                                            foreach (Match m in matchCollection) {
                                                if (m.Success && m.Groups.Count > 2) {
                                                    ElemeLuckyMoney eleme = new ElemeLuckyMoney();
                                                    eleme.Url = m.Groups[0].Value;
                                                    eleme.LuckyNum = int.Parse(m.Groups[1].Value);
                                                    eleme.Sn = m.Groups[2].Value;
                                                    onReceived(eleme);
                                                }
                                            }
                                        }
                                    }
                                }).Go();
                        }
                        Sync(onReceived, onExpired);
                    } else {
                        onExpired();
                    }
                }).Go();
        }

        public override string GetDisplayName() {
            return "微信登录";
        }
    }
}
