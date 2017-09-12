using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetingMsgSender
{
    class MsgContentBLL : PersonImporting.BLL.DBBaseBLL
    {
        protected PMS.IBLL.IS_SMSContentBLL ContentBLL;

        public MsgContentBLL()
        {
            ContentBLL = new PMS.BLL.S_SMSContentBLL();
        }

        public List<string> CheckMsgContent(DateTime lastTime)
        {
            List<string> msgContent = new List<string>();
            //找出晚于lastTime的所有短信
            var msgContentList = ContentBLL.GetListBy(c => c.SendDateTime.CompareTo(lastTime) >= 0).ToList();
            //找出会商通知
            var contentList = msgContentList.FindAll(c => c.SMSContent.IndexOf("会商通知") > -1).ToList();
            foreach(var temp in contentList)
            {
                msgContent.Add(temp.SMSContent);
            }
            return msgContent;
        }
    }
}
