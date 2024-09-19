using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Entity
{
    [SugarTable("SYS_CONFIG")]
    public class SYS_Config
    {
        public int conid {  get; set; }
        public string concode {  get; set; }
        public string conname {  get; set; }
        public string conval {  get; set; }
        public string state {  get; set; }
        public int seqvalue { get; set; }
        public DateTime? createtime { get; set; }
        public string userid { get; set; }
        public string describe { get; set; }
    }
}
