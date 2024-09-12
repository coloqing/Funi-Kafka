using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace DataBase.Entity
{
    ///<summary>
    ///原始报文
    ///</summary>
    [SplitTable(SplitType.Day)]
    [SugarTable("TB_YSBW_{year}{month}{day}")]
    public partial class TB_YSBW
    {
        public TB_YSBW()
        {
        }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsPrimaryKey = true, ColumnDescription = "")]
        public long id { get; set; }

        /// <summary>
        /// Desc:原始报文
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnDescription = "原始报文", Length = 8000)]
        public string ysbw { get; set; }

        /// <summary>
        /// Desc:创建时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnDescription = "创建时间")]
        [SplitField]
        public DateTime? create_time { get; set; }

        /// <summary>
        /// Desc:毫秒时间戳
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnDescription = "")]
        public long? timestamp { get; set; }

        /// <summary>
        /// Desc:偏移量
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnDescription = "")]
        public long? offset { get; set; }

        /// <summary>
        /// Desc:分区
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnDescription = "")]
        public int? partition { get; set; }

    }
}
