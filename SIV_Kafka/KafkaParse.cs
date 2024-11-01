﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using System.Security.AccessControl;
using Newtonsoft.Json;
using SqlSugar.Extensions;
using SqlSugar;
using System.Runtime.CompilerServices;
using DataBase;


namespace SIV_Kafka
{
    public class KafkaParse
    {
        private static IMapper _mapper;
        // 使用缓存的 PropertyInfo 数组（可选，用于性能优化）  
        private static ConditionalWeakTable<Type, PropertyInfo[]> cachedProperties = new ConditionalWeakTable<Type, PropertyInfo[]>();

        static KafkaParse()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<KAFKA_DATA, TB_PARSING_DATAS>();
            });

            config.CompileMappings();

            _mapper = config.CreateMapper();
        }

        //获取json数据
        //private static Dictionary<string, string> cxhKeyval = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText($"{Directory.GetCurrentDirectory()}/trainNumber.json"));

        /// <summary>
        /// 获取卡夫卡解析数据
        /// </summary>
        /// <param name="kafkaString">16进制字符串</param>
        /// <returns></returns>
        public static List<KAFKA_DATA> GetKafkaData(TB_YSBW ysbw)
        { 
            try
            {
                var result = new List<KAFKA_DATA>();

                // 将16进制字符串转换为字节数组  
                byte[] byteArray = StringToByteArray(ysbw.ysbw);

                var t = DateTime.Now;
                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(184549376).DateTime;
                var t1 = DateTimeOffset.Now;
                var dateTime1 = t1.ToUnixTimeSeconds();

                //项目ID
                int projId = ByteToInt(byteArray, 2, 2);
                //列车ID
                int trainId = ByteToInt(byteArray, 6, 2);
                //WTDId
                int wtdId = ByteToInt(byteArray, 8, 2);
                //数据总长度
                int dataLength = ByteToInt(byteArray, 10, 2);
                         
                int time1 = ByteToInt(byteArray, 12, 1);
                int time2 = ByteToInt(byteArray, 13, 1);
                int time3 = ByteToInt(byteArray, 14, 1);
                int time4 = ByteToInt(byteArray, 15, 1);
                int time5 = ByteToInt(byteArray, 16, 1);
                int time6 = ByteToInt(byteArray, 17, 1);

                int test = ByteToInt(byteArray, 102, 4, false);
                int test1 = ByteToInt(byteArray, 102, 4, true);
                int test2 = ByteToInt(byteArray, 90, 4, false);
                int test3 = ByteToInt(byteArray, 90, 4, true);
                int test4 = ByteToInt(byteArray, 62, 4, false);
                int test5 = ByteToInt(byteArray, 62, 4, true);
                int test6 = ByteToInt(byteArray, 82, 4, false);
                int test7 = ByteToInt(byteArray, 82, 4, true);

                //WTD时间
                var time = time1+2000 + "-" + time2 + "-" + time3 + " " +time4+":"+time5+":"+time6;
                
                // 查找帧头AA55的索引（注意，这里的索引是基于字节数组的）  
                List<int> AA55List = FindFrameHeader(byteArray, 0xAA, 0x55);
                //添加0-100字节偏移量
                List<int> indexLength1 = new() { 2,2,2,1,1,2,2,2};
                List<int> indexLength = new();
                //添加100-128字节偏移量
                indexLength.AddRange(getIndex(115, 4));
                
                foreach (var item in AA55List)
                {
                    List<int> data = new();
                    int startIndex = item + 19;
                    int startIndex1 = item;

                    foreach (var length in indexLength1)
                    {
                        int byteValue = ByteToInt(byteArray, startIndex1, length);
                        startIndex1 += length;
                        data.Add(byteValue);
                    }

                    foreach (var length in indexLength)
                    {
                        int byteValue = ByteToInt(byteArray, startIndex1, length, false);
                        startIndex1 += length;
                        data.Add(byteValue);
                    }

                    int dk = ByteToInt(byteArray, item+8, 2, false);
                    
                    
                    //把解析的值赋值给实体
                    var ParsingData = PopulateBFromList<KAFKA_DATA>(data);

                    var svt = DateTimeOffset.FromUnixTimeSeconds(ParsingData.SV_Unixtime).DateTime;
                    ParsingData.Id = SnowFlakeSingle.Instance.NextId();
                    ParsingData.YSBWID = ysbw.id;
                    ParsingData.DK = dk;
                    ParsingData.ProjId = projId;
                    ParsingData.TrainId = trainId;
                    ParsingData.WtdId = wtdId;
                    ParsingData.DeviceCode = trainId + "_" + ParsingData.YZJID;
                    ParsingData.WTD_Time = Convert.ToDateTime(time);
                    ParsingData.SV_Time = svt;
                    result.Add(ParsingData);
                }

                return result;
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }                    
        }

        /// <summary>
        /// 数据处理
        /// </summary>
        /// <param name="kafkaData"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        //private static TB_PARSING_DATAS GetParseData(TB_KAFKA_DATAS kafkaData)
        //{
        //    if (kafkaData == null)
        //    {
        //        throw new Exception("TB_KAFKA_DATAS 参数不能为空");
        //    }
        //    // 在静态构造函数中配置AutoMapper 

        //    var parseMapData = _mapper.Map<TB_PARSING_DATAS>(kafkaData);          
        //    parseMapData.jz1mbwd = Math.Round(kafkaData.jz1mbwd * 0.1, 1);
        //    parseMapData.jz1kswdcgq1wd = Math.Round(kafkaData.jz1kswdcgq1wd * 0.1, 1);
        //    parseMapData.jz1kswd = Math.Round(kafkaData.jz1kswd * 0.1, 1);
        //    parseMapData.jz1swwd = Math.Round(kafkaData.jz1swwd * 0.1, 1);
        //    parseMapData.jz1sfcgq1wd = Math.Round(kafkaData.jz1sfcgq1wd * 0.1, 1);
        //    parseMapData.jz1sfcgq2wd = Math.Round(kafkaData.jz1sfcgq2wd * 0.1, 1);
        //    parseMapData.jz1ysj1pqwd = Math.Round(kafkaData.jz1ysj1pqwd * 0.1, 1);
        //    parseMapData.jz1ysj2pqwd = Math.Round(kafkaData.jz1ysj2pqwd * 0.1, 1);
        //    parseMapData.jz1ysj1xqwd = Math.Round(kafkaData.jz1ysj1xqwd * 0.1, 1);
        //    parseMapData.jz1ysj2xqwd = Math.Round(kafkaData.jz1ysj2xqwd * 0.1, 1);
        //    parseMapData.jz1kqzljcmkwd = Math.Round(kafkaData.jz1kqzljcmkwd * 0.1, 1);

        //    parseMapData.jz1ysj1gyyl = (kafkaData.jz1ysj1gyyl * 20);
        //    parseMapData.jz1ysj1dyyl = (kafkaData.jz1ysj1dyyl * 20);
        //    parseMapData.jz1ysj2gyyl = (kafkaData.jz1ysj2gyyl * 20);
        //    parseMapData.jz1ysj2dyyl = (kafkaData.jz1ysj2dyyl * 20);
        //    parseMapData.jz1lwylz = (kafkaData.jz1lwylz * 2);
        //    parseMapData.jz1tfj1uxdlz = Math.Round(kafkaData.jz1tfj1uxdlz * 0.1, 1);
        //    parseMapData.jz1tfj1vxdlz = Math.Round(kafkaData.jz1tfj1vxdlz * 0.1, 1);
        //    parseMapData.jz1tfj1wxdlz = Math.Round(kafkaData.jz1tfj1wxdlz * 0.1, 1);
        //    parseMapData.jz1tfj2uxdlz = Math.Round(kafkaData.jz1tfj2uxdlz * 0.1, 1);
        //    parseMapData.jz1tfj2vxdlz = Math.Round(kafkaData.jz1tfj2vxdlz * 0.1, 1);
        //    parseMapData.jz1tfj2wxdlz = Math.Round(kafkaData.jz1tfj2wxdlz * 0.1, 1);
        //    parseMapData.jz1lnfj1uxdlz = Math.Round(kafkaData.jz1lnfj1uxdlz * 0.1, 1);
        //    parseMapData.jz1lnfj1vxdlz = Math.Round(kafkaData.jz1lnfj1vxdlz * 0.1, 1);
        //    parseMapData.jz1lnfj1wxdlz = Math.Round(kafkaData.jz1lnfj1wxdlz * 0.1, 1);
        //    parseMapData.jz1lnfj2uxdlz = Math.Round(kafkaData.jz1lnfj2uxdlz * 0.1, 1);
        //    parseMapData.jz1lnfj2vxdlz = Math.Round(kafkaData.jz1lnfj2vxdlz * 0.1, 1);
        //    parseMapData.jz1lnfj2wxdlz = Math.Round(kafkaData.jz1lnfj2wxdlz * 0.1, 1);
        //    parseMapData.jz1ysj1uxdlz = Math.Round(kafkaData.jz1ysj1uxdlz * 0.1, 1);
        //    parseMapData.jz1ysj1vxdlz = Math.Round(kafkaData.jz1ysj1vxdlz * 0.1, 1);
        //    parseMapData.jz1ysj1wxdlz = Math.Round(kafkaData.jz1ysj1wxdlz * 0.1, 1);
        //    parseMapData.jz1ysj2uxdlz = Math.Round(kafkaData.jz1ysj2uxdlz * 0.1, 1);
        //    parseMapData.jz1ysj2vxdlz = Math.Round(kafkaData.jz1ysj2vxdlz * 0.1, 1);
        //    parseMapData.jz1ysj2wxdlz = Math.Round(kafkaData.jz1ysj2wxdlz * 0.1, 1);

        //    parseMapData.jz1bpq1gl = Math.Round(kafkaData.jz1bpq1gl * 0.1, 1);
        //    parseMapData.jz1bpq2gl = Math.Round(kafkaData.jz1bpq2gl * 0.1, 1);
        //    parseMapData.jz1bpq1scdy = Math.Round(kafkaData.jz1bpq1scdy * 0.1, 1);
        //    parseMapData.jz1bpq2scdy = Math.Round(kafkaData.jz1bpq2scdy * 0.1, 1);

        //    parseMapData.jz1zhl1ldlz = Math.Round(kafkaData.jz1zhl1ldlz * 0.01, 2);
        //    parseMapData.jz1zhl2ldlz = Math.Round(kafkaData.jz1zhl2ldlz * 0.01, 2);

        //    parseMapData.ysjbpq1pfcwd = Math.Round(kafkaData.ysjbpq1pfcwd * 0.1, 1);
        //    parseMapData.ysjbpq2pfcwd = Math.Round(kafkaData.ysjbpq2pfcwd * 0.1, 1);
        //    parseMapData.ysjbpq1igbtwd = Math.Round(kafkaData.ysjbpq1igbtwd * 0.1, 1);
        //    parseMapData.ysjbpq2igbtwd = Math.Round(kafkaData.ysjbpq2igbtwd * 0.1, 1);
        //    parseMapData.create_time = kafkaData.rq;

        //    return parseMapData;
        //}

        //11号线获取列车号和车厢号
        private static (Dictionary<string, string>, Dictionary<string, string>) GetLch()
        {
            var cxhKeyval1 = new Dictionary<string, string>();
            var cxhCodeVal = new Dictionary<string, string>();
            decimal x = 10999000;
            int i = 0;

            string[] clh = { "A", "B", "C", "D" };//车厢
            string[] clh2 = { "D", "C", "B", "A" };//车厢
            string lu = "11";//11号线


            int FastNum = 001;
            while (x > 0)
            {
                x += 2002;
                if (x <= 11109110)
                {
                    i++;
                    cxhKeyval1.Add(i.ToString(), x.ToString());
                    for (int l = 1; l < 3; l++)
                    {
                        if (l > 1)
                        {
                            FastNum += 1;
                        }
                        if (l == 1)
                        {
                            foreach (var cl in clh)
                            {
                                cxhCodeVal.Add(x.ToString() + cl + l, lu + cl + FastNum.ToString("000"));
                            }
                        }
                        else
                        {
                            foreach (var cl in clh2)
                            {
                                cxhCodeVal.Add(x.ToString() + cl + l, lu + cl + FastNum.ToString("000"));
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
                FastNum += 1;
            }
            return (cxhKeyval1,cxhCodeVal);
        }

        /// <summary>
        /// 获取json数据
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidOperationException"></exception>

        private static Dictionary<string, string> LoadJsonData(string xlh)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "trainNumber.json");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The file does not exist.", filePath);
            }

            string fileContent = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(fileContent))
            {
                throw new InvalidOperationException("The file is empty.");
            }

            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileContent);
                return (Dictionary<string, string>)data[xlh];
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
                // 可以选择返回null、空字典或其他默认值  
                return new Dictionary<string, string>();
            }
        }

        ///// <summary>
        ///// 导出excel数据
        ///// </summary>
        ///// <param name="dataList"></param>
        //private static void ToExcel(List<TB_PARSING_DATAS> dataList)
        //{
        //    // 定义Excel文件路径
        //    string excelFilePath = @"D:\workspace\WorkFile\解析排查车数据.xlsx";

        //    // 创建一个Workbook对象
        //    Workbook workbook = new Workbook();

        //    // 获取第一个Worksheet
        //    Worksheet worksheet = workbook.Worksheets[0];

        //    // 获取对象的属性信息
        //    PropertyInfo[] properties = typeof(TB_PARSING_DATAS).GetProperties();

        //    // 设置表头
        //    int rowIndex = 0;

        //    int colIndex = 0; // 列索引从0开始
        //    foreach (var property in properties)
        //    {
        //        worksheet.Cells[rowIndex, colIndex].Value = property.Name;
        //        colIndex++; // 移动到下一列
        //    }


        //    rowIndex++;

        //    // 填充数据
        //    foreach (var obj in dataList)
        //    {
        //        colIndex = 0;
        //        foreach (var property in properties)
        //        {
        //            worksheet.Cells[rowIndex, colIndex].Value = property.GetValue(obj);
        //            colIndex++;
        //        }
        //        rowIndex++;
        //    }

        //    // 自动调整列宽
        //    worksheet.AutoFitColumns();

        //    // 保存Excel文件
        //    workbook.Save(excelFilePath);
        //}


        /// <summary>
        /// 通过反射获取实例
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static T PopulateBFromList<T>(List<int> values) where T : class, new()
        {
            if (values == null || values.Count == 0)
                throw new ArgumentException("Values list cannot be null or empty.");
             
            T instance = new T();
            Type type = typeof(T);

            if (!cachedProperties.TryGetValue(type, out var properties))
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType == typeof(int) && p.CanWrite)
                    .ToArray();

                cachedProperties.Add(type, properties);
            }

            for (int i = 0; i < Math.Min(values.Count, properties.Length); i++)
            {
                properties[i].SetValue(instance, values[i]);
            }

            return instance;
        }


        /// <summary>
        /// CRC校验
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public ushort ComputeCRC16CCITT(byte[] bytes)
        {
            // CRC-CCITT多项式，表示为无最高位1的二进制数（即X^16的系数被省略）  
            // 对于X^16+X^12+X^5+1，省略X^16后，剩下的是1001100000010001（二进制），转换为十六进制为0x1021  
            ushort Polynomial = 0x1021;
            // 初始化CRC寄存器为0xFFFF  
            ushort crc = 0xFFFF;

            crc = 0xFFFF;
            foreach (byte b in bytes)
            {
                crc ^= (ushort)(b << 8); // 将数据字节左移8位后与CRC寄存器异或  
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc = (ushort)((crc << 1) ^ Polynomial);
                    }
                    else
                    {
                        crc <<= 1;
                    }
                    crc &= 0xFFFF; // 实际上这一步在大多数情况下是多余的，因为ushort类型已经限制了范围  
                }
            }
            return crc;
        }

        //获取字节参数
        static List<int> getIndex(int num, int value)
        {
            var data = new List<int>();
            for (int i = 0; i < num; i++)
            {
                data.Add(value);
            }
            return data;
        }

        //获取字节中每个bit值
        static List<int> GetBitsFromByte(byte byteValue, int bit)
        {
            List<int> bits = new List<int>();

            // 从最低位（右边）到最高位（左边）遍历字节  
            for (int i = 0; i < bit; i++)
            {
                // 使用位与操作和位移来获取每一位的值  
                // (byteValue >> i) 将 byteValue 向右移动 i 位  
                // & 1 检查最低位是否为 1  
                int bitValue = (byteValue >> i) & 1;

                // 将位值添加到列表中  
                bits.Add(bitValue);
            }

            // 返回包含所有位值的列表  
            return bits;
        }


        //字节转软件版本
        static string ParseVersion(byte[] versionBytes)
        {
            // 检查输入数组是否至少包含两个字节  
            if (versionBytes == null || versionBytes.Length < 2)
            {
                throw new ArgumentException("版本字节数组必须至少包含两个字节。", nameof(versionBytes));
            }

            // A 是数组的第一个字节（高字节）  
            int A = versionBytes[0];

            // B 是数组第二个字节的高4位  
            int B = (versionBytes[1] & 0xF0) >> 4;

            // C 是数组第二个字节的低4位  
            int C = versionBytes[1] & 0x0F;

            // 格式化字符串并返回  
            return $"{A}.{B}.{C}";
        }

        //
        static byte[] getByte(byte[] byteArray, int startIndex, int length)
        {
            byte[] subset = new byte[length];
            Array.Copy(byteArray, startIndex, subset, 0, length);
            return subset;
        }

        ////byte字节转int
        //public static int ByteToInt(byte[] byteArray, int startIndex, int length, bool isBigEndian = true)
        //{
        //    int intValue = 0;
        //    if (startIndex < 0 || startIndex >= byteArray.Length || length < 0 || startIndex + length > byteArray.Length)
        //        throw new ArgumentOutOfRangeException();

        //    byte[] subset = new byte[length];
        //    Array.Copy(byteArray, startIndex, subset, 0, length);
        //    for (int i = 0; i < subset.Length; i++)
        //    {
        //        if (isBigEndian)
        //        {
        //            intValue |= subset[i] << ((subset.Length - 1 - i) * 8);
        //        }
        //        else
        //        {
        //            intValue |= subset[i] << (i * 8);
        //        }
        //    }
        //    return intValue;
        //}

        /// <summary>  
        /// 将字节数组转换为int，支持大端和小端模式  
        /// </summary>  
        /// <param name="byteArray">包含字节的数组</param>  
        /// <param name="startIndex">起始索引</param>  
        /// <param name="length">要转换的字节长度（必须是1, 2, 3, 或 4）</param>  
        /// <param name="isBigEndian">是否为大端模式</param>  
        /// <returns>转换后的int值</returns>  
        /// <exception cref="ArgumentOutOfRangeException">如果索引无效或长度不正确</exception>  
        public static int ByteToInt(byte[] byteArray, int startIndex, int length, bool isBigEndian = true)
        {
            if (startIndex < 0 || startIndex >= byteArray.Length || length < 0 || startIndex + length > byteArray.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex) + " 或 " + nameof(length) + " 无效");

            int intValue = 0;
            for (int i = 0; i < length; i++)
            {
                int shift = (isBigEndian ? length - 1 - i : i) * 8;
                intValue |= (byteArray[startIndex + i] & 0xFF) << shift;
            }

            return intValue;
        }

        //将16进制字符串转换为字节数组  
        public static byte[] StringToByteArray(string hex)
        {

            hex = hex.Trim().Replace(" ", "");
            if (hex == null)
            {
                throw new ArgumentNullException(nameof(hex));
            }

            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("十六进制字符串必须包含偶数个字符。");
            }

            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        static byte[] HexStringToByteArray(string hex)
        {
            hex = hex.Trim().Replace(" ", "");
            if (hex == null)
            {
                throw new ArgumentNullException(nameof(hex));
            }

            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("十六进制字符串必须包含偶数个字符。");
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return bytes;
        }

        /// <summary>
        /// 查找帧头索引
        /// </summary>
        /// <param name="array"></param>
        /// <param name="firstByte"></param>
        /// <param name="secondByte"></param>
        /// <returns></returns>
        static List<int> FindFrameHeader(byte[] array, byte firstByte, byte secondByte)
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < array.Length - 1; i++)
            {
                if (array[i] == firstByte && array[i + 1] == secondByte)
                {
                    indices.Add(i); // 添加第一个字节的索引  
                }
            }
            return indices;
        }
    }
}
