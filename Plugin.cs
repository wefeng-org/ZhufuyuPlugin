using MG.WeCode;
using MG.WeCode.WeClients;
using Newtonsoft.Json;
using Plugin;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;

namespace ZhufuyuPlugin
{
    internal class Plugin : IPlugin
    {
        private List<string> zfyList = new();
        private Config config = new();
        public string Name => "祝福语插件";

        public string Version => "v1.0.0";

        public string Author => "Byboy";

        public string Description => "祝福语插件";
        /// <summary>
        /// 无需设置,主动发消息时使用
        /// </summary>
        public string OriginId { get; set; }

        public void Initialize()
        {
            if (File.Exists("Plugins/祝福语.inf")) {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("Plugins/祝福语.inf"));
            } else {
                config = new();
                File.WriteAllText("Plugins/祝福语.inf",JsonConvert.SerializeObject(config));
            }
            File.ReadAllText("Plugins/祝福语.txt").Split("\r\n").ToList().ForEach(x => zfyList.Add(x));
            Events.GetReceiveMsg += Events_GetReceiveMsg;
        }

        private async Task Events_GetReceiveMsg(SuperWx.TLS_BFClent sender,List<WeChat.Pb.Entites.AddMsg> e)
        {
            Random r = new();
            foreach (var item in e) {
                if (!config.StartGroupUserName.Contains(item.FromUserName.String_t)) {
                    continue;
                }
                var t = item.Content.String_t;
                var atlist = new List<string>();
                if (item.MsgType == 10000 || item.MsgType == 10003 || item.MsgType == 10002 || item.MsgType == 37 || item.MsgType == 51) {
                    var content = Regex.Replace(t,@"^(.*?)\n<sysmsg","<sysmsg",RegexOptions.IgnoreCase);
                    var document = new XmlDocument() { InnerXml = content };
                    var sysmsg = (XmlElement)document.SelectSingleNode("sysmsg");
                    var t1 = sysmsg?.Attributes["type"].Value;
                    if (t1 == "pat") {
                        var fromusername = sysmsg.SelectSingleNode("pat/fromusername").InnerText;
                        var chatusername = sysmsg.SelectSingleNode("pat/chatusername").InnerText;
                        var pattedusername = sysmsg.SelectSingleNode("pat/pattedusername").InnerText;
                        var patsuffix = sysmsg.SelectSingleNode("pat/patsuffix").InnerText;
                        if (pattedusername == sender.WX.UserLogin.Username) {
                            await WeClient.Messages.SendTextMsg(sender.WX.UserLogin.OriginalId,
                                new List<string> { chatusername },
                                $"{sender.WX.UserLogin.NickName}祝您\n{zfyList[r.Next(zfyList.Count)]}@.",
                                fromusername);
                        }

                    }
                }
                var regex = Regex.Match(item.Content.String_t,@"^(?<username>[\d\w-@]+):\n(?<content>.*?)$",RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (regex.Success) {
                    var username = regex.Groups["username"].Value;
                    if (item.MsgSource != null) {
                        if (item.MsgSource.Contains("atuserlist")) {
                            var msgSourceXml = new XmlDocument();
                            msgSourceXml.LoadXml(item.MsgSource);
                            var atUserList = (XmlElement)msgSourceXml.SelectSingleNode("msgsource").SelectSingleNode("atuserlist");
                            atlist = atUserList.InnerText.Split(',')?.ToList();
                        }
                    }
                    if (atlist.Contains(sender.WX.UserLogin.Username)) {
                         _ = await WeClient.Messages.SendTextMsg(sender.WX.UserLogin.OriginalId,
                            new List<string> { item.FromUserName.String_t },
                            $"{sender.WX.UserLogin.NickName}祝您:\n{zfyList[r.Next(zfyList.Count)]}@.",
                            username);
                    }
                }
            }
        }

        public void Setting()
        {
            //打开文本文件
            Process.Start("notepad.exe","Plugins/祝福语.inf");
        }

        public void Terminate()
        {
            //卸载所有本插件的事件调用
            Events.GetReceiveMsg -= Events_GetReceiveMsg;
        }
    }
}
