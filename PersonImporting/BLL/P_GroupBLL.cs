using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.Model;
using PMS.Model.Enum;

namespace PersonImporting.BLL
{
   public class P_GroupBLL:DBBaseBLL
    {
        protected PMS.IBLL.IP_GroupBLL groupBLL;

        public P_GroupBLL()
        {
            groupBLL = new PMS.BLL.P_GroupBLL();
        }
        /// <summary>
        /// 检查是否存在群组，并创建--------导入通讯录时使用
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public ExistEnum CheckGroupExist(string groupName,int sort)
        {
            //根据群组名称获取群组集合
            var groupList= groupBLL.GetListBy(g => g.GroupName == groupName).ToList();
            ExistEnum enum_exist=ExistEnum.error;
            //判断集合是否为空
            if (groupList.Count() == 0)
            {
                //需要创建
                enum_exist = groupBLL.Create(new PMS.Model.P_Group()
                {
                    GroupName = groupName,
                    Sort = sort,
                    SubTime = DateTime.Now,
                    ModifiedOnTime = DateTime.Now,
                    isDel = false,
                    forbidDel = false
                }) == true ? ExistEnum.ok : ExistEnum.isExist;
            }
            return enum_exist;
        }

        public ExistEnum CheckGroupExist(string groupName)
        {
            //根据群组名称获取群组集合
            var groupList = groupBLL.GetListBy(g => g.GroupName == groupName).ToList();
            ExistEnum enum_exist = ExistEnum.isExist;
            //判断集合是否为空
            if (groupList.Count() == 0)
            {
                //不需要创建
                enum_exist = ExistEnum.isNotExist;
            }
            return enum_exist;
        }


        /// <summary>
        /// 通过名称得到群组对象
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public P_Group getGroupByName(string groupName)
        {
            //根据群组名称获取群组集合
          return groupBLL.GetListBy(g => g.GroupName.Equals(groupName)).FirstOrDefault();
        }

        /// <summary>
        /// 根据群组名称得到Id
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetGroupId(string name)
        {
            return groupBLL.GetListBy(p => p.GroupName.Equals(name)).FirstOrDefault().GID;
        }

        public List<P_Group> getGroupList()
        {
            //获取群组集合

            return groupBLL.GetListBy(g => g.isDel == false).ToList();
        }

        /// <summary>
        /// 查找所属联系人
        /// </summary>
        /// <param name="GID"></param>
        /// <returns></returns>
        public List<PMS.Model.P_PersonInfo> GetBelongingPerson(int GID)
        {
            var groupList = groupBLL.GetListBy(d => d.GID == GID).ToList();
            var belongingPersonList = groupList[0].P_PersonInfo.ToList();
            return belongingPersonList;
        }

        /// <summary>
        /// 获取所属的任务
        /// </summary>
        /// <param name="GID"></param>
        /// <returns></returns>
        public List<PMS.Model.S_SMSMission> GetBelongMission(int GID)
        {
            var departmentList = groupBLL.GetListBy(d => d.GID == GID).ToList();
            var RGMList = departmentList[0].R_Group_Mission.ToList();
            List<PMS.Model.S_SMSMission> belongingMissionList = new List<PMS.Model.S_SMSMission>();
            foreach (var temp in RGMList)
            {
                if (!temp.S_SMSMission.isDel) belongingMissionList.Add(temp.S_SMSMission);
            }
            return belongingMissionList;
        }
    }
}
