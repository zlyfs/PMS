using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Model.Enum
{
     public enum ResultCodeEnum_SendAPI
    {
        /// <summary>
        /// 发送提交正常反馈
        /// </summary>

        success,
        /// <summary>
        ///解析异常：指令内容不正确，缺项
        ///</summary>
        analysisError=401,
        /// <summary>
        ///账号异常：用户名，密码错误
        ///</summary>
        accountError=402,
        /// <summary>
        ///任务名不存在
        /// </summary>
        missionError=403,
        /// <summary>
        ///群组名不存在
        /// </summary>
        groupError=404,
        /// <summary>
        ///部门名不存在
        /// </summary>
        departmentError=405,
        /// <summary>
        ///短信字数过多
        /// </summary>
        contentError=406,
        /// <summary>
        ///推送失败：向联通推送信息失败，详细原因从返回状态报告中查询
        /// </summary>
        sendError=407,
        /// <summary>
        /// 账号或密码缺项
        /// </summary>
        missingAccountOrPwd = 408,
        /// <summary>
        /// 任务名缺项
        /// </summary>
        missingMission = 409,
        /// <summary>
        ///存储异常：发送正常，但存储记录时发送异常
        /// </summary>
        saveError =410,
        /// <summary>
        ///未知异常:其他
        /// </summary>
        unknowError=411,

    }
}
