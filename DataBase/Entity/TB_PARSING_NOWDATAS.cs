using DataBase.Entity;
using KP.Util;
using SqlSugar;
using Util;

namespace DataBase.Entity
{
    ///<summary>
    ///设备实时数据
    ///</summary>
    [Map(typeof(KAFKA_DATA))]
    [SugarTable("TB_PARSING_NOWDATAS")]
    public partial class TB_PARSING_NOWDATAS
    {

        /// <summary>
        /// 特征码AA55（43605）
        /// </summary>
        public int TZM { get; set; }

        /// <summary>
        /// 协议版本
        /// </summary>
        public int XY { get; set; }

        /// <summary>
        /// 帧号
        /// </summary>
        public int ZH { get; set; }

        /// <summary>
        /// 源主机ID
        /// </summary>
        public int YZJID { get; set; }

        /// <summary>
        /// 目标主机ID
        /// </summary>
        public int MBZJID { get; set; }

        /// <summary>
        /// 端口（5548）
        /// </summary>
        public int DK { get; set; }

        /// <summary>
        /// 系统代号
        /// </summary>
        public int XTDH { get; set; }

        /// <summary>
        /// 数据长度N字节
        /// </summary>
        public int DLN { get; set; }

        /// <summary>
        /// 预留
        /// </summary>
        public int YL { get; set; }

        /// <summary>
        /// 软件包版本
        /// </summary>
        public int SW_VER_DSP_STD { get; set; }

        /// <summary>
        /// 控制板FPGA软件版本
        /// </summary>
        public int SW_VER_FPGA { get; set; }

        /// <summary>
        /// 控制板CPU软件版本
        /// </summary>
        public int SW_VER_DSP_APP { get; set; }

        /// <summary>
        /// 输出总功率
        /// </summary>
        public int InConv_Output_Energy { get; set; }

        /// <summary>
        /// 输入总功率
        /// </summary>
        public int InConv_Input_Energy { get; set; }

        /// <summary>
        /// 输入变换器正常运行时间
        /// </summary>
        public int InConv_Operation_Time { get; set; }

        /// <summary>
        /// 输入变换器在线时间
        /// </summary>
        public int InConv_On_Time { get; set; }

        /// <summary>
        /// 输入电流3
        /// </summary>
        public int I_DC_In_3 { get; set; }

        /// <summary>
        /// 输入电流2
        /// </summary>
        public int I_DC_In_2 { get; set; }

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
        /// 输入变换器外部使用状态
        /// </summary>
        public int InConv_OutStatus { get; set; }

        /// <summary>
        /// 输入变换器内部使用状态
        /// </summary>
        public int InConv_InStatus { get; set; }

        /// <summary>
        /// 输入变换器操作模式
        /// </summary>
        public int Inconv_OpMode { get; set; }

        /// <summary>
        /// 输入变换器轻微故障
        /// </summary>
        public int Inconv_Minor { get; set; }

        /// <summary>
        /// 输入变换器中等故障
        /// </summary>
        public int Inconv_Maintenance { get; set; }

        /// <summary>
        /// 输入变换器严重故障
        /// </summary>
        public int Inconv_Major { get; set; }

        /// <summary>
        /// 输入变换器三次锁死事件编码
        /// </summary>
        public int InConv_Error { get; set; }

        /// <summary>
        /// 输入变换器事件编码
        /// </summary>
        public int InConv_Event { get; set; }

        /// <summary>
        /// 逆变器输出总功率
        /// </summary>
        public int Inv_Output_Energy { get; set; }

        /// <summary>
        /// 逆变器输入总功率
        /// </summary>
        public int Inv_Input_Energy { get; set; }

        /// <summary>
        /// 逆变器操作时间
        /// </summary>
        public int Inv_Operation_Time { get; set; }

        /// <summary>
        /// 逆变器在线时间
        /// </summary>
        public int Inv_On_Time { get; set; }

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
        /// LN相电流（RMS）
        /// </summary>
        public int IrmsLN { get; set; }

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
        /// 逆变母线电压（重复）
        /// </summary>
        public int Inv_U_DC_Link_Inv { get; set; }

        /// <summary>
        /// 逆变器下母线电压
        /// </summary>
        public int U_DC_Link_Inv_Lo { get; set; }

        /// <summary>
        /// 逆变上母线电压
        /// </summary>
        public int U_DC_Link_Inv_Up { get; set; }

        /// <summary>
        /// 逆变器外部使用状态
        /// </summary>
        public int Inv_OutStatus { get; set; }

        /// <summary>
        /// 逆变器内部使用状态
        /// </summary>
        public int Inv_InStatus { get; set; }

        /// <summary>
        /// 逆变器操作模式
        /// </summary>
        public int Inv_OpMode { get; set; }

        /// <summary>
        /// 逆变器轻微故障
        /// </summary>
        public int Inv_Minor { get; set; }

        /// <summary>
        /// 逆变器中等故障
        /// </summary>
        public int Inv_Maintenance { get; set; }

        /// <summary>
        /// 逆变器严重故障
        /// </summary>
        public int Inv_Major { get; set; }

        /// <summary>
        /// 逆变器三次锁死事件编码
        /// </summary>
        public int Inv_Error { get; set; }

        /// <summary>
        /// 逆变器事件编码
        /// </summary>
        public int Inv_Event { get; set; }

        /// <summary>
        /// 后备模式温度
        /// </summary>
        public int T_HS_BU { get; set; }

        /// <summary>
        /// 蓄电池容量
        /// </summary>
        public int Battery_Ah { get; set; }

        /// <summary>
        /// 蓄电池SOH
        /// </summary>
        public int Battery_SOH { get; set; }

        /// <summary>
        /// 蓄电池SOC百分比
        /// </summary>
        public int Battery_SOC_Per { get; set; }

        /// <summary>
        /// SOH状态
        /// </summary>
        public int SOH_State { get; set; }

        /// <summary>
        /// 停止SOH
        /// </summary>
        public int SOH_Stop { get; set; }

        /// <summary>
        /// 开启SOH
        /// </summary>
        public int SOH_Start { get; set; }

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
        /// 蓄电池OSC
        /// </summary>
        public int Battery_SOC { get; set; }

        /// <summary>
        /// 直流输出电压（重复）
        /// </summary>
        public int U_DC_Out_BC { get; set; }

        /// <summary>
        /// 充电机外部状态
        /// </summary>
        public int BC_OutStatus { get; set; }

        /// <summary>
        /// 充电机内部状态
        /// </summary>
        public int BC_InStatus { get; set; }

        /// <summary>
        /// 充电机操作模式
        /// </summary>
        public int BC_OpMode { get; set; }

        /// <summary>
        /// 充电机轻微故障
        /// </summary>
        public int BC_Minor { get; set; }

        /// <summary>
        /// 充电机中等故障
        /// </summary>
        public int BC_Maintenance { get; set; }

        /// <summary>
        /// 充电机严重故障
        /// </summary>
        public int BC_Major { get; set; }

        /// <summary>
        /// 充电机三次锁死事件编码
        /// </summary>
        public int BC_Error { get; set; }

        /// <summary>
        /// 充电机事件编码
        /// </summary>
        public int BC_Event { get; set; }

        /// <summary>
        /// 内部风扇运行总时间
        /// </summary>
        public int CNT_INT_FAN_RUNTIME { get; set; }

        /// <summary>
        /// 主风机运行总时间
        /// </summary>
        public int CNT_MAIN_FAN_RUNTIME { get; set; }

        /// <summary>
        /// 交流输出接触器开关次数
        /// </summary>
        public int CNT_DI_AC_CONT { get; set; }

        /// <summary>
        /// 直流输出接触器开关次数
        /// </summary>
        public int CNT_DI_DC_CONT { get; set; }

        /// <summary>
        /// 车间供电接触器开关次数
        /// </summary>
        public int CNT_DI_WSS_CONT { get; set; }

        /// <summary>
        /// 输入接触器2开关次数
        /// </summary>
        public int CNT_DI_INCONT2 { get; set; }

        /// <summary>
        /// 输入接触器1开关次数
        /// </summary>
        public int CNT_DI_INCONT1 { get; set; }

        /// <summary>
        /// 充电机锁死状态总时间
        /// </summary>
        public int CNT_BC_ERRORTIME { get; set; }

        /// <summary>
        /// 充电机故障状态总时间
        /// </summary>
        public int CNT_BC_MALFUNCTIONTIME { get; set; }

        /// <summary>
        /// 充电机正常运行状态总时间
        /// </summary>
        public int CNT_BC_RUNNINGTIME { get; set; }

        /// <summary>
        /// 充电机待机状态总时间
        /// </summary>
        public int CNT_BC_STANDBYTIME { get; set; }

        /// <summary>
        /// 充电机初始化状态总时间
        /// </summary>
        public int CNT_BC_INITTIME { get; set; }

        /// <summary>
        /// 充电机车间供电模式运行总时间
        /// </summary>
        public int CNT_BC_WSRUNTIME { get; set; }

        /// <summary>
        /// 逆变器总输出功率
        /// </summary>
        public int CNT_INV_OUTENERGY { get; set; }

        /// <summary>
        /// 逆变器锁死状态总时间
        /// </summary>
        public int CNT_INV_ERRORTIME { get; set; }

        /// <summary>
        /// 逆变器故障状态总时间
        /// </summary>
        public int CNT_INV_MALFUNCTIONTIME { get; set; }

        /// <summary>
        /// 逆变器正常运行状态总时间
        /// </summary>
        public int CNT_INV_RUNNINGTIME { get; set; }

        /// <summary>
        /// 逆变器待机状态总时间
        /// </summary>
        public int CNT_INV_STANDBYTIME { get; set; }

        /// <summary>
        /// 逆变器初始化状态总时间
        /// </summary>
        public int CNT_INV_INITTIME { get; set; }

        /// <summary>
        /// 逆变器车间供电模式运行总时间
        /// </summary>
        public int CNT_INV_WSRUNTIME { get; set; }

        /// <summary>
        /// 输入变换器输出总功率
        /// </summary>
        public int CNT_INCONV_OUTENERGY { get; set; }

        /// <summary>
        /// 输入变换器总输入功率
        /// </summary>
        public int CNT_INCONV_INENERGY { get; set; }

        /// <summary>
        /// 输入变换器锁死状态总时间
        /// </summary>
        public int CNT_INCONV_ERRORTIME { get; set; }

        /// <summary>
        /// 输入变换器故障状态总时间
        /// </summary>
        public int CNT_INCONV_MALFUNCTIONTIME { get; set; }

        /// <summary>
        /// 输入变换器正常运行总时间
        /// </summary>
        public int CNT_INCONV_RUNNINGTIME { get; set; }

        /// <summary>
        /// 输入变换器待机状态总时间
        /// </summary>
        public int CNT_INCONV_STANDBYTIME { get; set; }

        /// <summary>
        /// 输入变换器初始化状态总时间
        /// </summary>
        public int CNT_INCONV_INITTIME { get; set; }

        /// <summary>
        /// 输入变换器车间供电模式运行总时间
        /// </summary>
        public int CNT_INCONV_WSRUNTIME { get; set; }

        /// <summary>
        /// 通讯盒当前时间戳
        /// </summary>
        public int SV_Unixtime { get; set; }

        public long Id { get; set; }

        /// <summary>
        /// 项目ID
        /// </summary>
        public int ProjId { get; set; }

        /// <summary>
        /// 列车ID
        /// </summary>
        public int TrainId { get; set; }

        /// <summary>
        /// WTDID
        /// </summary>
        public int WtdId { get; set; }

        public long YSBWID { get; set; }

        [SplitField]
        public DateTime SV_Time { get; set; } = DateTime.Now;
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public DateTime WTD_Time { get; set; }

        /// <summary>
        /// Desc:更新时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnDescription = "更新时间")]
        public DateTime UpdateTime { get; set; } = DateTime.Now;

    }
}
