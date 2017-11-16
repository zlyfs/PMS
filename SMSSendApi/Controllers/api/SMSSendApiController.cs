using PMS.Model.ApiMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using PMS.Model.ViewModel;
using HttpHelper;
using System.Text;
using Common;
using PMS.IBLL;
using PMS.Model;
using PMS.Model.SMSModel;
using ISMS;
using PMS.Model.EqualCompare;
using System.Security.Cryptography;
using SMSFactory;
using System.Collections.Specialized;

namespace SMSSendApi.Controllers.api
{
    public class SMSSendApiController : ApiController
    {
        ISMSSend smsSendBLL { get; set; }
        //SMSFactory.SMSSend smsSendBLL = new SMSFactory.SMSSend();
        IP_PersonInfoBLL personBLL { get; set; }
        IP_DepartmentInfoBLL departmentBLL { get; set; }
        PMS.IBLL.IUserInfoBLL userBLL { get; set; }
        PMS.IBLL.IS_SMSMissionBLL smsMissionBLL { get; set; }
        PMS.IBLL.IP_GroupBLL groupBLL { get; set; }
        /// <summary>
        /// 通过spring实现Ioc
        /// </summary>
        //PMS.IBLL.IUserInfoBLL userBLL;
        //PMS.IBLL.IS_SMSMissionBLL smsMissionBLL;
        //PMS.IBLL.IP_GroupBLL groupBLL;

        string cookie_sessionId = null;

        public SMSSendApiController()
        {
            //读取http配置类
           var config =new Common.Config.HttpConfig();
            //读取cookie_session id值
            this.cookie_sessionId = config.Cookie_SessionId;


        }

        [HttpGet]
        public string Index()
        {
            return "测试api";
        }

        protected void Insert2Cookie(string id)
        {

        }

        /// <summary>
        /// 将userid存入memcached中，并返回在memcached中的key
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected string  Set2Memcached(string id)
        {
            //将userid存入memcached，并写入cookie，将cookie返回
            var session_guid =  HttpHelper.SessionHelper.SetMemcached(id);
            var temp = string.Format("memcached写入|{0}", session_guid);
            Common.LogHelper.WriteError(temp);
            return session_guid;
        }
        /// <summary>
        /// 发送方法,通过自写实现
        /// </summary>
        /// <param name="sendModel"></param>
        /// <returns></returns>
        [HttpPost]
        public SendResponseModel SendPort(SendResultModel sendModel)
        {
            //模拟一个post请求
            SendResponseModel sendResponseModel = new SendResponseModel { ResponseDate = DateTime.Now };
                      
            #region 各种信息检查判断,包括短信字数；所列任务、群组、部门是否存在；用户账户是否存在
            //1 判断传入的SendResultModel是否包含必须的内容—Q
            //1.1 短信内容为空或字数超过800不执行发送
            if (sendModel.Content == null || sendModel.Content.Length + 9 >= 800 || sendModel.Content.Equals(""))
            {
                sendResponseModel.ResultCode = Convert.ToString(Convert.ToInt32(PMS.Model.Enum.ResultCodeEnum_SendAPI.contentError));
                return sendResponseModel;

            }
            //1.2 传入的任务为空或任务不存在
            if(sendModel.SMSMissionNames==null)
            {
                sendResponseModel.ResultCode = Convert.ToString(Convert.ToInt32(PMS.Model.Enum.ResultCodeEnum_SendAPI.missingMission));
                return sendResponseModel;
            }
            if (!smsMissionBLL.AddValidation(sendModel.SMSMissionNames))
            {
                sendResponseModel.ResultCode = Convert.ToString(Convert.ToInt32(PMS.Model.Enum.ResultCodeEnum_SendAPI.missionError));
                return sendResponseModel;
            }
            //1.3 传入的群组不为空但群组不存在
            if (sendModel.GroupNames != null)
            {
                string[] groupname = sendModel.GroupNames.Split(';');
                foreach (var item in groupname)
                {
                    if (item == "" && !groupBLL.AddValidation(item))
                    {
                        sendResponseModel.ResultCode = Convert.ToString(Convert.ToInt32(PMS.Model.Enum.ResultCodeEnum_SendAPI.groupError));
                        return sendResponseModel;
                    }
                }
            }
            //1.4 传入的部门不为空但群组不存在
            if (sendModel.DepartmentNames != null)
            {
                string[] departmentname = sendModel.DepartmentNames.Split(';');
                foreach (var item in departmentname)
                {
                    if (item == "" && !departmentBLL.AddValidation(item))
                    {
                        sendResponseModel.ResultCode = Convert.ToString(Convert.ToInt32(PMS.Model.Enum.ResultCodeEnum_SendAPI.departmentError));
                        return sendResponseModel;
                    }
                }
            }
            // 1.5 根据传入的SendResultModel的账号及密码（md5）判断是否拥有权限（先写在这里以后通过过滤器实现）—Q
            if (sendModel.Account == null || sendModel.Pwd == null)
            {
                sendResponseModel.ResultCode = Convert.ToString(Convert.ToInt32(PMS.Model.Enum.ResultCodeEnum_SendAPI.missingAccountOrPwd));
                return sendResponseModel;
            }
            UserInfo userInfo = userBLL.GetListBy(u => u.UName == sendModel.Account && u.DelFlag == false).FirstOrDefault();

            if (userInfo == null)
            {
                sendResponseModel.ResultCode = Convert.ToString(Convert.ToInt32(PMS.Model.Enum.ResultCodeEnum_SendAPI.accountError));
                return sendResponseModel;
            }
            if (userInfo.UPwd != Encryption.MD5Encryption(sendModel.Pwd))
            {
                sendResponseModel.ResultCode = Convert.ToString(Convert.ToInt32(PMS.Model.Enum.ResultCodeEnum_SendAPI.accountError));
                return sendResponseModel;
            }
            #endregion
            #region 进行匹配发送
            //2.发送参数准备
            PMS.Model.ViewModel.ViewModel_Message model = new ViewModel_Message();
            //2.1传递信息内容
            model.Content = sendModel.Content;
            //2.2查询传递用户ID
            model.UID = userBLL.GetListBy(u => u.UName.Equals(sendModel.Account)).FirstOrDefault().ID;
            //2.3当有群组信息时，将群组信息放入发送模型
            if(sendModel.GroupNames!=null)
            {
                string[] groupnames = sendModel.GroupNames.Split(';');
                int i = 0;
                model.GroupIds = new int[groupnames.Count()];
                foreach (var temp in groupnames)
                {
                    var TempG = groupBLL.GetListBy(g => g.GroupName.Equals(temp)).FirstOrDefault();
                    //model.GroupIds[i] = new int();
                    model.GroupIds[i] = TempG.GID;
                    i++;
                }

            }
            //2.4当有部门信息时，将部门信息放入发送模型
            if (sendModel.DepartmentNames != null)
            {
                string[] departmentnames = sendModel.DepartmentNames.Split(';');
                int i = 0;
                model.DepartmentIds=new int[departmentnames.Count()];
                foreach(var temp in departmentnames)
                {
                    var TempD = departmentBLL.GetListBy(d => d.DepartmentName.Equals(temp)).FirstOrDefault();
                    model.DepartmentIds[i] = TempD.DID;
                    i++;
                }

            }
            //2.5查询传递任务ID
            model.SMSMissionID = smsMissionBLL.GetListBy(m => m.SMSMissionName.Equals(sendModel.SMSMissionNames)).FirstOrDefault().SMID.ToString();
            #endregion
            //3.建立发送模型
            PMS.Model.CombineModel.SendAndMessage_Model combine_model = new PMS.Model.CombineModel.SendAndMessage_Model();
            combine_model.Model_Message = model;
            //4.建立反馈模型
            PMS.IModel.ISMSModel_Receive receive = new SMSModel_Receive();
            //5.发送，并回收反馈信息
            var isOk_Send = DoSendNow(combine_model, out receive);
            //6.返回结果信息
            //6.1容错机制
            string ReceiveCode = null;
            try
            {
                ReceiveCode = (receive as SMSModel_Receive).result;
            }
            catch(Exception e)
            {
                ReceiveCode = Convert.ToString(Convert.ToInt32(PMS.Model.Enum.ResultCodeEnum_SendAPI.sendError)) ;
            }
            //6.2 返回信息
            return new SendResponseModel()
            {
                ResponseDate = DateTime.Now,
                ResultCode = ReceiveCode
            };
            



        }

        protected List<P_PersonInfo> GetPersonListByGroupDepartment(string dids, string gids, out int rowCount, int pageSize = -1, int pageIndex = -1)
        {

            List<int> list_dids = new List<int>();
            List<int> list_gids = new List<int>();
            if (dids != "")
            {
                var list_dids_temp = (from g in dids.Split(',')
                                      where g != ""
                                      select g).ToList();
                list_dids_temp.ForEach(g => list_dids.Add(int.Parse(g)));
            }
            if (gids != "")
            {
                var list_gids_temp = (from g in gids.Split(',')
                                      where g != ""
                                      select g).ToList();
                list_gids_temp.ForEach(g => list_gids.Add(int.Parse(g)));
            }


            //2 根据department以及group的id查询其对应的Person对象集合
            List<P_PersonInfo> list_person = new List<P_PersonInfo>();
            var list_department = departmentBLL.GetListByIds(list_dids);
            list_department.ForEach(d => list_person.AddRange(d.P_PersonInfo));
            var list_group = groupBLL.GetListByIds(list_gids);
            list_group.ForEach(g => list_person.AddRange(g.P_PersonInfo));

            //3 将联系人集合去重
            list_person = list_person.Distinct(new P_PersonEqualCompare()).ToList().Select(p => p.ToMiddleModel()).ToList();
            list_person = list_person.OrderByDescending(a => a.isVIP).ToList();
            rowCount = list_person.Count();

            if (pageIndex != -1 && pageSize != -1)
            {
                //分页
                list_person = list_person.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            }
            return list_person;
        }

        /// <summary>
        /// 立即发送
        /// </summary>
        /// <param name="model"></param>
        /// <param name="receive"></param>
        /// <returns></returns>
        private bool DoSendNow(PMS.Model.CombineModel.SendAndMessage_Model model, out /*SMSModel_Receive*/ PMS.IModel.ISMSModel_Receive receive)
        {
            smsSendBLL = new SMSFactory.SMSSend();
            //1 根据选定的群组及部门获取相应的联系人
            var list_PersonPhonesByGroupAndDepartment = smsSendBLL.GetFinalPersonPhoneList(model.Model_Message, GetPersonListByGroupDepartment);

            ////2 获取临时联系人电话集合
            //var list_tempPersonPhones = smsSendBLL.AddAndGetTempPersons(model.Model_Message, personBLL, groupBLL);

            ////2.2 获取最终的联系人电话集合
            //list_PersonPhonesByGroupAndDepartment.AddRange(list_tempPersonPhones);

            var list_phones = list_PersonPhonesByGroupAndDepartment;
            var isEmpty = list_phones.Count() == 0 ? true : false;
            //LogHelper.WriteError("list_phones.Count()"+ list_phones.Count().ToString());
            if (isEmpty)
            {
                LogHelper.WriteError("发送失败：没有号码报错退出");
                receive = new SMSModel_Receive();
                return false;
            }
            //3 转成发送对象
            var sendMsg = smsSendBLL.ToSendModel(model.Model_Message, list_phones);
            /*步骤四
                    生成提交对象及短信及作业对象
                    由SMSFactory进行短信提交操作（并选择延时/立刻发送）
            */
            //4 短信发送
            //注意：desc:定时时间格式错误;
            //      result:定时时间格式错误
            //PMS.Model.CombineModel.SendAndMessage_Model sendandMsgModel = new PMS.Model.CombineModel.SendAndMessage_Model() { Model_Message = model, Model_Send = sendMsg };
            model.Model_Send = sendMsg;
            //PMS.Model.Message.BaseResponse response = new PMS.Model.Message.BaseResponse();
            smsSendBLL.SendMsg(model, out /*response*/receive, false);
            //receive = new SMSModel_Receive();
            LogHelper.WriteError("短信发送,成功");
            return true;
        }

        /// <summary>
        /// 带有存储功能的发送
        /// </summary>
        /// <param name="sendModel"></param>
        /// <returns></returns>
        [HttpPost]
        public SendResponseModel DoSend(SendResultModel sendModel)
        {
            //return null;
            //模拟一个post请求
            SendResponseModel sendResponseModel = new SendResponseModel { ResponseDate = DateTime.Now };
            
            #region 各种判断
            //1 判断传入的SendResultModel是否包含必须的内容—Q
            //1.1 短信内容为空或字数超过800不执行发送
            if (sendModel.Content == null && sendModel.Content.Length + 9 >= 800)
            {
                sendResponseModel.ResultCode = Convert.ToString(PMS.Model.Enum.ResultCodeEnum_SendAPI.contentError);
                return sendResponseModel;

            }
            //1.2 传入的任务为空或任务不存在
            else if (!smsMissionBLL.AddValidation(sendModel.SMSMissionNames))
            {
                sendResponseModel.ResultCode = Convert.ToString(PMS.Model.Enum.ResultCodeEnum_SendAPI.missionError);
                return sendResponseModel;
            }
            //1.3 传入的群组不为空但群组不存在
            if (sendModel.GroupNames != null)
            {
                string[] groupname = sendModel.GroupNames.Split(';');
                foreach (var item in groupname)
                {
                    if (item == "" && !groupBLL.AddValidation(item))
                    {
                        sendResponseModel.ResultCode = Convert.ToString(PMS.Model.Enum.ResultCodeEnum_SendAPI.groupError);
                        return sendResponseModel;
                    }
                }
            }
            // 1.4 根据传入的SendResultModel的账号及密码（md5）判断是否拥有权限（先写在这里以后通过过滤器实现）—Q
            UserInfo userInfo = userBLL.GetListBy(u => u.UName == sendModel.Account && u.DelFlag == false).FirstOrDefault();

            if (userInfo == null && userInfo.UPwd != Encryption.MD5Encryption(sendModel.Pwd))
            {
                sendResponseModel.ResultCode = Convert.ToString(PMS.Model.Enum.ResultCodeEnum_SendAPI.accountError);
                return sendResponseModel;
            }
            #endregion

            //根据user获取id
            var userid = userInfo.ID;

            var session_id= this.Set2Memcached(userid.ToString());

            CookieCollection cookies = new CookieCollection();
            cookies.Add(
                        new Cookie()
                        {
                            Name = cookie_sessionId,
                            Value = session_id,
                            Expires = DateTime.Now.AddHours(3),
                            Domain = "nmefc"
                        });

            var temp_smsmission= smsMissionBLL.GetListBy(s => s.SMSMissionName == sendModel.SMSMissionNames).FirstOrDefault();

            //***测试用，之后用传入的参数替代***
            //测试-2，只传入任务及内容
            var sendObj = new ViewModel_Message()
            {
                Content = sendModel.Content,
                SMSMissionID = temp_smsmission.SMID.ToString()  
            };
            IHttpProvider httpProvider = new HttpProvider();

            //2 请求短信发送action url
            HttpResponseParameter responseParameter = httpProvider.Excute(new HttpRequestParameter
            {
                Url = "128.5.10.57/SMS/Send/DoSend/",
                IsPost = true,
                Encoding = Encoding.UTF8,
                JsonData = Common.SerializerHelper.SerializerToString(sendObj),
                Cookie = new HttpCookieType()
                {
                    CookieCollection = cookies
                }
            });
            var response_content =string.Format("responseParameter|{0}",Common.SerializerHelper.SerializerToString(responseParameter));
            Common.LogHelper.WriteError(response_content); 
            //3 将返执行发送后的结果转换为SendResponseModel，序列化后返回
            //未完成
            return new SendResponseModel() { };
        }

        public string DirectSend(PMS.Model.CombineModel.SendAndMessage_Model model)
        {
            #region 选联系人
            smsSendBLL = new SMSFactory.SMSSend();
            //1 根据选定的群组及部门获取相应的联系人
            var list_PersonPhonesByGroupAndDepartment = smsSendBLL.GetFinalPersonPhoneList(model.Model_Message, GetPersonListByGroupDepartment);

            ////2 获取临时联系人电话集合
            //var list_tempPersonPhones = smsSendBLL.AddAndGetTempPersons(model.Model_Message, personBLL, groupBLL);

            ////2.2 获取最终的联系人电话集合
            //list_PersonPhonesByGroupAndDepartment.AddRange(list_tempPersonPhones);

            var list_phones = list_PersonPhonesByGroupAndDepartment;
            var isEmpty = list_phones.Count() == 0 ? true : false;
            LogHelper.WriteError("发送人数：" + list_phones.Count().ToString());
            if (isEmpty)
            {
                return "没有号码报错退出";
            }
            //3 转成发送对象
            var sendMsg = smsSendBLL.ToSendModel(model.Model_Message, list_phones);
            #endregion

            #region 发送部分
            String _serverURL = "http://wt.3tong.net/http/sms/Submit";
            String _account = "dh74381";
            String _passWord = md5("uAvb3Qey");
            String _smsId = "";
            String _smsContent = model.Model_Message.Content;
            string _sign = "【国家海洋预报台】";
            string _subCode = "74431";
            string _sendTime = "";
            String _phones = null;
            _phones = list_phones[0].ToString();
            for(int i=1;i< list_phones.Count(); i++)
            {
                _phones = _phones + "," + list_phones[i].ToString();
            }
            
            String _data = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                                    + "<message>"
                                        + "<account>" + _account + "</account>"
                                        + "<password>" + _passWord + "</password>"
                                        + "<msgid>" + _smsId + "</msgid>"
                                        + "<phones>" + _phones + "</phones>"
                                        + "<content>" + _smsContent + "</content>"
                                        + "<sign>" + _sign + "</sign>"
                                        + "<subcode>" + _subCode + "</subcode>"
                                        + "<sendtime>" + _sendTime + "</sendtime>"
                                    + "</message>";
            string recive = httpInvoke(_serverURL, _data);
            recive = ReciveTest(recive);

            #endregion
            return recive;
        }
        #region 直接发送用的方法
        /// <summary>
        /// MD5加密程序（32位小写）
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string md5(string str)
        {
            byte[] result = Encoding.Default.GetBytes(str);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] output = md5.ComputeHash(result);
            String md = BitConverter.ToString(output).Replace("-", "");
            return md.ToLower();
        }

        private string httpInvoke(String iServerURL, String iMessage)
        {
            string recive = null;
            try
            {
                CTCWebClient _webClient = new CTCWebClient();
                NameValueCollection _postValues = new NameValueCollection();
                _postValues.Add("message", iMessage);
                byte[] _responseArray = _webClient.UploadValues(iServerURL, _postValues);
                //向服务器发送POST数据
                recive = Encoding.UTF8.GetString(_responseArray);
            }
            catch (Exception e)
            {
                return recive="发送报错";
            }

            return recive;
        }

        private string ReciveTest(string recive)
        {
            if(recive.Contains("提交成功"))
            {
                return "提交成功";
            }
            return recive;
        }
        #endregion
    }
}