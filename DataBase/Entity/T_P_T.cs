using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Entity
{
    /// <summary>
    /// 辅助变流器散热滤网堵塞查找表
    /// </summary>
    [SugarTable("T_P_T")]
    public class T_P_T
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public string? DeviceCode { get; set; }
        public int T1 {  get; set; }
        public int T2 { get; set; }

        public int W1 { get; set; }

        public int W2 { get; set; }

        public int Value {  get; set; }

        public DateTime CreateTime { get; set; }

        public bool IsDelete {  get; set; } 
    }
}
