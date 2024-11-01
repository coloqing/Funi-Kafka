using AutoMapper;
using Confluent.Kafka;
using DataBase;
using DataBase.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIV_Kafka
{
    public class KafkaService
    {
        private static Timer _timer;
        private readonly IMapper _mapper;
        private readonly ILogger<KafkaParse> _Kfklogger;
        private readonly ILogger<KafkaService> _logger;
        private readonly IConfiguration _Config;

        //private readonly SqlSugarClient _dbContext;
        private readonly MyDbContext _dbContext;
        private readonly AppSettings _appSettings;
        private KafkaConsumerHelper<string, string> _ConsumerHelper;
        private static Dictionary<string, string> cxhKeyval = new Dictionary<string, string>();
        private static DateTime chuanbiaoRQ = DateTime.MinValue; 

        public KafkaService(IMapper mapper,ILogger<KafkaService> logger, MyDbContext dbContext, IOptions<AppSettings> appSettings, IConfiguration Config)
        {
            _mapper = mapper;
            _logger = logger;
            _dbContext = dbContext;
            _appSettings = appSettings.Value;
            _Config = Config; 
            string bootstrapServers = _appSettings.KafkaConfig.bootstrapServers;
            string groupId = _appSettings.KafkaConfig.groupId;
            string username = _appSettings.KafkaConfig.username;
            string password = _appSettings.KafkaConfig.password;
            string topic = _appSettings.KafkaConfig.topic;

            if (string.IsNullOrWhiteSpace(username))
                _ConsumerHelper = new KafkaConsumerHelper<string, string>(bootstrapServers, groupId, topic);
            else
                _ConsumerHelper = new KafkaConsumerHelper<string, string>(bootstrapServers, groupId, username, password, topic);
            //InitTables();
        }

        /// <summary>
        /// 解析程序开始入口
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            //InitTables();
            await GetTestData();
            
            //await _ConsumerHelper.StartConsumingWithMultipleThreadsAsyncV1(consumeCallback, 8);

            _logger.LogInformation("kafkaStartConsuming...");
   
        }

        public async Task TestTime()
        {
            
                // 创建一个Stopwatch实例  
                while (true)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew(); // 开始计时  

                    try
                    {
                        await GetTestData(); // 调用你的方法  
                    }
                    catch (Exception ex)
                    {
                        // 处理可能发生的异常  
                        Console.WriteLine($"Error in GetTestData: {ex.Message}");
                    }

                    stopwatch.Stop(); // 停止计时  

                    // 输出执行时间  
                    Console.WriteLine($"Execution time of GetTestData: {stopwatch.ElapsedMilliseconds} ms");


                    await Task.Delay(10);
                } 
      
        }
        
        /// <summary>
        /// 测试数据
        /// </summary>
        /// <returns></returns>
        public async Task GetTestData()
        {
            int num = 0;

            string folderPath = "D:/workspace/鼎汉车辆智慧空调系统/广州地铁7号线/kafka数据/kafka导出";
            string[] files = Directory.GetFiles(folderPath, "*.bin", SearchOption.AllDirectories);
            // 或者  
            Stopwatch stopwatch = new();
            try
            {
                InitTables();
                foreach (string file in files)
                {
                    // 读取文件内容  
                    byte[] fileBytes = File.ReadAllBytes(file);

                    string fileContent = Encoding.UTF8.GetString(fileBytes);

                    stopwatch.Restart();

                    fileContent = "55FF138B0100004B000204021809131116096E0000000000AA550004CE7D0206AC156EFDE00100000000405100001450000015050000C0030000FB0300001100000011000000000000000000000022000000250200002602000026020000C10400000187260030000000320000003A00000026020000BC0200004C040000230000007C0000004D04000020020000220000003E0600005D000000040000001000000000000000000000000000000000000000000000007503000000000000110000001100000002000000C104000001872600F30100002700000032000000030000009C000000D90100004B0000004B0000004B000000DC000000DB000000DB000000BC02000063010000590100002F000000110000001000000000000000000000000000000000000000000000000000000000000000000000006400000001000000000000000000000021000000FFFFFFFF76000000640000007C000000110000000100000010000000000000000000000000000000000000000000000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000001450000000000000000000000000000000800000008000000080000015050000BF5DEC66000000000000000000000000000000000000000000000000C7C8AA550004CB7D0506AC156EFDE00100000000405100001450000015050000DA0300000C040000110000001100000000000000000000001F000000260200002602000025020000C10400000107270032000000340000003B00000026020000BF0200004D0400002A0000007C0000004B040000E50100001F000000190600005D000000040000001000000000000000000000000000000000000000000000008203000000000000110000001100000003000000C104000001072700F30100002A0000003900000001000000A10000009C010000420000004300000043000000DB000000DC000000DC000000BF02000065010000590100002F000000110000001000000000000000000000000000000000000000000000000000000000000000000000006400000001000000000000000000000020000000FFFFFFFF76000000640000007C000000110000000100000010000000000000000000000000000000000000000000000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000001450000000000000000000000000000000800000008000000080000015050000C15DEC6600000000000000000000000000000000000000000000000086890CF0";
                    var fileContent1 = "55FF138B01000051000204021809131116096E0000000000AA55000450540206AC156EFDE00100000000405100001450000015050000F4030000230400000B0000000C0000000000000000000000480000002C0200002A0200002A020000C10400000187260035000000370000003A0000002A020000C002000056040000310000007C000000550400007D040000480000002F0600005D00000004000000100000000000000000000000000000000000000000000000AB030000000000000B0000000C00000002000000C104000001872600F40100002C0000003F00000003000000860000000C0400009E0000009E0000009D000000DB000000DC000000DC000000C0020000650100005A0100002F000000110000001000000000000000000000000000000000000000000000000000000000000000000000006400000001000000000000000000000021000000FFFFFFFF76000000640000007C000000110000000100000010000000000000000000000000000000000000000000000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000001450000000000000000000000000000000800000008000000080000015050000BF5DEC66000000000000000000000000000000000000000000000000A58AAA55000452540506AC156EFDE001000000004051000014500000150500001D040000540400000B0000000B0000000000000000000000490000002B020000280200002C020000C10400000107270037000000380000003B00000029020000BE020000530400002B0000007C0000005504000085040000490000002E0600005D00000004000000100000000000000000000000000000000000000000000000DE030000000000000B0000000B00000002000000C104000001072700F3010000290000003E000000020000008400000016040000A00000009F0000009F000000DB000000DC000000DC000000BE020000640100005A0100002F000000110000001000000000000000000000000000000000000000000000000000000000000000000000006400000001000000000000000000000021000000FFFFFFFF76000000640000007C000000110000000100000010000000000000000000000000000000000000000000000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000000080000000800000008000001450000000000000000000000000000000800000008000000080000015050000C45DEC66000000000000000000000000000000000000000000000000610F7EDE";

                    // 这里是你要每秒执行的方法  
                    var ysbwModel = new TB_YSBW()
                    {
                        id = SnowFlakeSingle.Instance.NextId(),
                        ysbw = fileContent1
                    };
                    var data = KafkaParse.GetKafkaData(ysbwModel);
                    if (data.Count>0)
                    {
                        //添加寿命数据
                        //await AddOrUpdateSMData(data);

                        //var lch = data.FirstOrDefault()?.LCId;

                        DataCacheService.AddKAFKA_DATA(data);
                        //var count = await _dbContext.Insertable(data).SplitTable().ExecuteCommandAsync();
                        await UpdateDataNow(data);
                        DataCacheService.AddTB_YSBW(ysbwModel);
                    }
                    await Task.Delay(200);
                    stopwatch.Stop();
                    Console.WriteLine( $"总耗时 == {stopwatch.ElapsedMilliseconds}");
                }
            }
            catch (Exception ex)
            {

                throw new Exception(ex.ToString());
            }         
        }

        /// <summary>
        /// 处理从 Kafka 接收到的消息
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> consumeCallback(ConsumeResult<string, string> result)
        {
            try
            {
                // 从 Kafka 接收到的消息
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                var offset = result.Offset;
                var partition = result.Partition.Value;
                await Console.Out.WriteLineAsync($"分区:{partition},偏移量{offset}");

                var ysbwModel = new TB_YSBW
                {
                    ysbw = result.Message.Value,
                    create_time = DateTime.Now,
                    partition = partition,
                    offset = offset,
                    timestamp = result.Message.Timestamp.UnixTimestampMs,
                    id = SnowFlakeSingle.Instance.NextId()
                };

                // 处理数据
                var data = KafkaParse.GetKafkaData(ysbwModel);

                //添加寿命数据
                //await AddOrUpdateSMData(data);

                //DataCacheService.AddTB_PARSING_DATAS(data);

                //var lch = data.FirstOrDefault()?.LCId;

                await UpdateDataNow(data);

                DataCacheService.AddTB_YSBW(ysbwModel);
               
                TimeSpan elapsedTime = stopwatch.Elapsed;
                await Console.Out.WriteLineAsync("程序运行时间（毫秒）: " + elapsedTime.TotalMilliseconds);
                await Console.Out.WriteLineAsync($"--------------------------------------");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 创建新表加日期
        /// </summary>
        public void InitTables()
        {
            try
            {
                _logger.LogInformation("开始更新表结构...");

                // 加载程序集并获取相关类型  
                var assembly = Assembly.LoadFrom("DataBase.dll").GetTypes().Where(x => x.Namespace == "DataBase.Entity").ToList();

                foreach (Type item in assembly)
                {
                    // 检查表是否存在  
                    if (_dbContext.DbMaintenance.IsAnyTable(item.Name))
                    {
                        // 获取差异并处理  
                        var diffString = _dbContext.CodeFirst.GetDifferenceTables(item).ToDiffList();
                        ProcessTableDifferences(item, diffString);
                    }
                    else
                    {
                        _dbContext.CodeFirst.InitTables(item); // 假设存在这样的方法  
                        _logger.LogInformation($"表{item.Name}不存在，已创建。");
                    }               
                }

                _logger.LogInformation("表结构更新完成");
            }
            catch (Exception ex)
            {
                _logger.LogError("表结构更新失败: " + ex.ToString());
            }
        }

        private void ProcessTableDifferences(Type item, IEnumerable<TableDifferenceInfo> diffString)
        {
            foreach (var info in diffString)
            {
                if (info.AddColums.Count > 0)
                {
                    try
                    {
                        _dbContext.CodeFirst.InitTables(item);
                        _logger.LogDebug($"表{item.Name}新增字段：{string.Join(",", info.AddColums)}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"更新表{item.Name}时出错：{ex.ToString()}");
                    }
                }
                // 可以添加其他差异处理逻辑，如删除列、修改列等  
            }
        }

        /// <summary>
        /// 添加或更新寿命数据
        /// </summary>
        /// <param name="dataList"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task AddOrUpdateSMData(List<TB_PARSING_DATAS> dataList)
        {
            var result = new List<PartsLife>();
            if (dataList.Count>0)
            {
                try
                {
                    var Kdata = dataList.FirstOrDefault();
                    //var isData = await _dbContext.Queryable<PartsLife>().Where(x => x.CH == Kdata.LCId.ToString()).ToListAsync();
                    //if (isData.Count > 0)
                    //{
                    //    return;
                    //}
                    //var config = _dbContext.Queryable<SYS_Config>();
 
                    //获取线路名称
                    //var XL = config.Where(x => x.concode == "GZML7S").First()?.conval;
   
                    //获取寿命部件

                    string sql = @"SELECT
	                            bj.csname AS Name, 
	                            bj.csval AS Code, 
	                            bj.csdw AS Type, 
	                            bj.sort AS RatedLife,
                                bj.sxname AS FarecastCode
                            FROM
	                            dbo.DEVCSXD AS bj
                            WHERE
	                            bj.cswz = 'smbj'";

                    //var smbj = await _dbContext.SqlQueryable<PartsLifeDTO>(sql).ToListAsync();
                    //var equipments = await _dbContext.Queryable<EquipmentFault>().ToListAsync();

                    //var propertyCache = new ConcurrentDictionary<string, Func<object, object>>();

                    //foreach (var data in dataList)
                    //{
                    //    Type type = data.GetType();

                    //    foreach (var item in smbj)
                    //    {
                    //        string propertyName = item.Code;
                    //        var faultCode = equipments.Where(x => x.AnotherName == propertyName && x.HvacType == data.yxtzjid).FirstOrDefault()?.FaultCode;

                    //        // 尝试从缓存中获取属性访问器  
                    //        if (!propertyCache.TryGetValue(propertyName, out var getValue))
                    //        {
                    //            // 如果缓存中不存在，则使用反射创建并添加到缓存中  
                    //            PropertyInfo propertyInfo = type.GetProperty(propertyName);
                    //            if (propertyInfo != null)
                    //            {
                    //                // 注意：这里我们假设属性可以安全地转换为 object，然后在需要时转换为具体类型  
                    //                getValue = obj => propertyInfo.GetValue(obj, null);
                    //                propertyCache.TryAdd(propertyName, getValue); // 尝试添加到缓存中，防止并发时的重复添加  
                    //            }
                    //            else
                    //            {
                    //                // 如果属性不存在，则可以根据需要处理（例如记录错误、跳过等）  
                    //                continue; // 这里我们选择跳过当前 item  
                    //            }
                    //        }
                    //        string? code = null;
                    //        decimal? value = null;

                    //        if (item.FarecastCode != null)
                    //        {
                    //            //code = data.yxtzjid == 1 ? "HVAC01" : "HVAC02";
                    //        }
                    //        if (item.Type == "H")
                    //        {
                    //            //8月13号初始值
                    //            value = 2025;// 使用缓存中的访问器来获取属性值  
                    //        }
                    //        else
                    //        {
                    //            value = Convert.ToDecimal(getValue(data));
                    //        }

                    //        // 创建 PartsLife 对象并设置属性  
                    //        var partLife = new PartsLife
                    //        {
                    //            XL = XL,
                    //            //CH = data.lch,
                    //            //CX = data.cxh,
                    //            //WZ = data.yxtzjid,
                    //            createtime = DateTime.Now,
                    //            updatetime = DateTime.Now,
                    //            RunLife = value, // 使用缓存中的访问器来获取属性值  
                    //            Name = item.Name,
                    //            Code = item.Code,
                    //            FarecastCode = code+item.FarecastCode,
                    //            Type = item.Type,
                    //            RatedLife = item.RatedLife,
                    //            FaultCode = faultCode
                    //        };

                    //        result.Add(partLife);
                    //    }
                    //}
                    
                        //await _dbContext.Insertable(result).ExecuteCommandAsync();
                        //await Console.Out.WriteLineAsync("寿命数据新增成功");

                    DataCacheService.AddPartsLifes(result);
                                     
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.ToString());
                    _logger.LogError($"添加寿命数据时出错：{ex.ToString()}");
                }
            }          
        }

        //更新实时数据
        private async Task UpdateDataNow(List<KAFKA_DATA> data)
        {
            var lch = data.First().TrainId;
            
            var realData = _mapper.Map<List<TB_PARSING_NOWDATAS>>(data);
            var isRealData = _dbContext.Queryable<TB_PARSING_NOWDATAS>().Where(x => x.TrainId == lch);
            if (isRealData.Any())
            {
                foreach (var item in realData)
                {
                    var newData = isRealData.First(x => x.YZJID == item.YZJID && x.TrainId == item.TrainId);
                    item.Id = newData.Id;                 
                    item.UpdateTime = DateTime.Now;
                }

                var updata = await _dbContext.Updateable(realData).ExecuteCommandAsync();

                if (updata>0)
                {
                    await Console.Out.WriteLineAsync($"实时数据更新成功");
                }
            }
            else
            {
                var add = await _dbContext.Insertable(realData).ExecuteCommandAsync();
                if (add >0)
                {
                    await Console.Out.WriteLineAsync("实时数据新增成功");
                }
                
            }

        }
    }
}
