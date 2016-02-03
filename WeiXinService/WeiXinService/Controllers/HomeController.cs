using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using Newtonsoft.Json;
using System.Xml;

namespace WeiXinService.Controllers
{
    public class HomeController : Controller
    {
        const string Token = "zwn20160203";
        string AppID = "";
        string AppSercert = "";
        DateTime LastGetTokenTime;
        string WeiXinTokenStr = "";
        string getTokenUrl = "https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}";
        public void Index()
        {
            if (Request.HttpMethod.ToLower() == "get")
            {
                CheckAsDevloper();
            }
            else
            {
                if (LastGetTokenTime == null || (DateTime.Now - LastGetTokenTime).TotalSeconds >= 2 * 60 * 60)
                {
                    GetWeiXinToken();
                }
                DealUserQuest();
            }
        }
        /// <summary>
        /// 认证成为开发者
        /// </summary>
        [HttpGet]
        public void CheckAsDevloper()
        {
            try
            {
                string echoStr = Request.QueryString["echostr"].ToString();
                string signaTure = Request.QueryString["signature"].ToString();
                string timeStamp = Request.QueryString["timestamp"].ToString();
                string nonce = Request.QueryString["nonce"].ToString();
                string[] ArrTemp = { Token, timeStamp, nonce };
                Array.Sort(ArrTemp);
                string tempStr = string.Join("", ArrTemp);
                tempStr = FormsAuthentication.HashPasswordForStoringInConfigFile(tempStr, "SHA1");
                tempStr = tempStr.ToLower();
                if (tempStr == signaTure)
                {
                    if (!String.IsNullOrEmpty(echoStr))
                    {
                        Response.Write(echoStr);
                        Response.End();
                    }
                }
            }
            catch (Exception ex)
            {
                Response.Write(DateTime.Now.ToString() + " " + ex.Message);
                Response.End();
            }
        }
        /// <summary>
        /// 获取微信Token
        /// </summary>
        public void GetWeiXinToken()
        {
            if (string.IsNullOrEmpty(AppID) || string.IsNullOrEmpty(AppSercert))
            {
                GetAppIDAndAppSercert();
            }
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(string.Format(getTokenUrl, AppID, AppSercert));
            req.Method = "GET";
            using (WebResponse wr = req.GetResponse())
            {
                HttpWebResponse myResponse = (HttpWebResponse)req.GetResponse();
                StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
                string content = reader.ReadToEnd();
                if (!String.IsNullOrEmpty(content))
                {
                    WeiXinToken wxToken = JsonConvert.DeserializeObject<WeiXinToken>(content);
                    WeiXinTokenStr = wxToken.access_token;
                    LastGetTokenTime = DateTime.Now;
                }
            }
        }
        /// <summary>
        /// 从本地配置文件读取AppID和AppSercert
        /// </summary>
        public void GetAppIDAndAppSercert()
        {
            try
            {
                AppID = ConfigurationManager.AppSettings["AppID"].ToString();
                AppSercert = ConfigurationManager.AppSettings["AppSecret"].ToString();
            }
            catch (Exception ex)
            {
            }
        }
        public void DealUserQuest()
        {
            WeiXinTextMessage wxMsg = GetWXMessage();
            string res = "";
            if (!String.IsNullOrEmpty(wxMsg.EventName) && wxMsg.EventName.Trim() == "subScribe")
            {

            }
            else
            {
                res = sendTextMessage(wxMsg, "i have got your msg,your msg is:" + wxMsg.Content);
            }
            Response.Write(res);
            Response.End();
        }
        /// <summary>
        /// 获取微信请求消息
        /// </summary>
        /// <returns></returns>
        public WeiXinTextMessage GetWXMessage()
        {
            WeiXinTextMessage wxMsg = new WeiXinTextMessage();
            StreamReader str = new StreamReader(Request.InputStream, Encoding.UTF8);
            XmlDocument xml = new XmlDocument();
            xml.Load(str);
            wxMsg.ToUserName = xml.SelectSingleNode("xml").SelectSingleNode("ToUserName").InnerText;
            wxMsg.FromUserName = xml.SelectSingleNode("xml").SelectSingleNode("FromUserName").InnerText;
            wxMsg.MsgType = xml.SelectSingleNode("xml").SelectSingleNode("MsgType").InnerText;
            if (wxMsg.MsgType.Trim() == "text")
            {
                wxMsg.Content = xml.SelectSingleNode("xml").SelectSingleNode("Content").InnerText;
            }
            if (wxMsg.MsgType.Trim() == "event")
            {
                wxMsg.EventName = xml.SelectSingleNode("xml").SelectSingleNode("Event").InnerText;
            }
            return wxMsg;
        }
        /// <summary>    
        /// 发送文字消息    
        /// </summary>    
        /// <param name="wx">获取的收发者信息    
        /// <param name="content">内容    
        /// <returns></returns>    
        private string sendTextMessage(WeiXinTextMessage wx, string content)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(AppDomain.CurrentDomain.BaseDirectory + "\\MsgFolder\\TextMsg.xml");
            xml.SelectSingleNode("xml").SelectSingleNode("ToUserName").InnerText = xml.SelectSingleNode("xml").SelectSingleNode("ToUserName").InnerText.Replace("toUser", wx.FromUserName);
            xml.SelectSingleNode("xml").SelectSingleNode("FromUserName").InnerText = xml.SelectSingleNode("xml").SelectSingleNode("FromUserName").InnerText.Replace("fromUser", wx.ToUserName);
            xml.SelectSingleNode("xml").SelectSingleNode("CreateTime").InnerText = ConvertDateTimeInt(DateTime.Now).ToString();
            xml.SelectSingleNode("xml").SelectSingleNode("MsgType").InnerText = xml.SelectSingleNode("xml").SelectSingleNode("MsgType").InnerText.Replace("MsgType", wx.MsgType);
            xml.SelectSingleNode("xml").SelectSingleNode("Content").InnerText = xml.SelectSingleNode("xml").SelectSingleNode("Content").InnerText.Replace("Content", content);
            string res = xml.InnerXml;
            return res;
        }
        /// <summary>  
        /// 时间戳转为C#格式时间  
        /// </summary>  
        /// <param name="timeStamp">Unix时间戳格式</param>  
        /// <returns>C#格式时间</returns>  
        public static DateTime GetTime(string timeStamp)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timeStamp + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }
        /// <summary>  
        /// DateTime时间格式转换为Unix时间戳格式  
        /// </summary>  
        /// <param name="time"> DateTime时间格式</param>  
        /// <returns>Unix时间戳格式</returns>  
        public static int ConvertDateTimeInt(System.DateTime time)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            return (int)(time - startTime).TotalSeconds;
        }
    }
    public class WeiXinToken
    {
        public string access_token;
        public string expires_in;
    }
    public class WeiXinTextMessage
    {
        public string FromUserName { get; set; }
        public string ToUserName { get; set; }
        public string MsgType { get; set; }
        public string EventName { get; set; }
        public string Content { get; set; }
        public string EventKey { get; set; }
    }
}
