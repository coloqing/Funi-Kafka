using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Entity
{
    [SugarTable("TB_PARSING_DATAS")]
    public class TB_PARSING_DATAS
    {
        /// <summary>
        /// id
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, ColumnDescription = "主键ID")]
        public long Id {  get; set; }

        /// <summary>
        /// 项目id
        /// </summary>
        public int ProjId {  get; set; }

        /// <summary>
        /// 列车id
        /// </summary>
        public int LCId {  get; set; }

        /// <summary>
        /// 源主机ID
        /// </summary>
        public int YZJID { get; set; }

        /// <summary>
        /// 输出总功率
        /// </summary>
        public int InConv_Output_Energy { get; set; }

        /// <summary>
        /// 输入总功率
        /// </summary>
        public int InConv_Input_Energy { get; set; }

        /// <summary>
        /// 输入电流1
        /// </summary>
        public int I_DC_In_1 { get; set; }

        /// <summary>
        /// 输入变换器2下母线电压
        /// </summary>
        public int U_DC_Link_InConv_2_LO { get; set; }

        /// <summary>
        /// 输入变换器2上母线电压
        /// </summary>
        public int U_DC_Link_InConv_2_UP { get; set; }

        /// <summary>
        /// 输入变换器1下母线电压
        /// </summary>
        public int U_DC_Link_InConv_1_LO { get; set; }

        /// <summary>
        /// 数字输出信号
        /// </summary>
        public int InConv_DIGOUT { get; set; }

        /// <summary>
        /// 数字输入信号
        /// </summary>
        public int InConv_DIGIN { get; set; }

        /// <summary>
        /// 输入变换器温度2
        /// </summary>
        public int T_HS_InConv_2 { get; set; }

        /// <summary>
        /// 输入变换器温度1
        /// </summary>
        public int T_HS_InConv_1 { get; set; }

        /// <summary>
        /// 控制板温度
        /// </summary>
        public int T_Container { get; set; }

        /// <summary>
        /// 输入变换器1上母线电压
        /// </summary>
        public int U_DC_Link_InConv_1_UP { get; set; }

        /// <summary>
        /// 逆变母线电压
        /// </summary>
        public int U_DC_Link_Inv { get; set; }

        /// <summary>
        /// 输入变换器2直流母线电压
        /// </summary>
        public int U_DC_Link_InConv_2 { get; set; }

        /// <summary>
        /// 直流输出电流
        /// </summary>
        public int I_DC_Out { get; set; }

        /// <summary>
        /// 直流输出电压
        /// </summary>
        public int U_DC_Out { get; set; }

        /// <summary>
        /// 输入变换器1直流母线电压
        /// </summary>
        public int U_DC_Link_InConv_1 { get; set; }

        /// <summary>
        /// 输入功率
        /// </summary>
        public int P_DC_In { get; set; }

        /// <summary>
        /// 输入电流
        /// </summary>
        public int I_DC_In { get; set; }

        /// <summary>
        /// 输入电压
        /// </summary>
        public int U_DC_In { get; set; }

        /// <summary>
        /// 逆变器输出总功率
        /// </summary>
        public int Inv_Output_Energy { get; set; }

        /// <summary>
        /// 接地电压
        /// </summary>
        public int U_Iso { get; set; }

        /// <summary>
        /// 逆变器数字输出信号
        /// </summary>
        public int Inv_DIGOUT { get; set; }

        /// <summary>
        /// 逆变器数字输入信号
        /// </summary>
        public int Inv_DIGIN { get; set; }

        /// <summary>
        /// 三相频率
        /// </summary>
        public int Frq { get; set; }

        /// <summary>
        /// 逆变器温度2
        /// </summary>
        public int T_HS_Inv_2 { get; set; }

        /// <summary>
        /// 逆变器温度1
        /// </summary>
        public int T_HS_Inv_1 { get; set; }

        /// <summary>
        /// 三相输出无功功率
        /// </summary>
        public int ReactPwr { get; set; }

        /// <summary>
        /// 三相输出有功功率
        /// </summary>
        public int ActPower { get; set; }

        /// <summary>
        /// L3相电流
        /// </summary>
        public int I_L3 { get; set; }

        /// <summary>
        /// L2相电流
        /// </summary>
        public int I_L2 { get; set; }

        /// <summary>
        /// L1相电流
        /// </summary>
        public int I_L1 { get; set; }

        /// <summary>
        /// L3相电压
        /// </summary>
        public int U_L3 { get; set; }

        /// <summary>
        /// L2相电压
        /// </summary>
        public int U_L2 { get; set; }

        /// <summary>
        /// L1相电压
        /// </summary>
        public int U_L1 { get; set; }

        /// <summary>
        /// 逆变器下母线电压
        /// </summary>
        public int U_DC_Link_Inv_Lo { get; set; }

        /// <summary>
        /// 逆变上母线电压
        /// </summary>
        public int U_DC_Link_Inv_Up { get; set; }

        /// <summary>
        /// 蓄电池温度
        /// </summary>
        public int T_Battery { get; set; }

        /// <summary>
        /// 蓄电池电流
        /// </summary>
        public int I_Battery { get; set; }

        /// <summary>
        /// 蓄电池电压
        /// </summary>
        public int U_Battery { get; set; }

        /// <summary>
        /// 交流输出接触器开关次数
        /// </summary>
        public int CNT_DI_AC_CONT { get; set; }

        /// <summary>
        /// 输入接触器1开关次数
        /// </summary>
        public int CNT_DI_INCONT1 { get; set; }

        /// <summary>
        /// 充电机故障状态总时间
        /// </summary>
        public int CNT_BC_MALFUNCTIONTIME { get; set; }

        /// <summary>
        /// 逆变器正常运行状态总时间
        /// </summary>
        public int CNT_INV_RUNNINGTIME { get; set; }

        /// <summary>
        /// 输入变换器正常运行总时间
        /// </summary>
        public int CNT_INCONV_RUNNINGTIME { get; set; }

        /// <summary>
        /// 通讯盒当前时间戳
        /// </summary>
        public int SV_Unixtime { get; set; }

        public DateTime SV_Time { get; set; }

        public DateTime CreateTime { get;}

    }
}
