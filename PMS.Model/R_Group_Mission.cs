//------------------------------------------------------------------------------
// <auto-generated>
//    此代码是根据模板生成的。
//
//    手动更改此文件可能会导致应用程序中发生异常行为。
//    如果重新生成代码，则将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace PMS.Model
{
    using System;
    using System.Collections.Generic;
    
    public partial class R_Group_Mission
    {
        public int ID { get; set; }
        public int MissionID { get; set; }
        public int GroupID { get; set; }
        public bool isPass { get; set; }
    
        public virtual P_Group P_Group { get; set; }
        public virtual S_SMSMission S_SMSMission { get; set; }
    }
}
