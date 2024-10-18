using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Entity
{
    /// <summary>
    /// 故障预警表
    /// </summary>
    [SugarTable("FaultOrWarn")]
    public class FaultOrWarn :BaseEntity
    {
        /// <summary>
        /// 故障预警名称
        /// </summary>
        public string? Name {  get; set; }   

        /// <summary>
        /// 线路名称
        /// </summary>
        public int? LineId { get; set; }

        /// <summary>
        /// 列车号
        /// </summary>
        public string? TrainNumber { get; set; }
        public int? TrainId { get; set; }

        /// <summary>
        /// 车型
        /// </summary>
        public string? TrainModel { get; set; }

        /// <summary>
        /// 子系统
        /// </summary>
        public string? SubSystem { get; set; }

        /// <summary>
        /// 车厢号
        /// </summary>
        public string? CarriageNumber { get; set; }
        public int? CarriageId { get; set; }

        /// <summary>
        /// 设备编号
        /// </summary>
        public string? DeviceCode { get; set; }

        /// <summary>
        /// 故障编码
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// 类型 1：故障 2：预警
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// 故障预警等级
        /// </summary>
        public int Grade { get; set; }

        /// <summary>
        /// 状态  0：已处理 1：未处理
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// 推送返回Id
        /// </summary>
        public long? UpToId { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }
    }
}
