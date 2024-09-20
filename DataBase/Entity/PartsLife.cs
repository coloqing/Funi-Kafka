using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Entity
{
    /// <summary>
    /// 寿命数据表
    /// </summary>
    [SugarTable("PartsLife")]
    public class PartsLife
    {

        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键ID  部件寿命表")]
        public long Id { get; set; }

        /// <summary>
        /// 部件名称
        /// </summary>
        [SugarColumn(ColumnDescription = "部件名称", Length = 255)]
        public string? Name { get; set; }

        /// <summary>
        /// 寿命部件Code
        /// </summary>
        [SugarColumn(ColumnDescription = "部件Code", Length = 255)]
        public string? Code { get;set; }

        /// <summary>
        /// 线路
        /// </summary>           
        [SugarColumn(ColumnDescription = "线路", Length = 255)]
        public string? XL { get; set; }

        /// <summary>
        /// 车号
        /// </summary>           
        [SugarColumn(ColumnDescription = "车号", Length = 255)]
        public string? CH { get; set; }

        /// <summary>
        /// 车厢
        /// </summary>           
        [SugarColumn(ColumnDescription = "车厢", Length = 255)]
        public string? CX { get; set; }

        /// <summary>
        /// 部件位置
        /// </summary>           
        [SugarColumn(ColumnDescription = "部件位置", Length = 255)]
        public int? WZ { get; set; }


        /// <summary>
        /// 类型
        /// </summary>           
        [SugarColumn(ColumnDescription = "类型", Length = 255)]
        public string? Type { get; set; }

        /// <summary>
        /// 已耗寿命/次数
        /// </summary>           
        [SugarColumn(ColumnDescription = "已耗寿命/次数")]
        public decimal? RunLife { get; set; }

        /// <summary>
        /// 额定寿命/次数
        /// </summary>           
        [SugarColumn(ColumnDescription = "额定寿命/次数")]
        public decimal? RatedLife { get; set; }

        /// <summary>
        /// 剩余寿命/次数
        /// </summary>           
        [SugarColumn(ColumnDescription = "剩余寿命/次数")]
        public decimal? SurplusLife { get; set; }

        /// <summary>
        /// 百分比
        /// </summary>           
        [SugarColumn(ColumnDescription = "百分比")]
        public decimal? Percent { get; set; }

        /// <summary>
        /// 预警编码
        /// </summary>
        public string? FaultCode { get; set; }

        /// <summary>
        /// 寿命预测编码
        /// </summary>
        public string? FarecastCode { get; set; }

        /// <summary>
        /// Desc:创建时间
        /// Default:DateTime.Now
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnDescription = "创建时间")]
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// Desc:创建人
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnDescription = "创建人", Length = 50)]
        public string? CreateUser { get; set; }

        /// <summary>
        /// Desc:修改时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnDescription = "修改时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// Desc:修改人
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnDescription = "修改人", Length = 50)]
        public string? UpdateUser { get; set; }

    }
}
