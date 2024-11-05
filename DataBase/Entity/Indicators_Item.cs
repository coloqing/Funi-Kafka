using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Entity
{
    /// <summary>
    /// 性能指标记录表
    /// </summary>
    [SugarTable("Indicators_Item")]
    public class Indicators_Item
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 性能指标id
        /// </summary>
        public int IndicatorsId { get; set; }

        /// <summary>
        /// 性能指标预警编码
        /// </summary>
        public string? IndicatorsCode { get; set; }

        /// <summary>
        /// 设备编码
        /// </summary>
        public string? DeviceCode {  get; set; }

        /// <summary>
        /// 值
        /// </summary>
        public float Value { get; set; }
   
        public DateTime CreateTime { get; set; }
        
    }
}
