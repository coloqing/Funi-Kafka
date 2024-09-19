using DataBase.Entity;
using KP.Util;
using SqlSugar;
using Util;

namespace DataBase.Entity
{
    ///<summary>
    ///设备实时数据
    ///</summary>
    [Map(typeof(TB_PARSING_DATAS))]
    [SugarTable("TB_PARSING_NEWDATAS")]
    public partial class TB_PARSING_NEWDATAS : TB_PARSING_DATAS
    {
        /// <summary>
        /// Desc:更新时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnDescription = "更新时间")]
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 设备状态 0：开机 1：关机
        /// </summary>           
        [SugarColumn(ColumnDescription = "设备状态")]
        public int State { get; set; } = 0;

        /// <summary>
        /// Desc:开关机时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnDescription = "开机时间")]
        public DateTime OnTime { get; set; } = DateTime.Now;
    }
}
