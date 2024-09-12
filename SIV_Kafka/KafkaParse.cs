using System;
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
using DataBase.Entity;


namespace KAFKA_PARSE
{
    public class KafkaParse
    {
        private static IMapper _mapper;
        // 使用缓存的 PropertyInfo 数组（可选，用于性能优化）  
        private static Dictionary<Type, PropertyInfo[]> cachedProperties = new Dictionary<Type, PropertyInfo[]>();

        static KafkaParse()
        {
            //var config = new MapperConfiguration(cfg =>
            //{
            //    cfg.CreateMap<TB_KAFKA_DATAS, TB_PARSING_DATAS>();
            //});

            //config.CompileMappings();

            //_mapper = config.CreateMapper();
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
            
                //项目ID
                int xlh = ByteToInt(byteArray, 2, 2);
                //列车ID
                int lchNum = ByteToInt(byteArray, 6, 2);


                // 查找帧头AA55的索引（注意，这里的索引是基于字节数组的）  
                List<int> AA55List = FindFrameHeader(byteArray, 0xAA, 0x55);
                //添加0-100字节偏移量
                List<int> indexLength = new() { 2, 1, 1, 2, 1, 1, 1, 1, 1, 1, 2, 4, 1, 1, 1, 1, 1, 1, 76 };
                //添加100-128字节偏移量
                indexLength.AddRange(getIndex(14, 2));
                //添加128-162字节偏移量
                indexLength.AddRange(getIndex(34, 1));
                //添加162-168字节偏移量
                indexLength.AddRange(new List<int> { 2, 2, 2 });
                //添加168-252字节偏移量
                indexLength.AddRange(getIndex(21, 4));
                //添加252 - 292字节偏移量
                indexLength.AddRange(new List<int> { 1, 1, 38 });
                //添加292 - 300字节偏移量
                indexLength.AddRange(getIndex(4, 2));

                foreach (var item in AA55List)
                {
                    List<int> data = new();
                    int startIndex = item;
                    foreach (var length in indexLength)
                    {
                        int byteValue = ByteToInt(byteArray, startIndex, length);
                        startIndex += length;
                        data.Add(byteValue);
                    }
                    //添加通风模式
                    data.AddRange(GetBitsFromByte(byteArray[item + 300], 8));
                    //添加控制模式和自检
                    data.AddRange(GetBitsFromByte(byteArray[item + 301], 8));
                    //添加部件状态
                    data.AddRange(GetBitsFromByte(byteArray[item + 302], 8));
                    //添加紧急通风状态
                    data.Add((byteArray[item + 303] >> 7) & 1);
                    //添加故障信息
                    data.AddRange(GetBitsFromByte(byteArray[item + 320], 8));
                    data.AddRange(GetBitsFromByte(byteArray[item + 321], 8));
                    data.AddRange(GetBitsFromByte(byteArray[item + 322], 8));
                    data.AddRange(GetBitsFromByte(byteArray[item + 323], 8));
                    data.AddRange(GetBitsFromByte(byteArray[item + 324], 8));
                    data.AddRange(GetBitsFromByte(byteArray[item + 325], 7));
                    data.AddRange(GetBitsFromByte(byteArray[item + 326], 3));
                    data.AddRange(GetBitsFromByte(byteArray[item + 327], 8));
                    data.AddRange(GetBitsFromByte(byteArray[item + 328], 5));
                    data.AddRange(GetBitsFromByte(byteArray[item + 329], 6));
                    //CRC
                    data.Add(ByteToInt(byteArray, item + 398, 2));

                    //把解析的值赋值给实体
                    var ParsingData = PopulateBFromList<KAFKA_DATA>(data);

                    //获取控制器时间
                    var rq = $"{ParsingData.Year + 2000}-{ParsingData.Month}-{ParsingData.Day} {ParsingData.Hour}:{ParsingData.Minute}:{ParsingData.Second}";
                    var rqTime = DateTime.Parse(rq);
                    //获取软件版本
                    var v = ParseVersion(getByte(byteArray, item + 102, 2));

                    //获取配置的json数据
                    var cxhKeyval = LoadJsonData("GZML7");
                    //获取11号线
                    var GZML11 = GetLch();

                    //获取车厢号别名
                    var cxh01 = cxhKeyval[ParsingData.yxtzjid.ToString()];
                    var cxhBl = cxh01.Substring(1, 1).ToString() == "1" ? cxhKeyval[lchNum.ToString()].Substring(0, 3) : cxhKeyval[lchNum.ToString()].Substring(3, 3);
                    //获取车厢号
                    var cxh = cxhKeyval["xlh"] + cxh01.Substring(0, 1) + cxhBl;
                    var lch = cxhKeyval["xlh"] + cxhKeyval[lchNum.ToString()]; //7号线
                                                                               //var lch = GZML11.Item1[lchNum.ToString()];
                                                                               //var cxh = GZML11.Item2[lch + cxh01];

                    //获取通风模式
                    var ms = GetBitsFromByte(byteArray[item + 300], 8);
                    int tfms = ms.IndexOf(1);
                    var parseData = GetParseData(ParsingData);
                    parseData.tfms = tfms;
                    parseData.rq = rqTime;
                    parseData.create_time = rqTime;
                    parseData.software_version = v;
                    parseData.lch = lch;
                    parseData.cxh = cxh;
                    parseData.cxhName = cxh01;
                    parseData.yxtzjid = ((parseData.yxtzjid & 1) == 1) ? 1 : 2;
                    parseData.device_code = cxh + "_" + parseData.yxtzjid;
                    parseData.id = SnowFlakeSingle.Instance.NextId();
                    result.Add(parseData);
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
        private static TB_PARSING_DATAS GetParseData(TB_KAFKA_DATAS kafkaData)
        {
            if (kafkaData == null)
            {
                throw new Exception("TB_KAFKA_DATAS 参数不能为空");
            }
            // 在静态构造函数中配置AutoMapper 

            var parseMapData = _mapper.Map<TB_PARSING_DATAS>(kafkaData);          
            parseMapData.jz1mbwd = Math.Round(kafkaData.jz1mbwd * 0.1, 1);
            parseMapData.jz1kswdcgq1wd = Math.Round(kafkaData.jz1kswdcgq1wd * 0.1, 1);
            parseMapData.jz1kswd = Math.Round(kafkaData.jz1kswd * 0.1, 1);
            parseMapData.jz1swwd = Math.Round(kafkaData.jz1swwd * 0.1, 1);
            parseMapData.jz1sfcgq1wd = Math.Round(kafkaData.jz1sfcgq1wd * 0.1, 1);
            parseMapData.jz1sfcgq2wd = Math.Round(kafkaData.jz1sfcgq2wd * 0.1, 1);
            parseMapData.jz1ysj1pqwd = Math.Round(kafkaData.jz1ysj1pqwd * 0.1, 1);
            parseMapData.jz1ysj2pqwd = Math.Round(kafkaData.jz1ysj2pqwd * 0.1, 1);
            parseMapData.jz1ysj1xqwd = Math.Round(kafkaData.jz1ysj1xqwd * 0.1, 1);
            parseMapData.jz1ysj2xqwd = Math.Round(kafkaData.jz1ysj2xqwd * 0.1, 1);
            parseMapData.jz1kqzljcmkwd = Math.Round(kafkaData.jz1kqzljcmkwd * 0.1, 1);

            parseMapData.jz1ysj1gyyl = (kafkaData.jz1ysj1gyyl * 20);
            parseMapData.jz1ysj1dyyl = (kafkaData.jz1ysj1dyyl * 20);
            parseMapData.jz1ysj2gyyl = (kafkaData.jz1ysj2gyyl * 20);
            parseMapData.jz1ysj2dyyl = (kafkaData.jz1ysj2dyyl * 20);
            parseMapData.jz1lwylz = (kafkaData.jz1lwylz * 2);
            parseMapData.jz1tfj1uxdlz = Math.Round(kafkaData.jz1tfj1uxdlz * 0.1, 1);
            parseMapData.jz1tfj1vxdlz = Math.Round(kafkaData.jz1tfj1vxdlz * 0.1, 1);
            parseMapData.jz1tfj1wxdlz = Math.Round(kafkaData.jz1tfj1wxdlz * 0.1, 1);
            parseMapData.jz1tfj2uxdlz = Math.Round(kafkaData.jz1tfj2uxdlz * 0.1, 1);
            parseMapData.jz1tfj2vxdlz = Math.Round(kafkaData.jz1tfj2vxdlz * 0.1, 1);
            parseMapData.jz1tfj2wxdlz = Math.Round(kafkaData.jz1tfj2wxdlz * 0.1, 1);
            parseMapData.jz1lnfj1uxdlz = Math.Round(kafkaData.jz1lnfj1uxdlz * 0.1, 1);
            parseMapData.jz1lnfj1vxdlz = Math.Round(kafkaData.jz1lnfj1vxdlz * 0.1, 1);
            parseMapData.jz1lnfj1wxdlz = Math.Round(kafkaData.jz1lnfj1wxdlz * 0.1, 1);
            parseMapData.jz1lnfj2uxdlz = Math.Round(kafkaData.jz1lnfj2uxdlz * 0.1, 1);
            parseMapData.jz1lnfj2vxdlz = Math.Round(kafkaData.jz1lnfj2vxdlz * 0.1, 1);
            parseMapData.jz1lnfj2wxdlz = Math.Round(kafkaData.jz1lnfj2wxdlz * 0.1, 1);
            parseMapData.jz1ysj1uxdlz = Math.Round(kafkaData.jz1ysj1uxdlz * 0.1, 1);
            parseMapData.jz1ysj1vxdlz = Math.Round(kafkaData.jz1ysj1vxdlz * 0.1, 1);
            parseMapData.jz1ysj1wxdlz = Math.Round(kafkaData.jz1ysj1wxdlz * 0.1, 1);
            parseMapData.jz1ysj2uxdlz = Math.Round(kafkaData.jz1ysj2uxdlz * 0.1, 1);
            parseMapData.jz1ysj2vxdlz = Math.Round(kafkaData.jz1ysj2vxdlz * 0.1, 1);
            parseMapData.jz1ysj2wxdlz = Math.Round(kafkaData.jz1ysj2wxdlz * 0.1, 1);

            parseMapData.jz1bpq1gl = Math.Round(kafkaData.jz1bpq1gl * 0.1, 1);
            parseMapData.jz1bpq2gl = Math.Round(kafkaData.jz1bpq2gl * 0.1, 1);
            parseMapData.jz1bpq1scdy = Math.Round(kafkaData.jz1bpq1scdy * 0.1, 1);
            parseMapData.jz1bpq2scdy = Math.Round(kafkaData.jz1bpq2scdy * 0.1, 1);

            parseMapData.jz1zhl1ldlz = Math.Round(kafkaData.jz1zhl1ldlz * 0.01, 2);
            parseMapData.jz1zhl2ldlz = Math.Round(kafkaData.jz1zhl2ldlz * 0.01, 2);

            parseMapData.ysjbpq1pfcwd = Math.Round(kafkaData.ysjbpq1pfcwd * 0.1, 1);
            parseMapData.ysjbpq2pfcwd = Math.Round(kafkaData.ysjbpq2pfcwd * 0.1, 1);
            parseMapData.ysjbpq1igbtwd = Math.Round(kafkaData.ysjbpq1igbtwd * 0.1, 1);
            parseMapData.ysjbpq2igbtwd = Math.Round(kafkaData.ysjbpq2igbtwd * 0.1, 1);
            parseMapData.create_time = kafkaData.rq;

            return parseMapData;
        }

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
                cachedProperties[type] = properties;
            }

            for (int i = 0;  i < values.Count && i < properties.Length; i++)
            {
                properties[i].SetValue(instance, values[i]);
            }

            return instance;
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

        //byte字节转int
        public static int ByteToInt(byte[] byteArray, int startIndex, int length, bool isBigEndian = true)
        {
            int intValue = 0;
            if (startIndex < 0 || startIndex >= byteArray.Length || length < 0 || startIndex + length > byteArray.Length)
                throw new ArgumentOutOfRangeException();

            byte[] subset = new byte[length];
            Array.Copy(byteArray, startIndex, subset, 0, length);
            for (int i = 0; i < subset.Length; i++)
            {
                if (isBigEndian)
                {
                    intValue |= subset[i] << ((subset.Length - 1 - i) * 8);
                }
                else
                {
                    intValue |= subset[i] << (i * 8);
                }
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
