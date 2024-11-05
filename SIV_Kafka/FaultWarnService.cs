using AutoMapper;
using Confluent.Kafka;
using DataBase;
using DataBase.Entity;
using KP.Util;
using Microsoft.Data.SqlClient.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SqlSugar;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Reflection;
using System.Xml.Linq;
using Util;

namespace SIV_Kafka
{
    /// <summary>
    /// 预警报警定时服务
    /// </summary>
    public class FaultWarnService : BackgroundService
    {
        //同步预警报警分析系统平台处理结果
        private Timer _warnHandleResTimer;
        //定时删除任务
        private Timer _deleteTimer;
        //故障定时任务
        private Timer _faultTimer;
        //测试用
        private Timer _testTimer;
        //寿命推送
        private Timer _lifetTimer;
        //预警定时任务
        private Timer _warnTimer;
        //物模型上传卡夫卡任务
        private Timer _hvacmodleTimer;

        private readonly static Dictionary<string, string> _setting = Helper.LoadJsonData("SIV");
        private readonly string _appId = _setting["AppId"];
        private readonly string _appKey = _setting["AppKey"];
        private readonly string _baseUrl = _setting["BaseUrl"];
        private readonly string _lineCode = _setting["LineCode"];
        private readonly string _GetHttpHandle = _setting["GetHttpHandleTime"];
        private readonly string _FaultDataPush = _setting["FaultDataPushTime"];
        private readonly string _AddOrUpdateSMData = _setting["AddOrUpdateSMDataTime"];
        private readonly string _AddWarnData = _setting["AddWarnDataTime"];
        private readonly string _XieruKafkaAsync = _setting["XieruKafkaAsync"];


        private readonly ILogger<FaultWarnService> _logger;
        //private readonly SqlSugarClient _db;
        private readonly MyDbContext _db;
        private readonly IMapper _mapper;
        private readonly AppSettings _appSettings;

        public FaultWarnService(IOptions<AppSettings> appSettings, ILogger<FaultWarnService> logger, MyDbContext dbContext, IMapper mapper)
        {
            _logger = logger;
            _db = dbContext;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {

            _deleteTimer = new Timer(async _ =>
            {

                _logger.LogInformation("定时删除任务");

                await Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // 计算今天午夜的时间（假设现在是UTC时间，你可能需要调整为本地时间）  
                        DateTime now = DateTime.Now;
                        DateTime midnight = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Local);

                        // 如果已经过了今天的午夜，则计算明天的午夜  
                        if (now > midnight)
                        {
                            midnight = midnight.AddDays(1);
                        }

                        // 计算距离下一次午夜还有多长时间  
                        TimeSpan timeToWait = midnight - now;

                        // 等待直到午夜  
                        try
                        {
                            await Task.Delay(timeToWait, cancellationToken);

                            // 执行你的任务  
                            //await Delete();
                        }
                        catch (TaskCanceledException)
                        {
                            // 计时器被取消，退出循环  
                            break;
                        }
                    }
                });

            }, null, TimeSpan.Zero, TimeSpan.FromDays(1));

            _warnHandleResTimer = new Timer(async _ =>
            {
                _logger.LogInformation("开始同步分析平台处理结果");
                //await GetHttpHandle();
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(Convert.ToDouble(_GetHttpHandle)));


            _testTimer = new Timer(async _ =>
            {
                _logger.LogInformation("开始添加测试数据");
                //await GetHttpHandle();

            }, null, TimeSpan.Zero, TimeSpan.FromDays(1));


            _faultTimer = new Timer(async _ =>
            {
                _logger.LogInformation("开始同步故障");
                //await FaultDataPush();

            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(Convert.ToDouble(_FaultDataPush)));

            _lifetTimer = new Timer(async _ =>
            {
                _logger.LogInformation("开始更新寿命并推送");
                //await AddOrUpdateSMData();
                //await AddOrUpdateSMDataV1();

            }, null, TimeSpan.Zero, TimeSpan.FromHours(Convert.ToDouble(_AddOrUpdateSMData)));

            _warnTimer = new Timer(async _ =>
            {
                _logger.LogInformation("开始预警");

                try
                {
                    await GetWycgqFault();//8.1 网压传感器故障预警
                    //await GetYsjFault();
                    //await GetZfqzdFault();
                    //await GetZljxlFault();
                    //await GetZlmbwdFault();
                    //await GetSbdlzFault();
                    //await GetLwzdFault();
                    //await GetLnqzdFault();
                    //await GetKqzlFault();
                    //await GetLnjcfdlFault();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"预警失败，{ex.ToString()}");
                }
               

            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(Convert.ToDouble(_AddWarnData)));

            _hvacmodleTimer = new Timer(async _ =>
            {
                _logger.LogInformation("物模型写入卡夫卡");
                //await XieruKafkaAsync();

            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(Convert.ToDouble(_XieruKafkaAsync)));

            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _faultTimer?.Change(Timeout.Infinite, 0);
            _lifetTimer?.Change(Timeout.Infinite, 0);
            _warnHandleResTimer?.Change(Timeout.Infinite, 0);
            _warnTimer?.Change(Timeout.Infinite, 0);
            _deleteTimer?.Change(Timeout.Infinite, 0);
            _hvacmodleTimer?.Change(Timeout.Infinite, 0);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 删除一个月前的数据
        /// </summary>
        /// <returns></returns>
        public async Task Delete()
        {
            var date = DateTime.Today.AddDays(-30).ToString("yyyyMMdd");
            var ysbwDate = DateTime.Today.AddDays(-30).ToString("yyyyMMdd");

            try
            {
                var sql = $@"if object_id('TB_PARSING_DATAS_{date}','U') is not null 
                           drop table  TB_PARSING_DATAS_{date}";

                var ysbwSql = $@"if object_id('TB_YSBW_{ysbwDate}','U') is not null 
                           drop table TB_YSBW_{ysbwDate}";

                //删除过期数据
                await _db.Ado.ExecuteCommandAsync(sql);
                await _db.Ado.ExecuteCommandAsync(ysbwSql);

                _logger.LogError("删除成功");
            }
            catch (Exception ex)
            {
                _logger.LogError($"删除失败,{ex}");
            }

        }

        ///// <summary>
        ///// 更新寿命数据并推送
        ///// </summary>
        ///// <param name="dataList"></param>
        ///// <returns></returns>
        ///// <exception cref="Exception"></exception>
        //private async Task AddOrUpdateSMData()
        //{
        //    var result = new List<PartsLife>();

        //    try
        //    {
        //        var nowdate = DateTime.Now;
        //        var startime = nowdate.AddMinutes(-5);
        //        var dataTime = DateTime.Now.ToString("yyyyMMdd");

        //        var nowLifeSql = $@"SELECT
        //                                 s.jz1lnfj1ljgzsj, 
        //                                 s.jz1lnfj2ljgzsj, 
        //                                 s.jz1lnfj1zcljgzsj, 
        //                                 s.jz1lnfj2zcljgzsj, 
        //                                 s.jz1tfj1ljgzsj, 
        //                                 s.jz1tfj2ljgzsj, 
        //                                 s.jz1tfj1zcljgzsj, 
        //                                 s.jz1tfj2zcljgzsj, 
        //                                 s.jz1ysj1ljgzsj, 
        //                                 s.jz1ysj2ljgzsj, 
        //                                 s.jz1kqjhqljgzsj, 
        //                                 s.jz1zwxdljgzsj, 
        //                                 s.jz1kqjhqdgljgzsj, 
        //                                 s.jz1zwxddgljgzsj, 
        //                                 s.jz1tfjjjtfjcqdzcs, 
        //                                 s.jz1tfjjcqdzcs, 
        //                                 s.jz1tfj2jcqdzcs, 
        //                                 s.jz1lnfjjcqdzcs, 
        //                                 s.jz1ysj1jcqdzcs, 
        //                                 s.jz1ysj2jcqdzcs, 
        //                                 s.lch, 
        //                                 s.cxh, 
        //                                 s.yxtzjid, 
        //                                 s.device_code, 
        //                                 s.cxhName, 
        //                                 s.create_time
        //                                FROM
        //                                 dbo.TB_PARSING_NEWDATAS AS s
        //                                ";
        //        var lifeData = await _db.SqlQueryable<TB_PARSING_NEWDATAS>(nowLifeSql).ToListAsync();

        //        var partsLifeData = await _db.Queryable<PartsLife>().Where(x => x.Type == "次").ToListAsync();

        //        var propertyCache = new ConcurrentDictionary<string, Func<object, object>>();

        //        foreach (var life in lifeData)
        //        {
        //            var data = partsLifeData.Where(x => x.CX == life.cxh && x.WZ == life.yxtzjid).ToList();

        //            if (data != null)
        //            {
        //                Type type = life.GetType();

        //                foreach (var item in data)
        //                {
        //                    string propertyName = item.Code;

        //                    // 尝试从缓存中获取属性访问器  
        //                    if (!propertyCache.TryGetValue(propertyName, out var getValue))
        //                    {
        //                        // 如果缓存中不存在，则使用反射创建并添加到缓存中  
        //                        PropertyInfo propertyInfo = type.GetProperty(propertyName);
        //                        if (propertyInfo != null)
        //                        {
        //                            // 注意：这里我们假设属性可以安全地转换为 object，然后在需要时转换为具体类型  
        //                            getValue = obj => propertyInfo.GetValue(obj, null);
        //                            propertyCache.TryAdd(propertyName, getValue); // 尝试添加到缓存中，防止并发时的重复添加  
        //                        }
        //                        else
        //                        {
        //                            // 如果属性不存在，则可以根据需要处理（例如记录错误、跳过等）  
        //                            continue; // 这里我们选择跳过当前 item  
        //                        }
        //                    }
        //                    var runLife = Convert.ToInt32(getValue(life));

        //                    item.RunLife = runLife;
        //                    item.updatetime = life.rq;

        //                    result.Add(item);

        //                }
        //            }
        //        }

        //        var updateNum = await _db.Updateable(result)
        //                 .UpdateColumns(it => new { it.RunLife, it.updatetime })
        //                 .WhereColumns(it => it.Id)
        //                 .ExecuteCommandAsync();
        //        await Console.Out.WriteLineAsync($"寿命数据更新成功,更新了{updateNum}条数据");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"更新寿命数据时出错：{ex.ToString()}");
        //    }
        //}

        ///// <summary>
        ///// 更新寿命数据并推送
        ///// </summary>      
        ///// <returns></returns>
        ///// <exception cref="Exception"></exception>
        //private async Task AddOrUpdateSMDataV1()
        //{
        //    var result = new List<PartsLife>();

        //    try
        //    {
        //        var nowLifeSql = $@"SELECT
        //                                 s.jz1lnfj1ljgzsj, 
        //                                 s.jz1lnfj2ljgzsj, 
        //                                 s.jz1lnfj1zcljgzsj, 
        //                                 s.jz1lnfj2zcljgzsj, 
        //                                 s.jz1tfj1ljgzsj, 
        //                                 s.jz1tfj2ljgzsj, 
        //                                 s.jz1tfj1zcljgzsj, 
        //                                 s.jz1tfj2zcljgzsj, 
        //                                 s.jz1ysj1ljgzsj, 
        //                                 s.jz1ysj2ljgzsj, 
        //                                 s.jz1kqjhqljgzsj, 
        //                                 s.jz1zwxdljgzsj, 
        //                                 s.jz1kqjhqdgljgzsj, 
        //                                 s.jz1zwxddgljgzsj, 
        //                                 s.jz1tfjjjtfjcqdzcs, 
        //                                 s.jz1tfjjcqdzcs, 
        //                                 s.jz1tfj2jcqdzcs, 
        //                                 s.jz1lnfjjcqdzcs, 
        //                                 s.jz1ysj1jcqdzcs, 
        //                                 s.jz1ysj2jcqdzcs, 
        //                                 s.lch, 
        //                                 s.cxh, 
        //                                 s.yxtzjid, 
        //                                 s.device_code, 
        //                                 s.cxhName, 
        //                                 s.create_time,
        //                                    s.rq,
        //                                    s.jz1zwxdzt, 
        //                                 s.jz1kqjhqzt, 
        //                                 s.jz1tfj1zt, 
        //                                 s.jz1tfj2zt, 
        //                                 s.jz1lnfj1zt, 
        //                                 s.jz1lnfj2zt, 
        //                                 s.jz1ysj1zt, 
        //                                 s.jz1ysjj2zt, 
        //                                 s.jjtfzt
        //                                FROM
        //                                 dbo.TB_PARSING_DATAS" + $"_{DateTime.Now:yyyyMMdd} " + " AS s";

        //        var time = DateTime.Now.AddHours(-1);
        //        var lifeData = await _db.SqlQueryable<TB_PARSING_DATAS>(nowLifeSql).Where(x => x.rq > time).ToListAsync();
        //        if (lifeData.Count == 0)
        //        {
        //            return;
        //        }

        //        var partsLifeData = await _db.Queryable<PartsLife>().Where(x => x.Type == "H").ToListAsync();

        //        var propertyCache = new ConcurrentDictionary<string, Func<object, object>>();

        //        var qTime = Convert.ToDecimal(_AddOrUpdateSMData);

        //        foreach (var life in partsLifeData)
        //        {
        //            var codeMappings = new Dictionary<string, (Func<TB_PARSING_DATAS, bool> condition, string zeroStatusProperty)>
        //            {
        //                { "jz1zwxdljgzsj", (x => x.jz1zwxdzt == 0, "jz1zwxdzt") },
        //                { "jz1zwxddgljgzsj", (x => x.jz1zwxdzt == 0, "jz1zwxdzt") },
        //                { "jz1kqjhqljgzsj", (x => x.jz1kqjhqzt == 0, "jz1kqjhqzt") },
        //                { "jz1kqjhqdgljgzsj", (x => x.jz1kqjhqzt == 0, "jz1kqjhqzt") },
        //                { "jz1lnfj1ljgzsj", (x => x.jz1lnfj1zt == 0, "jz1lnfj1zt") },
        //                { "jz1lnfj1zcljgzsj", (x => x.jz1lnfj1zt == 0, "jz1lnfj1zt") },
        //                { "jz1lnfj2ljgzsj", (x => x.jz1lnfj2zt == 0, "jz1lnfj2zt") },
        //                { "jz1lnfj2zcljgzsj", (x => x.jz1lnfj2zt == 0, "jz1lnfj2zt") },
        //                { "jz1tfj1ljgzsj", (x => x.jz1tfj1zt == 0, "jz1tfj1zt") },
        //                { "jz1tfj1zcljgzsj", (x => x.jz1tfj1zt == 0, "jz1tfj1zt") },
        //                { "jz1tfj2ljgzsj", (x => x.jz1tfj2zt == 0, "jz1tfj2zt") },
        //                { "jz1tfj2zcljgzsj", (x => x.jz1tfj2zt == 0, "jz1tfj2zt") },
        //                { "jz1ysj1ljgzsj", (x => x.jz1ysj1zt == 0, "jz1ysj1zt") },
        //                { "jz1ysj2ljgzsj", (x => x.jz1ysjj2zt == 0, "jz1ysjj2zt") }
        //            };

        //            if (codeMappings.TryGetValue(life.Code, out var mapping))
        //            {
        //                var runData = lifeData.Where(x => mapping.condition(x) && x.cxh == life.CX && x.yxtzjid == life.WZ);
        //                var runLife = Math.Round((decimal)runData.Count() / 3600, 1);
        //                life.RunLife += runLife;
        //                life.updatetime = runData.OrderByDescending(x => x.rq).FirstOrDefault().rq;
        //            }

        //            result.Add(life);
        //        }

        //        var updateNum = await _db.Updateable(result)
        //                 .UpdateColumns(it => new { it.RunLife, it.updatetime })
        //                 .WhereColumns(it => it.Id)
        //                 .ExecuteCommandAsync();
        //        await Console.Out.WriteLineAsync($"寿命数据更新成功,更新了{updateNum}条数据");

        //        var pushLifeData = result.Where(x => x.FaultCode != null).ToList();

        //        //寿命推送
        //        pushLifeData = pushLifeData.Where(x => !string.IsNullOrEmpty(x.FarecastCode)).ToList();
        //        await LifeDataPush(pushLifeData);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"更新寿命数据时出错：{ex.ToString()}");
        //    }
        //}

        ///// <summary>
        ///// 寿命推送
        ///// </summary>
        ///// <returns></returns>
        //private async Task LifeDataPush(List<PartsLife> partsLife)
        //{
        //    string urlType = "life-prediction";

        //    // 构建app_token
        //    string appToken = $"app_id={_appId}&app_key={_appKey}&date=" + DateTime.Now.ToString("yyyy-MM-dd");
        //    string tokenMd5 = Helper.GetMD5String(appToken).ToUpper();
        //    string url = $"{_baseUrl}{urlType}";

        //    List<LifeResDTO> newlife = new List<LifeResDTO>();

        //    var data = new LifeHttpResDTO();

        //    try
        //    {
        //        foreach (var part in partsLife)
        //        {
        //            newlife.Add(new LifeResDTO
        //            {
        //                line_code = _lineCode,
        //                train_code = part.CH,
        //                coach_no = part.CX,
        //                life_code = part.FarecastCode,
        //                prediction_time = DateTime.Now.ToString(),
        //                running_day = (part.RunLife / 24).ToString(),
        //                residual_day = ((part.RatedLife - part.RunLife) / 24).ToString()
        //            });
        //        }

        //        data.app_id = _appId;
        //        data.app_token = tokenMd5;
        //        data.lifes = newlife;
        //        var trainSta = await HttpClientExample.SendPostRequestAsync<HttpReq>(url, data);
        //        if (trainSta != null)
        //        {
        //            if (trainSta.result_code == "200")
        //            {
        //                await Console.Out.WriteLineAsync("寿命数据推送成功");
        //            }
        //            else
        //            {
        //                await Console.Out.WriteLineAsync("寿命数据推送失败");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"寿命数据推送错误，{ex.ToString()}");
        //    }

        //}

        ///// <summary>
        ///// 添加/推送故障信息
        ///// </summary>
        ///// <returns></returns>
        //private async Task FaultDataPush()
        //{
        //    var addFaults = new List<FaultOrWarn>();
        //    var updateFaults = new List<FaultOrWarn>();

        //    try
        //    {
        //        var config = _db.Queryable<SYS_Config>().ToList();
        //        //获取故障时间
        //        var sj = config.Where(x => x.concode == "FaultSj").First()?.conval;

        //        //获取线路名称
        //        var XL = config.Where(x => x.concode == _lineCode).First()?.conval;

        //        string sql = @"SELECT
        //	pd.jz1zhl1dlqgz, 
        //	pd.jz1zhl2dlqgz, 
        //	pd.jz1ysj1dlqgz, 
        //	pd.jz1ysj2dlqgz, 
        //	pd.jz1tfjjcqgz, 
        //	pd.jz1lnfjjcqgz, 
        //	pd.jz1tfjjjtfjcqgz, 
        //	pd.jz1ysj1jcqgz, 
        //	pd.jz1ysj2jcqgz, 
        //	pd.jz1ysj1gygz, 
        //	pd.jz1ysj1dygz, 
        //	pd.jz1ysj2gygz, 
        //	pd.jz1ysj2dygz, 
        //	pd.zygz, 
        //	pd.jjtfnbqgz, 
        //	pd.jz1zwxdgz, 
        //	pd.jz1tfj1gzgz, 
        //	pd.jz1tfj2gzgz, 
        //	pd.jz1lnfj1gzgz, 
        //	pd.jz1lnfj2gzgz, 
        //	pd.jz1bpq1gz, 
        //	pd.jz1bpq2gz, 
        //	pd.jz1bpq1txgz, 
        //	pd.jz1bpq2txgz, 
        //	pd.jz1xff1gz, 
        //	pd.jz1xff2gz, 
        //	pd.jz1hff1gz, 
        //	pd.jz1hff2gz, 
        //	pd.kswdcgq1gz, 
        //	pd.jz1hfcgqgz, 
        //	pd.jz1xfcgqgz, 
        //	pd.jz1sfcgq1gz, 
        //	pd.jz1sfcgq2gz, 
        //	pd.jz1kqjhqgz, 
        //	pd.jz1ysj1pqwdcgqgz, 
        //	pd.jz1ysj1xqwdcgqgz, 
        //	pd.jz1ysj2pqwdcgqgz, 
        //	pd.jz1ysj2xqwdcgqgz, 
        //	pd.jz1ysj1pqwdgz, 
        //	pd.jz1ysj2pqwdgz, 
        //	pd.jz1cjmk1txgz, 
        //	pd.jz1cjmk2txgz, 
        //	pd.jz1kqzljcmkgz, 
        //	pd.jz1gyylcgq1gz, 
        //	pd.jz1dyylcgq1gz, 
        //	pd.jz1gyylcgq2gz, 
        //	pd.jz1dyylcgq2gz, 
        //	pd.jz1qwgz, 
        //	pd.jz1zdgz, 
        //	pd.jz1yzgz, 
        //	pd.jz1ysj1gzgz, 
        //	pd.jz1ysj2gzgz, 
        //	pd.jz1zwxd1gz, 
        //	pd.jz1zwxd2gz, 
        //	pd.jz1tfj1gsjcqgz, 
        //	pd.jz1tfj2gsjcqgz, 
        //	pd.jz1tfj1dsjcqgz, 
        //	pd.jz1tfj2dsjcqgz, 
        //	pd.fpfjgz, 
        //	pd.fpfjjcqgz, 
        //	pd.fpfjjjtfjcqgz, 
        //	pd.fpffgz, 
        //	pd.fhfgz,
        //                            pd.yccgqgz, 
        //                         pd.airwdcgqgz, 
        //                         pd.airsdcgqgz, 
        //                         pd.airco2cgqgz, 
        //                         pd.airpm25cgqgz, 
        //                         pd.airtvoccgqgz,
        //	pd.lch, 
        //	pd.cxh,
        //                            pd.cxhName,
        //	pd.device_code, 
        //	pd.yxtzjid,
        //                            pd.rq
        //FROM
        //	dbo.TB_PARSING_DATAS" + $"_{DateTime.Now:yyyyMMdd} " + "AS pd " +
        //                        $@"WHERE
        //	pd.rq >= DATEADD(MINUTE,-{sj},GETDATE())";

        //        //获取故障编码
        //        var equipments = await _db.Queryable<EquipmentFault>().Where(x => !string.IsNullOrEmpty(x.AnotherName)).ToListAsync();
        //        var faultData = _db.Queryable<FaultOrWarn>().ToList();
        //        var newDataQ = _db.Queryable<TB_PARSING_NEWDATAS>().ToList();
        //        //获取故障数据
        //        var faults = await _db.SqlQueryable<FaultDTO>(sql).OrderBy(x => x.rq).ToListAsync();

        //        foreach (var item in faults)
        //        {
        //            var dic = GetPropertiesAndValues(item);
        //            var faultdic = dic.Where(x => x.Value.ToString() == "1");

        //            foreach (var item1 in faultdic)
        //            {
        //                var faultCode = equipments.Where(x => x.AnotherName == item1.Key && x.CXH == item.cxhName && (x.HvacType == item.yxtzjid || x.HvacType == 3)).FirstOrDefault();
        //                if (faultCode == null) continue;

        //                var isAny1 = faultData.Any(x => x.DeviceCode == item.device_code && x.Code == faultCode.FaultCode && x.State == "1");
        //                if (isAny1) continue;

        //                var isAny2 = addFaults.Any(x => x.DeviceCode == item.device_code && x.Code == faultCode.FaultCode && x.State == "1");
        //                if (isAny2) continue;

        //                var faultOrWarn = new FaultOrWarn
        //                {
        //                    xlh = XL,
        //                    lch = item.lch,
        //                    cxh = item.cxh,
        //                    DeviceCode = item.device_code,
        //                    Code = faultCode.FaultCode,
        //                    FaultType = faultCode.Type,
        //                    Name = faultCode.FaultName,
        //                    Type = "1",
        //                    State = "1",
        //                    createtime = item.rq
        //                };
        //                addFaults.Add(faultOrWarn);
        //            }
        //        }

        //        var faultOn = faultData.Where(x => x.State == "1" && x.Type == "1").ToList();
        //        foreach (var On in faultOn)
        //        {
        //            var Aname = equipments.Where(x => x.FaultCode == On.Code).First();
        //            if (Aname == null) continue;

        //            var newData = newDataQ.Where(x => x.device_code == On.DeviceCode).First();
        //            if (newData == null) continue;

        //            var dic = GetPropertiesAndValues(newData);
        //            dic.TryGetValue(Aname.AnotherName, out var value);
        //            if (value.ToString() == "0")
        //            {
        //                On.State = "0";
        //                On.updatetime = newData.update_time;
        //                updateFaults.Add(On);
        //            }
        //        }

        //        var num = _db.Insertable(addFaults).ExecuteCommand();
        //        var num1 = _db.Updateable(updateFaults).UpdateColumns(it => new FaultOrWarn { State = it.State, updatetime = it.updatetime }).ExecuteCommand();

        //        _logger.LogInformation($"故障同步完成，新增了{num}条故障,关闭了{num1}条故障");

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"故障信息添加失败，{ex.ToString()}");
        //    };
        //}



        //static Dictionary<string, object> GetPropertiesAndValues<T>(T obj)
        //{
        //    var dict = new Dictionary<string, object>();

        //    // 获取对象的Type信息  
        //    Type type = obj.GetType();

        //    // 获取所有公共属性的PropertyInfo数组  
        //    PropertyInfo[] properties = type.GetProperties();

        //    // 遍历属性  
        //    foreach (PropertyInfo prop in properties)
        //    {
        //        // 获取属性值并添加到字典中  
        //        // 注意：这里假设所有属性都有getter，否则会抛出异常  
        //        dict.Add(prop.Name, prop.GetValue(obj, null));
        //    }

        //    return dict;
        //}

        ///// <summary>
        ///// 获取平台故障预警处理信息并更新
        ///// </summary>
        ///// <returns></returns>
        //public async Task GetHttpHandle()
        //{
        //    string urlType = "warn-status";

        //    // 构建app_token
        //    string appToken = $"app_id={_appId}&app_key={_appKey}&date=" + DateTime.Now.ToString("yyyy-MM-dd");
        //    string tokenMd5 = Helper.GetMD5String(appToken).ToUpper();
        //    var updateFaults = new List<FaultOrWarn>();
        //    try
        //    {
        //        var q = _db.Queryable<FaultOrWarn>().Where(x => x.State == "1" && x.Type == "2" && x.SendRepId != 0);
        //        var idList = q.OrderByDescending(x => x.createtime).Select(x => x.SendRepId).ToList();

        //        var groups = idList.Chunk(300);

        //        foreach (var group in groups)
        //        {                   
        //            var ids = string.Join(",", group);
        //            string url = $"{_baseUrl}{urlType}?app_id={_appId}&app_token={tokenMd5}&warn_id={ids}";
        //            var faultReq = await HttpClientExample.SendGetRequestAsync<HttpReq<List<WarnStateReq>>>(url);
        //            if (faultReq != null)
        //            {
        //                var faultstate = faultReq.result_data.Where(x => x.status == 0).Select(x => x.id) ;
        //                var data = q.Where(x => faultstate.Contains(x.SendRepId)).ToList();            
        //                var updatenum = await _db.Updateable(data).UpdateColumns(x => new FaultOrWarn { State = "0",updatetime = DateTime.Now}).ExecuteCommandAsync();
        //                _logger.LogInformation($"预警同步完成，更新了{updatenum}条预警");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"报警更新失败，{ex.ToString()}");
        //    }
        //}

        ///// <summary>
        ///// 故障推送
        ///// </summary>
        ///// <param name="newFault"></param>
        ///// <param name="endFault"></param>
        ///// <returns></returns>
        //public async Task<HttpReq<FaultReq>> FaultSetHttpPost(List<FaultOrWarn> newFault, List<FaultOrWarn> endFault)
        //{

        //    string urlType = "fault-assess";

        //    // 构建app_token
        //    string appToken = $"app_id={_appId}&app_key={_appKey}&date=" + DateTime.Now.ToString("yyyy-MM-dd");
        //    string tokenMd5 = Helper.GetMD5String(appToken).ToUpper();
        //    string url = $"{_baseUrl}{urlType}";

        //    var new_faults = new List<FaultsModels>();
        //    var end_faults = new List<FaultsModels>();

        //    foreach (var item in newFault)
        //    {
        //        new_faults.Add(new FaultsModels()
        //        {
        //            fault_name = item.Name,
        //            line_code = _lineCode,
        //            train_code = item.lch,
        //            coach_no = item.cxh.Substring(2),
        //            fault_code = item.Code,
        //            access_time = item.createtime?.ToString("yyyy-MM-dd HH:mm:ss")
        //        });
        //    }

        //    foreach (var item in endFault)
        //    {
        //        end_faults.Add(new FaultsModels()
        //        {
        //            fault_name = item.Name,
        //            line_code = _lineCode,
        //            train_code = item.lch,
        //            fault_code = item.Code,
        //            coach_no = item.cxh.Substring(2),
        //            access_time = item.updatetime?.ToString("yyyy-MM-dd HH:mm:ss")
        //        });
        //    }

        //    var request = new Fault_AssessModels()
        //    {
        //        app_id = _appId,
        //        app_token = tokenMd5,
        //        new_faults = new_faults,
        //        end_faults = end_faults
        //    };

        //    var faultReq = await HttpClientExample.SendPostRequestAsync<HttpReq<FaultReq>>(url, request);

        //    return faultReq;
        //}

        //#region 预警模型
        /// <summary>
        /// 8.1网压传感器故障预警模型
        /// </summary>
        /// <returns></returns>
        private async Task GetWycgqFault()
        {
            //EquipmentFault xfWarn1, sfWarn1, sfWarn2, hfWarn1;
            var addFaults = new List<FaultOrWarn>();
            var addIndicators = new List<Indicators_Item>();
            var nowData = _db.Queryable<TB_PARSING_NOWDATAS>().ToList().GroupBy(x => x.TrainId);
            var faultData = _db.Queryable<FaultOrWarn>().ToList();
            //获取线路名称
            //var XL = config.Where(x => x.concode == _lineCode).First().conval;
            var newdata = await GetNewData(1);

            foreach (var item in nowData)
            {
                if (
                    item.First().U_DC_In > 1000 && item.First().U_DC_In <= 1800
                    &&item.First().InConv_Event != 53 
                    && item.First().BC_OpMode == 0 && item.First().Inv_OpMode == 0 && item.First().Inconv_OpMode == 0
                    )
                {

                    var add = AddOffsetDivisor(item.First().U_DC_In, item.Last().U_DC_In, item.First().DeviceCode,1);
                    addIndicators.Add(add);

                    var data = newdata.Where(x => x.TrainId == item.First().TrainId && x.YZJID == item.First().YZJID).ToList();
                    var add1 = AddU_DC_InAvg(data, item.First().DeviceCode,2);
                    addIndicators.Add(add1);
                }

                if (
                    item.Last().U_DC_In > 1000 && item.Last().U_DC_In <= 1800
                    &&item.Last().InConv_Event != 53 
                    && item.Last().BC_OpMode == 0 && item.Last().Inv_OpMode == 0 && item.Last().Inconv_OpMode == 0
                    )                
                {
                    var add = AddOffsetDivisor(item.Last().U_DC_In, item.First().U_DC_In, item.Last().DeviceCode,1);
                    addIndicators.Add(add);

                    var data = newdata.Where(x => x.TrainId == item.Last().TrainId && x.YZJID == item.Last().YZJID).ToList();
                    var add1 = AddU_DC_InAvg(data, item.Last().DeviceCode,2);
                    addIndicators.Add(add1);
                }

                if (
                   item.First().U_DC_In <= 1000
                   && item.First().BC_OpMode == 1 && item.First().Inv_OpMode == 1 && item.First().Inconv_OpMode == 1
                   )
                {
                    var add = AddU_DC_InAbs(item.First().U_DC_In, item.First().DeviceCode,3);
                    addIndicators.Add(add);
                }

                if (
                   item.Last().U_DC_In <= 1000 
                   && item.Last().BC_OpMode == 1 && item.Last().Inv_OpMode == 1 && item.Last().Inconv_OpMode == 1
                   )
                {
                    var add = AddU_DC_InAbs(item.Last().U_DC_In, item.Last().DeviceCode, 3);
                    addIndicators.Add(add);
                }
            }


            if (addFaults.Count > 0)
            {
                //var faultReq = await FaultSetHttpPost(addFaults, new List<FaultOrWarn>());

                //if (faultReq != null && faultReq.result_code == "200")
                //{
                //    _logger.LogInformation($"温度异常预警推送成功，新增了{addFaults.Count}条预警");

                //    for (int i = 0; i < addFaults.Count; i++)
                //    {
                //        addFaults[i].SendRepId = faultReq.result_data.new_faults[i];
                //    }
                //}
                var addnum = _db.Insertable(addIndicators).ExecuteCommand();
                _logger.LogInformation($"温度异常预警同步完成，新增了{addnum}条预警");
            }
        }

        /// <summary>
        /// 8.2中间电压传感器故障预警模型
        /// </summary>
        /// <returns></returns>
        private async Task GetZjdycgqFault()
        {
            //EquipmentFault xfWarn1, sfWarn1, sfWarn2, hfWarn1;
            var addFaults = new List<FaultOrWarn>();
            var addIndicators = new List<Indicators_Item>();
            var nowData = _db.Queryable<TB_PARSING_NOWDATAS>().ToList().GroupBy(x => x.TrainId);
            var faultData = _db.Queryable<FaultOrWarn>().ToList();
            //获取线路名称
            //var XL = config.Where(x => x.concode == _lineCode).First().conval;
            var newdata = await GetNewData(1);

            foreach (var item in nowData)
            {
                if (
                    item.First().U_DC_Link_Inv > 1000 && item.First().U_DC_Link_Inv <= 1800
                    && item.First().BC_OpMode == 0 && item.First().Inv_OpMode == 0 && item.First().Inconv_OpMode == 0
                    )
                {
                    var add = AddOffsetDivisor(item.First().U_DC_Link_Inv, item.Last().U_DC_Link_Inv, item.First().DeviceCode, 4);
                    addIndicators.Add(add);

                    var data = newdata.Where(x => x.TrainId == item.First().TrainId && x.YZJID == item.First().YZJID).ToList();
                    var add1 = AddU_DC_Link_InvAvg(data, item.First().DeviceCode, 5);
                    addIndicators.Add(add1);
                }

                if (
                    item.Last().U_DC_Link_Inv > 1000 && item.Last().U_DC_Link_Inv <= 1800
                    && item.Last().BC_OpMode == 0 && item.Last().Inv_OpMode == 0 && item.Last().Inconv_OpMode == 0
                    )
                {
                    var add = AddOffsetDivisor(item.Last().U_DC_Link_Inv, item.First().U_DC_Link_Inv, item.Last().DeviceCode, 4);
                    addIndicators.Add(add);

                    var data = newdata.Where(x => x.TrainId == item.Last().TrainId && x.YZJID == item.Last().YZJID).ToList();
                    var add1 = AddU_DC_Link_InvAvg(data, item.Last().DeviceCode, 5);
                    addIndicators.Add(add1);
                }

                if (
                   item.First().U_DC_Link_Inv <= 1000
                   && item.First().BC_OpMode == 1 && item.First().Inv_OpMode == 1 && item.First().Inconv_OpMode == 1
                   )
                {
                    var add = AddU_DC_InAbs(item.First().U_DC_In, item.First().DeviceCode, 6);
                    addIndicators.Add(add);
                }

                if (
                   item.Last().U_DC_Link_Inv <= 1000
                   && item.Last().BC_OpMode == 1 && item.Last().Inv_OpMode == 1 && item.Last().Inconv_OpMode == 1
                   )
                {
                    var add = AddU_DC_InAbs(item.Last().U_DC_Link_Inv, item.Last().DeviceCode, 6);
                    addIndicators.Add(add);
                }
            }


            if (addFaults.Count > 0)
            {
                //var faultReq = await FaultSetHttpPost(addFaults, new List<FaultOrWarn>());

                //if (faultReq != null && faultReq.result_code == "200")
                //{
                //    _logger.LogInformation($"温度异常预警推送成功，新增了{addFaults.Count}条预警");

                //    for (int i = 0; i < addFaults.Count; i++)
                //    {
                //        addFaults[i].SendRepId = faultReq.result_data.new_faults[i];
                //    }
                //}
                var addnum = _db.Insertable(addIndicators).ExecuteCommand();
                _logger.LogInformation($"温度异常预警同步完成，新增了{addnum}条预警");
            }
        }

        /// <summary>
        /// 8.3输入电流传感器故障预警模型
        /// </summary>
        /// <returns></returns>
        private async Task GetSrdlcgqFault()
        {
            //EquipmentFault xfWarn1, sfWarn1, sfWarn2, hfWarn1;
            var addFaults = new List<FaultOrWarn>();
            var addIndicators = new List<Indicators_Item>();
            var nowData = _db.Queryable<TB_PARSING_NOWDATAS>().ToList().GroupBy(x => x.TrainId);
            var faultData = _db.Queryable<FaultOrWarn>().ToList();
            //获取线路名称
            //var XL = config.Where(x => x.concode == _lineCode).First().conval;
            var newdata = await GetNewData(1);

            foreach (var item in nowData)
            {
                if (
                    item.First().InConv_DIGIN ==0
                    && item.First().BC_OpMode == 0 && item.First().Inv_OpMode == 0 && item.First().Inconv_OpMode == 0
                    )
                {
                    var add = AddOffsetDivisor(item.First().I_DC_In, item.Last().I_DC_In, item.First().DeviceCode, 7);
                    addIndicators.Add(add);
                }

                if (
                    item.Last().InConv_DIGIN == 0
                    && item.Last().BC_OpMode == 0 && item.Last().Inv_OpMode == 0 && item.Last().Inconv_OpMode == 0
                    )
                {
                    var add = AddOffsetDivisor(item.Last().I_DC_In, item.First().I_DC_In, item.Last().DeviceCode, 7);
                    addIndicators.Add(add);

                }

                if (item.First().InConv_DIGIN == 1)
                {
                    var add = AddU_DC_InAbs(item.First().I_DC_In, item.First().DeviceCode, 8);
                    addIndicators.Add(add);
                }

                if (item.Last().InConv_DIGIN == 1)
                {
                    var add = AddU_DC_InAbs(item.Last().I_DC_In, item.Last().DeviceCode, 8);
                    addIndicators.Add(add);
                }
            }


            if (addFaults.Count > 0)
            {
                //var faultReq = await FaultSetHttpPost(addFaults, new List<FaultOrWarn>());

                //if (faultReq != null && faultReq.result_code == "200")
                //{
                //    _logger.LogInformation($"温度异常预警推送成功，新增了{addFaults.Count}条预警");

                //    for (int i = 0; i < addFaults.Count; i++)
                //    {
                //        addFaults[i].SendRepId = faultReq.result_data.new_faults[i];
                //    }
                //}
                var addnum = _db.Insertable(addIndicators).ExecuteCommand();
                _logger.LogInformation($"温度异常预警同步完成，新增了{addnum}条预警");
            }
        }

        /// <summary>
        /// 8.4 输出三相电流传感器故障预警
        /// </summary>
        /// <returns></returns>
        private async Task GetSxdlcgqFault()
        {
            //EquipmentFault xfWarn1, sfWarn1, sfWarn2, hfWarn1;
            var addFaults = new List<FaultOrWarn>();
            var addIndicators = new List<Indicators_Item>();
            var nowData = _db.Queryable<TB_PARSING_NOWDATAS>().ToList().GroupBy(x => x.TrainId);
            var faultData = _db.Queryable<FaultOrWarn>().ToList();
            //获取线路名称
            //var XL = config.Where(x => x.concode == _lineCode).First().conval;
            var newdata = await GetNewData(1);

            foreach (var item in nowData)
            {
                if (item.First().U_DC_Link_Inv > 1000 && item.First().U_DC_Link_Inv <= 1800
                    && item.First().InConv_DIGIN == 0
                    && item.First().BC_OpMode == 0 && item.First().Inv_OpMode == 0 && item.First().Inconv_OpMode == 0
                    )
                {
                    var add = AddOffsetDivisor(item.First().I_L1, item.First().I_L2, item.First().DeviceCode, 9);
                    addIndicators.Add(add);
                }

                if (
                    item.First().U_DC_Link_Inv > 1000 && item.First().U_DC_Link_Inv <= 1800
                    && item.Last().InConv_DIGIN == 0
                    && item.Last().BC_OpMode == 0 && item.Last().Inv_OpMode == 0 && item.Last().Inconv_OpMode == 0
                    )
                {
                    var add = AddOffsetDivisor(item.Last().I_DC_In, item.First().I_DC_In, item.Last().DeviceCode, 7);
                    addIndicators.Add(add);

                }

                if (item.First().InConv_DIGIN == 1)
                {
                    var add = AddU_DC_InAbs(item.First().I_DC_In, item.First().DeviceCode, 8);
                    addIndicators.Add(add);
                }

                if (item.Last().InConv_DIGIN == 1)
                {
                    var add = AddU_DC_InAbs(item.Last().I_DC_In, item.Last().DeviceCode, 8);
                    addIndicators.Add(add);
                }
            }


            if (addFaults.Count > 0)
            {
                //var faultReq = await FaultSetHttpPost(addFaults, new List<FaultOrWarn>());

                //if (faultReq != null && faultReq.result_code == "200")
                //{
                //    _logger.LogInformation($"温度异常预警推送成功，新增了{addFaults.Count}条预警");

                //    for (int i = 0; i < addFaults.Count; i++)
                //    {
                //        addFaults[i].SendRepId = faultReq.result_data.new_faults[i];
                //    }
                //}
                var addnum = _db.Insertable(addIndicators).ExecuteCommand();
                _logger.LogInformation($"温度异常预警同步完成，新增了{addnum}条预警");
            }
        }

        /// <summary>
        /// 偏置因子计算
        /// </summary>
        /// <param name="u"></param>
        /// <param name="u1"></param>
        /// <param name="deviceCode"></param>
        /// <returns></returns>
        private Indicators_Item AddOffsetDivisor(int u,int u1,string? deviceCode,int id)
        {
            var yz = (u - u1) * 100 / (float)u1;
            return AddIndicators_Item(yz, deviceCode,id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="deviceCode"></param>
        /// <returns></returns>
        private Indicators_Item AddU_DC_InAvg(List<KAFKA_DATA> data, string? deviceCode,int id)
        {
            var max = data.Max(x => x.U_DC_In);
            var min = data.Min(x => x.U_DC_In);
            var avg = data.Average(x => x.U_DC_In);
            var yz = (max - min) * 100 / (float)avg;
            return AddIndicators_Item(yz, deviceCode, id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="deviceCode"></param>
        /// <returns></returns>
        private Indicators_Item AddU_DC_Link_InvAvg(List<KAFKA_DATA> data, string? deviceCode, int id)
        {
            var max = data.Max(x => x.U_DC_Link_Inv);
            var min = data.Min(x => x.U_DC_Link_Inv);
            var avg = data.Average(x => x.U_DC_Link_Inv);
            var yz = (max - min) * 100 / (float)avg;
            return AddIndicators_Item(yz, deviceCode, id);
        }

        /// <summary>
        /// 零漂计算
        /// </summary>
        /// <param name="u"></param>
        /// <param name="deviceCode"></param>
        /// <returns></returns>
        private Indicators_Item AddU_DC_InAbs(int u, string? deviceCode,int id)
        {
            var yz = Math.Abs(u - 0);
            return AddIndicators_Item(yz, deviceCode, id);
        }

        private Indicators_Item AddIndicators_Item(float yz, string? deviceCode,int indicatorsId)
        {
            return new Indicators_Item()
            {
                DeviceCode = deviceCode,
                IndicatorsId = indicatorsId,
                Value = yz,
                CreateTime = DateTime.Now
            };
        }

        ///// <summary>
        ///// 设备运行电流异常预警模型
        ///// </summary>
        ///// <returns></returns>
        //private async Task GetSbdlzFault()
        //{
        //    EquipmentFault Warn1, Warn2, LnWarn1, LnWarn2, ysjWarn1, ysjWarn2;
        //    var addFaults = new List<FaultOrWarn>();

        //    var config = _db.Queryable<SYS_Config>().ToList();
        //    var nowData = _db.Queryable<TB_PARSING_NEWDATAS>().ToList();
        //    var equipments = await _db.Queryable<EquipmentFault>().ToListAsync();
        //    var faultData = _db.Queryable<FaultOrWarn>().ToList();
        //    //获取线路名称
        //    var XL = config.Where(x => x.concode == _lineCode).First().conval;

        //    var newData = await GetNewData(null);
        //    if (newData.Count == 0) return;

        //    foreach (var item in nowData)
        //    {
        //        if (item.yxtzjid == 1)
        //        {
        //            Warn1 = equipments.First(x => x.FaultCode == "hvac1014090001");
        //            Warn2 = equipments.First(x => x.FaultCode == "hvac1014090002");

        //            LnWarn1 = equipments.First(x => x.FaultCode == "hvac1014100001");
        //            LnWarn2 = equipments.First(x => x.FaultCode == "hvac1014100002");

        //            ysjWarn1 = equipments.First(x => x.FaultCode == "hvac1014110001");
        //            ysjWarn2 = equipments.First(x => x.FaultCode == "hvac1014110002");
        //        }
        //        else
        //        {
        //            Warn1 = equipments.First(x => x.FaultCode == "hvac1014090003");
        //            Warn2 = equipments.First(x => x.FaultCode == "hvac1014090004");

        //            LnWarn1 = equipments.First(x => x.FaultCode == "hvac1014100003");
        //            LnWarn2 = equipments.First(x => x.FaultCode == "hvac1014100004");

        //            ysjWarn1 = equipments.First(x => x.FaultCode == "hvac1014110003");
        //            ysjWarn2 = equipments.First(x => x.FaultCode == "hvac1014110004");
        //        }

        //        var data = newData.Where(x => x.device_code == item.device_code);

        //        var tfj1 = data.Any(x => x.jz1tfj1uxdlz < 1 || x.jz1tfj1vxdlz < 1 || x.jz1tfj1wxdlz < 1);
        //        var tfj2 = data.Any(x => x.jz1tfj2uxdlz < 1 || x.jz1tfj2vxdlz < 1 || x.jz1tfj2wxdlz < 1);

        //        var lnfj1 = data.Any(x => x.jz1lnfj1uxdlz < 1.2 || x.jz1lnfj1vxdlz < 1.2 || x.jz1lnfj1wxdlz < 1.2);
        //        var lnfj2 = data.Any(x => x.jz1lnfj2uxdlz < 1.2 || x.jz1lnfj2vxdlz < 1.2 || x.jz1lnfj2wxdlz < 1.2);

        //        var ysj1 = data.Any(x => x.jz1ysj1uxdlz < 4 || x.jz1ysj1vxdlz < 4 || x.jz1ysj1wxdlz < 4 || x.jz1ysj1uxdlz > 25 || x.jz1ysj1vxdlz > 25 || x.jz1ysj1wxdlz > 25);
        //        var ysj2 = data.Any(x => x.jz1ysj2uxdlz < 4 || x.jz1ysj2vxdlz < 4 || x.jz1ysj2wxdlz < 4 || x.jz1ysj2uxdlz > 25 || x.jz1ysj2vxdlz > 25 || x.jz1ysj2wxdlz > 25);

        //        var tfj1F = faultData.Any(x => x.Code == Warn1.FaultCode && x.State == "1" && x.DeviceCode == item.device_code);
        //        var tfj2F = faultData.Any(x => x.Code == Warn2.FaultCode && x.State == "1" && x.DeviceCode == item.device_code);

        //        var lnfj1F = faultData.Any(x => x.Code == LnWarn1.FaultCode && x.State == "1" && x.DeviceCode == item.device_code);
        //        var lnfj2F = faultData.Any(x => x.Code == LnWarn2.FaultCode && x.State == "1" && x.DeviceCode == item.device_code);

        //        var ysj1F = faultData.Any(x => x.Code == ysjWarn1.FaultCode && x.State == "1" && x.DeviceCode == item.device_code);
        //        var ysj2F = faultData.Any(x => x.Code == ysjWarn2.FaultCode && x.State == "1" && x.DeviceCode == item.device_code);

        //        var addfault = new FaultOrWarn
        //        {
        //            xlh = XL,
        //            lch = item.lch,
        //            cxh = item.cxh,
        //            DeviceCode = item.device_code,
        //            Type = "2",
        //            State = "1",
        //            createtime = item.update_time
        //        };

        //        if (item.jz1tfj1zt == 1 && !tfj1F && tfj1)
        //        {
        //            addfault.Code = Warn1.FaultCode;
        //            addfault.Name = Warn1.FaultName;
        //            addfault.FaultType = Warn1.Type;                      
        //            addFaults.Add(addfault);
        //        }

        //        if (item.jz1tfj2zt == 1 && !tfj2F && tfj2)
        //        {
        //            addfault.Code = Warn2.FaultCode;
        //            addfault.Name = Warn2.FaultName;
        //            addfault.FaultType = Warn2.Type;
        //            addFaults.Add(addfault);
        //        }

        //        if (item.jz1lnfj1zt == 1 && !lnfj1F && lnfj1)
        //        {
        //            addfault.Code = LnWarn1.FaultCode;
        //            addfault.Name = LnWarn1.FaultName;
        //            addfault.FaultType = LnWarn1.Type;
        //            addFaults.Add(addfault);
        //        }

        //        if (item.jz1lnfj2zt == 1 && !lnfj2F && lnfj2)
        //        {
        //            addfault.Code = LnWarn2.FaultCode;
        //            addfault.Name = LnWarn2.FaultName;
        //            addfault.FaultType = LnWarn2.Type;
        //            addFaults.Add(addfault);
        //        }

        //        if (item.jz1ysj1zt == 1 && !ysj1F && ysj1)
        //        {
        //            addfault.Code = ysjWarn1.FaultCode;
        //            addfault.Name = ysjWarn1.FaultName;
        //            addfault.FaultType = ysjWarn1.Type;
        //            addFaults.Add(addfault);
        //        }

        //        if (item.jz1ysjj2zt == 1 && !ysj2F && ysj2)
        //        {
        //            addfault.Code = ysjWarn2.FaultCode;
        //            addfault.Name = ysjWarn2.FaultName;
        //            addfault.FaultType = ysjWarn2.Type;
        //            addFaults.Add(addfault);
        //        }
        //    }

        //    if (addFaults.Count > 0)
        //    {
        //        var faultReq = await FaultSetHttpPost(addFaults, new List<FaultOrWarn>());

        //        if (faultReq != null && faultReq.result_code == "200")
        //        {
        //            _logger.LogInformation($"设备运行电流异常预警推送成功，新增了{addFaults.Count}条预警");

        //            for (int i = 0; i < addFaults.Count; i++)
        //            {
        //                addFaults[i].SendRepId = faultReq.result_data.new_faults[i];
        //            }
        //        }
        //        var addnum = _db.Insertable(addFaults).ExecuteCommand();
        //        _logger.LogInformation($"设备运行电流异常预警同步完成，新增了{addnum}条预警");
        //    }
        //}

        //#region 寿命预警模型

        ///// <summary>
        ///// 寿命异常预警模型
        ///// </summary>
        ///// <param name="data"></param>
        ///// <param name="equipments"></param>
        ///// <param name="XL"></param>
        ///// <returns></returns>
        //private (FaultOrWarn?, FaultOrWarn?) GetLifeFault(PartsLife data, List<EquipmentFault> equipments, string XL)
        //{
        //    EquipmentFault equipment;
        //    FaultOrWarn? faultOrWarn = null, upWarn = null, isAny1;

        //    string device_code = data.CX + "_" + data.WZ;

        //    equipment = equipments.First(x => x.FaultCode == data.FaultCode);

        //    var faultOrWarns = _db.Queryable<FaultOrWarn>()
        //                        .Where(x => x.DeviceCode == device_code && x.Code == equipment.FaultCode)
        //                        .ToList();

        //    isAny1 = faultOrWarns.FirstOrDefault(x => x.Code == equipment.FaultCode);

        //    //addAny1 = faults.FirstOrDefault(x => x.DeviceCode == device_code && x.Code == equipment.FaultCode);
        //    //addAny2 = faults.FirstOrDefault(x => x.DeviceCode == device_code && x.Code == Warn2.FaultCode);

        //    if (data.RunLife > data.RatedLife)
        //    {
        //        if (isAny1 == null)
        //        {
        //            //创建 faultOrWarn 对象并设置属性
        //            faultOrWarn = new FaultOrWarn
        //            {
        //                xlh = XL,
        //                lch = data.CH,
        //                cxh = data.CX,
        //                DeviceCode = device_code,
        //                Code = equipment.FaultCode,
        //                Name = equipment.FaultName,
        //                FaultType = equipment.Type,
        //                Type = "2",
        //                State = "1",
        //                createtime = data.updatetime
        //            };
        //        }
        //    }
        //    else
        //    {
        //        if (isAny1 != null && isAny1.State == "1")
        //        {
        //            isAny1.State = "0";
        //            isAny1.updatetime = data.updatetime;
        //            upWarn = isAny1;
        //        }
        //    }

        //    return (faultOrWarn, upWarn);
        //}

        ///// <summary>
        ///// 车内空气质量预警模型
        ///// </summary>
        ///// <param name="data"></param>
        ///// <param name="equipments"></param>
        ///// <param name="XL"></param>
        ///// <returns></returns>
        //private async Task GetKqzlFault()
        //{
        //    EquipmentFault Warn1;
        //    var addFaults = new List<FaultOrWarn>();

        //    var config = _db.Queryable<SYS_Config>().ToList();
        //    var nowData = _db.Queryable<TB_PARSING_NEWDATAS>().ToList();
        //    var equipments = await _db.Queryable<EquipmentFault>().ToListAsync();
        //    //获取线路名称
        //    var XL = config.Where(x => x.concode == _lineCode).First().conval;

        //    var newData = await GetNewData("15");
        //    if (newData.Count == 0) return;

        //    Warn1 = equipments.First(x => x.FaultCode == "hvac1014130001");

        //    var groupData = nowData.GroupBy(x => x.cxh);

        //    var faultOrWarns = _db.Queryable<FaultOrWarn>().Where(x => x.Code == Warn1.FaultCode && x.State == "1");
        //    foreach (var item in groupData)
        //    {
        //        var isFault = faultOrWarns.Any(x => x.cxh == item.Key);

        //        if (isFault) continue;

        //        var dataList = newData.Where(x => x.cxh == item.Key).ToList();

        //        var isTrue1 = dataList.Any(x => x.kssdz < 80);
        //        var isTrue2 = dataList.Any(x => x.jz1co2nd <= 1500);
        //        var isTrue3 = dataList.Any(x => (x.jz1kswd + x.jz1kswdcgq1wd + x.jz1kqzljcmkwd) / 3 >= 20 && (x.jz1kswd + x.jz1kswdcgq1wd + x.jz1kqzljcmkwd) / 3 <= 28);

        //        if (isTrue1 || isTrue2 || isTrue3)
        //        {
        //            var addFault = new FaultOrWarn
        //            {
        //                xlh = XL,
        //                lch = item.First().lch,
        //                cxh = item.Key,
        //                DeviceCode = item.First().device_code,
        //                Code = Warn1.FaultCode,
        //                Name = Warn1.FaultName,
        //                FaultType = Warn1.Type,
        //                Type = "2",
        //                State = "1",
        //                createtime = item.First().create_time
        //            };
        //            addFaults.Add(addFault);
        //        }
        //    }

        //    if (addFaults.Count > 0)
        //    {
        //        var faultReq = await FaultSetHttpPost(addFaults, new List<FaultOrWarn>());

        //        if (faultReq != null && faultReq.result_code == "200")
        //        {
        //            _logger.LogInformation($"车内空气质量预警推送成功，新增了{addFaults.Count}条预警");

        //            for (int i = 0; i < addFaults.Count; i++)
        //            {
        //                addFaults[i].SendRepId = faultReq.result_data.new_faults[i];
        //            }
        //        }
        //        var addnum = _db.Insertable(addFaults).ExecuteCommand();
        //        _logger.LogInformation($"车内空气质量预警同步完成，新增了{addnum}条预警");
        //    }

        //}


        ///// <summary>
        ///// 制冷目标温度异常预警模型
        ///// </summary>
        ///// <returns></returns>
        //private async Task GetZlmbwdFault()
        //{
        //    try
        //    {
        //        EquipmentFault Warn1;
        //        var addFaults = new List<FaultOrWarn>();

        //        var config = _db.Queryable<SYS_Config>().ToList();
        //        var nowData = _db.Queryable<TB_PARSING_NEWDATAS>().ToList();
        //        var equipments = await _db.Queryable<EquipmentFault>().ToListAsync();
        //        //获取线路名称
        //        var XL = config.Where(x => x.concode == _lineCode).First().conval;

        //        var newData = await GetNewData("31");

        //        if (newData.Count == 0) return;

        //        var ysjzt = newData.Any(x => x.jz1ysj1zt == 0 && x.jz1ysjj2zt == 0);

        //        if (ysjzt) return;

        //        var time = DateTime.Now.AddMinutes(-1);

        //        var cxhGroup = nowData.GroupBy(x => x.cxh).ToList();

        //        Warn1 = equipments.First(x => x.FaultCode == "hvac1014010001");

        //        var faultData = _db.Queryable<FaultOrWarn>().Where(x => x.State == "1" && x.Code == Warn1.FaultCode);

        //        foreach (var item in cxhGroup)
        //        {                   
        //            var isAny1 = faultData.Any(x => x.cxh == item.Key);

        //            var ysjdata = newData.Where(x => x.cxh == item.Key && x.create_time >= time);

        //            var isTrue1 = ysjdata.Any(x => (((x.jz1kswd + x.jz1kswdcgq1wd) / 2 - 2.5) - x.jz1mbwd) > 4);

        //            if (isAny1 == null && isTrue1)
        //            {
        //                var addFault = new FaultOrWarn
        //                {
        //                    xlh = XL,
        //                    lch = item.First().lch,
        //                    cxh = item.Key,
        //                    //DeviceCode = item.device_code,
        //                    Code = Warn1.FaultCode,
        //                    Name = Warn1.FaultName,
        //                    FaultType = Warn1.Type,
        //                    Type = "2",
        //                    State = "1",
        //                    createtime = item.First().create_time
        //                };
        //                addFaults.Add(addFault);
        //            }
        //        }

        //        if (addFaults.Count > 0)
        //        {
        //            var faultReq = await FaultSetHttpPost(addFaults, new List<FaultOrWarn>());

        //            if (faultReq != null && faultReq.result_code == "200")
        //            {
        //                Console.WriteLine($"制冷目标温度异常预警推送成功，新增了{addFaults.Count}条预警");

        //                for (int i = 0; i < addFaults.Count; i++)
        //                {
        //                    addFaults[i].SendRepId = faultReq.result_data.new_faults[i];
        //                }
        //            }

        //            var addnum = _db.Insertable(addFaults).ExecuteCommand();
        //            _logger.LogInformation($"制冷目标温度异常预警同步完成，新增了{addnum}条预警");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"制冷目标温度异常预警失败：{ex.ToString()}");
        //    }
        //}

        ///// <summary>
        ///// 制冷剂泄露预警模型
        ///// </summary>
        ///// <returns></returns>
        //private async Task GetZljxlFault()
        //{
        //    try
        //    {
        //        EquipmentFault Warn1, Warn2;
        //        var addFaults = new List<FaultOrWarn>();

        //        var config = _db.Queryable<SYS_Config>().ToList();
        //        var nowData = _db.Queryable<TB_PARSING_NEWDATAS>().ToList();
        //        var equipments = await _db.Queryable<EquipmentFault>().ToListAsync();
        //        //获取线路名称
        //        var XL = config.Where(x => x.concode == _lineCode).First().conval;

        //        var newData = await GetNewData(null);

        //        if (newData.Count == 0) return;

        //        foreach (var item in nowData)
        //        {

        //            if (item.yxtzjid == 1)
        //            {
        //                Warn1 = equipments.First(x => x.FaultCode == "hvac1014050001");
        //                Warn2 = equipments.First(x => x.FaultCode == "hvac1014050002");
        //            }
        //            else
        //            {
        //                Warn1 = equipments.First(x => x.FaultCode == "hvac1014050003");
        //                Warn2 = equipments.First(x => x.FaultCode == "hvac1014050004");
        //            }

        //            var faultOrWarns = _db.Queryable<FaultOrWarn>()
        //                            .Where(x => x.DeviceCode == item.device_code && (x.Code == Warn1.FaultCode || x.Code == Warn2.FaultCode))
        //                            .ToList();

        //            var isAny1 = faultOrWarns.FirstOrDefault(x => x.Code == Warn1.FaultCode);
        //            var isAny2 = faultOrWarns.FirstOrDefault(x => x.Code == Warn2.FaultCode);

        //            var ysj1data = newData.Where(x => x.device_code == item.device_code && x.jz1ysj1zt == 0);
        //            var ysj2data = newData.Where(x => x.device_code == item.device_code && x.jz1ysjj2zt == 0);

        //            var isTrue1 = ysj1data.Any(x => x.jz1ysj1gyyl < 600 || x.jz1ysj1dyyl < 600);
        //            var isTrue2 = ysj1data.Any(x => x.jz1ysj2gyyl < 600 || x.jz1ysj2dyyl < 600);

        //            if (isAny1 == null && isTrue1)
        //            {
        //                var addFault = new FaultOrWarn
        //                {
        //                    xlh = XL,
        //                    lch = item.lch,
        //                    cxh = item.cxh,
        //                    DeviceCode = item.device_code,
        //                    Code = Warn1.FaultCode,
        //                    Name = Warn1.FaultName,
        //                    FaultType = Warn1.Type,
        //                    Type = "2",
        //                    State = "1",
        //                    createtime = item.create_time
        //                };
        //                addFaults.Add(addFault);
        //            }
        //            if (isAny2 == null && isTrue2)
        //            {
        //                var addFault = new FaultOrWarn
        //                {
        //                    xlh = XL,
        //                    lch = item.lch,
        //                    cxh = item.cxh,
        //                    DeviceCode = item.device_code,
        //                    Code = Warn2.FaultCode,
        //                    Name = Warn2.FaultName,
        //                    FaultType = Warn2.Type,
        //                    Type = "2",
        //                    State = "1",
        //                    createtime = item.create_time
        //                };
        //                addFaults.Add(addFault);
        //            }
        //        }

        //        var faultReq = await FaultSetHttpPost(addFaults, new List<FaultOrWarn>());

        //        if (faultReq != null && faultReq.result_code == "200")
        //        {
        //            _logger.LogInformation($"制冷剂泄露预警推送成功，新增了{addFaults.Count}条预警");

        //            for (int i = 0; i < addFaults.Count; i++)
        //            {
        //                addFaults[i].SendRepId = faultReq.result_data.new_faults[i];
        //            }
        //        }

        //        if (addFaults.Count > 0)
        //        {
        //            var addnum = _db.Insertable(addFaults).ExecuteCommand();
        //            _logger.LogInformation($"制冷剂泄露预警同步完成，新增了{addnum}条预警");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"制冷剂泄露预警失败：{ex.ToString()}");
        //    }
        //}

        ///// <summary>
        ///// 滤网脏堵预警模型
        ///// </summary>
        ///// <returns></returns>
        //private async Task GetLwzdFault()
        //{
        //    EquipmentFault Warn1;
        //    var addFaults = new List<FaultOrWarn>();
        //    var setFaults = new List<FaultOrWarn>();

        //    var config = _db.Queryable<SYS_Config>().ToList();
        //    var nowData = _db.Queryable<TB_PARSING_NEWDATAS>().Where(x => x.tfms == 0).ToList();
        //    var equipments = await _db.Queryable<EquipmentFault>().ToListAsync();
        //    var faultDataQue = _db.Queryable<FaultOrWarn>().Where(x => x.State == "1");
        //    //获取线路名称
        //    var XL = config.Where(x => x.concode == _lineCode).First().conval;

        //    var newData = await GetNewData(null);

        //    if (newData.Count == 0) return;  

        //    foreach (var item in nowData)
        //    {
        //        if (item.yxtzjid == 1)
        //        {
        //            Warn1 = equipments.First(x => x.FaultCode == "hvac1014080001");
        //        }
        //        else
        //        {
        //            Warn1 = equipments.First(x => x.FaultCode == "hvac1014080002");
        //        }

        //        var faultData = faultDataQue.Where(x => x.DeviceCode == item.device_code && x.Code == Warn1.FaultCode);

        //        var isAny1 = faultData.Any(x => x.Type == "2");

        //        var startOfDay = new DateTime(item.update_time.Year, item.update_time.Month, item.update_time.Day, 0, 0, 0);
        //        var isAny2 = faultData.Any(x => x.Type == "4" && x.createtime > startOfDay);

        //        if (isAny1 || isAny2) continue;

        //        var time = item.create_time.AddMinutes(-3);
        //        var time1 = item.create_time.AddMinutes(-1);
        //        var dataList = newData.Where(x => x.device_code == item.device_code && x.create_time >= time);

        //        var isTrue1 = !dataList.Any(x => x.jz1tfj1zt == 0);
        //        var isTrue2 = !dataList.Any(x => x.jz1tfj2zt == 0);

        //        var data1minute = dataList.Where(x => x.create_time >= time1);
        //        var isFault1 = data1minute.Any(x => x.jz1lwylz >= 100);
        //        var isFault2 = data1minute.Any(x => x.jz1lwylz >= 150);

        //        if (isFault1)
        //        {
        //            var addFault = new FaultOrWarn
        //            {
        //                xlh = XL,
        //                lch = item.lch,
        //                cxh = item.cxh,
        //                DeviceCode = item.device_code,
        //                Code = Warn1.FaultCode,
        //                Name = Warn1.FaultName,
        //                FaultType = Warn1.Type,
        //                Type = "4",
        //                State = "1",
        //                createtime = item.create_time
        //            };
        //            addFaults.Add(addFault);

        //            var count = faultData.Where(x => x.Type == "4").Count();

        //            if (count >= 9)
        //            {
        //                addFault.Type = "2";
        //                addFaults.Add(addFault);
        //                setFaults.Add(addFault);
        //            }
        //        }

        //        if (isFault2)
        //        {
        //            var addFault = new FaultOrWarn
        //            {
        //                xlh = XL,
        //                lch = item.lch,
        //                cxh = item.cxh,
        //                DeviceCode = item.device_code,
        //                Code = Warn1.FaultCode,
        //                Name = Warn1.FaultName,
        //                FaultType = Warn1.Type,
        //                Type = "5",
        //                State = "1",
        //                createtime = item.create_time
        //            };
        //            addFaults.Add(addFault);

        //            var count = faultData.Where(x => x.Type == "5").Count();

        //            if (count >= 29)
        //            {
        //                addFault.Type = "3";
        //                addFault.Name = Warn1.FaultName[..^2] + "报警";
        //                addFaults.Add(addFault);
        //                setFaults.Add(addFault);
        //            }
        //        }                             
        //    }

        //    var faultReq = await FaultSetHttpPost(setFaults, new List<FaultOrWarn>());

        //    if (faultReq != null && faultReq.result_code == "200")
        //    {
        //        _logger.LogInformation($"冷凝进出风短路预警推送成功，新增了{setFaults.Count}条预警");

        //        for (int i = 0; i < addFaults.Count; i++)
        //        {
        //            addFaults[i].SendRepId = faultReq.result_data.new_faults[i];
        //        }
        //    }

        //    if (addFaults.Count > 0)
        //    {
        //        var addnum = _db.Insertable(addFaults).ExecuteCommand();
        //        _logger.LogInformation($"冷凝进出风短路预警同步完成，新增了{addnum}条预警");
        //    }
        //}


        ///// <summary>
        ///// 蒸发器脏堵预警模型
        ///// </summary>
        ///// <returns></returns>
        //private async Task GetZfqzdFault()
        //{
        //    EquipmentFault Warn1;
        //    var addFaults = new List<FaultOrWarn>();
        //    var setFaults = new List<FaultOrWarn>();

        //    var config = _db.Queryable<SYS_Config>().ToList();
        //    var nowData = _db.Queryable<TB_PARSING_NEWDATAS>().ToList();
        //    var equipments = await _db.Queryable<EquipmentFault>().ToListAsync();
        //    //获取线路名称
        //    var XL = config.Where(x => x.concode == _lineCode).First().conval;

        //    var newData = await GetNewData(null);

        //    if (newData.Count == 0) return;

        //    foreach (var item in nowData)
        //    {
        //        var isFault = item.tfms == 0 && item.jz1xff1kd == 100 && item.jz1hff1kd == 100 && item.jz1xff2kd == 100 && item.jz1hff2kd == 100 && item.jz1lwylz <= 40;
        //        if (!isFault) continue;

        //        if (item.yxtzjid == 1)
        //        {
        //            Warn1 = equipments.First(x => x.FaultCode == "hvac1014170001");
        //        }
        //        else
        //        {
        //            Warn1 = equipments.First(x => x.FaultCode == "hvac1014170002");
        //        }

        //        var dataList = newData.Where(x => x.device_code == item.device_code).OrderBy(x => x.create_time).ToList();

        //        var startOfDay = new DateTime(item.update_time.Year, item.update_time.Month, item.update_time.Day, 0, 0, 0);

        //        var faultData = _db.Queryable<FaultOrWarn>().Where(x => x.DeviceCode == item.device_code && x.Code == Warn1.FaultCode && x.State == "1");

        //        var isAny1 = faultData.Any(x => x.Type == "2");
        //        var isAny2 = faultData.Any(x => x.Type == "4" && x.createtime > startOfDay);

        //        if (isAny1 || isAny2) continue;

        //        var time = item.update_time.AddMinutes(-3);
        //        var dataListTimeQ = dataList.Where(x => x.create_time >= time && x.create_time < item.update_time);

        //        var time1 = item.update_time.AddMinutes(-1);
        //        var minutes1Data = dataList.Where(x => x.create_time >= time1 && x.create_time < item.update_time).OrderBy(x => x.create_time);

        //        var AP1 = minutes1Data.Average(x => (x.jz1tfj1uxdlz + x.jz1tfj1vxdlz + x.jz1tfj1wxdlz) / 3);
        //        var AP2 = minutes1Data.Average(x => (x.jz1tfj2uxdlz + x.jz1tfj2vxdlz + x.jz1tfj2wxdlz) / 3);


        //        var tfj1 = AP1 >= 0.9 * 1.1 && AP1 <= 1.1;
        //        var tfj2 = AP2 >= 0.9 * 1.1 && AP2 <= 1.1;

        //        var isTrue1 = !dataListTimeQ.Any(x => x.jz1tfj1zt == 0) && tfj1;
        //        var isTrue2 = !dataListTimeQ.Any(x => x.jz1tfj2zt == 0) && tfj2;

        //        if (isTrue1 || isTrue2)
        //        {
        //            var addFault = new FaultOrWarn
        //            {
        //                xlh = XL,
        //                lch = item.lch,
        //                cxh = item.cxh,
        //                DeviceCode = item.device_code,
        //                Code = Warn1.FaultCode,
        //                Name = Warn1.FaultName,
        //                FaultType = Warn1.Type,
        //                Type = "3",
        //                State = "1",
        //                createtime = item.create_time
        //            };
        //            addFaults.Add(addFault);

        //            var count = faultData.Where(x => x.Type == "3").Count();

        //            if (count >= 14)
        //            {
        //                addFault.Type = "2";
        //                addFaults.Add(addFault);
        //                setFaults.Add(addFault);
        //            }
        //        }            
        //    }

        //    var faultReq = await FaultSetHttpPost(setFaults, new List<FaultOrWarn>());

        //    if (faultReq != null && faultReq.result_code == "200")
        //    {
        //        _logger.LogInformation($"冷凝进出风短路预警推送成功，新增了{setFaults.Count}条预警");

        //        for (int i = 0; i < addFaults.Count; i++)
        //        {
        //            addFaults[i].SendRepId = faultReq.result_data.new_faults[i];
        //        }
        //    }

        //    if (addFaults.Count > 0)
        //    {
        //        var addnum = _db.Insertable(addFaults).ExecuteCommand();


        //        _logger.LogInformation($"冷凝进出风短路预警同步完成，新增了{addnum}条预警");
        //    }
        //}

        ///// <summary>
        ///// 冷凝器脏堵预警模型
        ///// </summary>
        ///// <returns></returns>
        //private async Task GetLnqzdFault()
        //{
        //    EquipmentFault Warn1;
        //    var addFaults = new List<FaultOrWarn>();
        //    var setFaults = new List<FaultOrWarn>();

        //    var config = _db.Queryable<SYS_Config>().ToList();
        //    var nowData = _db.Queryable<TB_PARSING_NEWDATAS>().ToList();
        //    var equipments = await _db.Queryable<EquipmentFault>().ToListAsync();
        //    var faultOrWarn = _db.Queryable<FaultOrWarn>().ToList();
        //    //获取线路名称
        //    var XL = config.Where(x => x.concode == _lineCode).First().conval;

        //    var newData = await GetNewData(null);
        //    if (newData.Count == 0) return;

        //    foreach (var item in nowData)
        //    {

        //        if (!new[] { 2, 3, 4, 5 }.Contains(item.tfms))
        //        {
        //            continue;
        //        }

        //        if (item.yxtzjid == 1)
        //        {
        //            Warn1 = equipments.First(x => x.FaultCode == "hvac1014180001");
        //        }
        //        else
        //        {
        //            Warn1 = equipments.First(x => x.FaultCode == "hvac1014180002");
        //        }

        //        var faultData = faultOrWarn.Where(x => x.DeviceCode == item.device_code && x.Code == Warn1.FaultCode && x.State == "1");

        //        var startOfDay = new DateTime(item.update_time.Year, item.update_time.Month, item.update_time.Day, 0, 0, 0);

        //        var dataList = newData.Where(x => x.device_code == item.device_code).ToList();
        //        var isAny1 = faultData.Any(x => x.Type == "2");
        //        var isAny2 = faultData.Any(x => x.Type == "4" && x.createtime > startOfDay);

        //        if (isAny1 || isAny2) continue;

        //        var time = item.update_time.AddMinutes(-3);
        //        var dataListTimeQ = dataList.Where(x => x.create_time >= time && x.create_time < item.update_time);

        //        var time1 = item.update_time.AddMinutes(-1);
        //        var minutes1Data = dataList.Where(x => x.create_time >= time1 && x.create_time < item.update_time).OrderBy(x => x.create_time);

        //        var ysj1pl = minutes1Data.Any(x => x.jz1ysj1pl != 50);
        //        var ysj2pl = minutes1Data.Any(x => x.jz1ysj2pl != 50);

        //        var P50 = 0.35565 + 0.05022 * item.jz1swwd;

        //        var ysj1P = (item.jz1ysj1gyyl / 1000) > (P50 * 1.1);
        //        var ysj2P = (item.jz1ysj2gyyl / 1000) > (P50 * 1.1);

        //        var isTrue1 = !dataListTimeQ.Any(x => x.jz1ysj1zt == 0) && ysj1P;
        //        var isTrue2 = !dataListTimeQ.Any(x => x.jz1ysjj2zt == 0) && ysj2P;

        //        if (isTrue1 || isTrue2)
        //        {
        //            var addFault = new FaultOrWarn
        //            {
        //                xlh = XL,
        //                lch = item.lch,
        //                cxh = item.cxh,
        //                DeviceCode = item.device_code,
        //                Code = Warn1.FaultCode,
        //                Name = Warn1.FaultName,
        //                FaultType = Warn1.Type,
        //                Type = "4",
        //                State = "1",
        //                createtime = item.update_time
        //            };
        //            addFaults.Add(addFault);

        //            var count = faultData.Where(x => x.Type == "4").Count();

        //            if (count >= 9)
        //            {
        //                addFault.Type = "2";
        //                addFaults.Add(addFault);
        //                setFaults.Add(addFault);
        //            }

        //            if (count >= 14)
        //            {
        //                addFault.Type = "3";
        //                addFault.Name = Warn1.FaultName[..^2] + "报警";
        //                addFaults.Add(addFault);
        //                setFaults.Add(addFault);
        //            }
        //        }   
        //    }

        //    var faultReq = await FaultSetHttpPost(setFaults, new List<FaultOrWarn>());

        //    if (faultReq != null && faultReq.result_code == "200")
        //    {
        //        _logger.LogInformation($"冷凝进出风短路预警推送成功，新增了{setFaults.Count}条预警");

        //        for (int i = 0; i < addFaults.Count; i++)
        //        {
        //            addFaults[i].SendRepId = faultReq.result_data.new_faults[i];
        //        }
        //    }

        //    if (addFaults.Count > 0)
        //    {
        //        var addnum = _db.Insertable(addFaults).ExecuteCommand();


        //        _logger.LogInformation($"冷凝进出风短路预警同步完成，新增了{addnum}条预警");
        //    }
        //}

        ///// <summary>
        ///// 冷凝进出风短路预警模型
        ///// </summary>
        ///// <returns></returns>
        //private async Task GetLnjcfdlFault()
        //{
        //    EquipmentFault Warn1;
        //    var addFaults = new List<FaultOrWarn>();

        //    var config = _db.Queryable<SYS_Config>().ToList();
        //    var nowData = _db.Queryable<TB_PARSING_NEWDATAS>().ToList();
        //    var equipments = await _db.Queryable<EquipmentFault>().ToListAsync();
        //    //获取线路名称
        //    var XL = config.Where(x => x.concode == _lineCode).First().conval;

        //    var newData = await GetNewData(null);
        //    if (newData.Count == 0) return;

        //    foreach (var item in nowData)
        //    {

        //        if (!new[] { 2, 3, 4, 5 }.Contains(item.tfms)) continue;

        //        if (item.yxtzjid == 1)
        //        {
        //            Warn1 = equipments.First(x => x.FaultCode == "hvac1014190001");
        //        }
        //        else
        //        {
        //            Warn1 = equipments.First(x => x.FaultCode == "hvac1014190002");
        //        }

        //        var dataList = newData.Where(x => x.device_code == item.device_code).ToList();
        //        var isAny1 = _db.Queryable<FaultOrWarn>().First(x => x.DeviceCode == item.device_code && x.Code == Warn1.FaultCode && x.State == "1");

        //        var time = item.update_time.AddMinutes(-3);
        //        var dataListTimeQ = dataList.Where(x => x.create_time >= time && x.create_time < item.update_time);

        //        var time1 = item.update_time.AddMinutes(-1);
        //        var minutes1Data = dataList.Where(x => x.create_time >= time1 && x.create_time < item.update_time).OrderBy(x => x.create_time);

        //        var ysj1pl = minutes1Data.Any(x => x.jz1ysj1pl != minutes1Data.First().jz1ysj1pl);
        //        var ysj2pl = minutes1Data.Any(x => x.jz1ysj2pl != minutes1Data.First().jz1ysj2pl);

        //        var ylc = minutes1Data.Last().jz1ysj1gyyl - minutes1Data.First().jz1ysj1gyyl;
        //        var ylc2 = minutes1Data.Last().jz1ysj2gyyl - minutes1Data.First().jz1ysj2gyyl;

        //        var isTrue1 = !dataListTimeQ.Any(x => x.jz1ysj1zt == 0) && !ysj1pl && ylc > 200;
        //        var isTrue2 = !dataListTimeQ.Any(x => x.jz1ysjj2zt == 0) && !ysj2pl && ylc2 > 200;

        //        if (isTrue1 || isTrue2)
        //        {
        //            var addFault = new FaultOrWarn
        //            {
        //                xlh = XL,
        //                lch = item.lch,
        //                cxh = item.cxh,
        //                DeviceCode = item.device_code,
        //                Code = Warn1.FaultCode,
        //                Name = Warn1.FaultName,
        //                FaultType = Warn1.Type,
        //                Type = "2",
        //                State = "1",
        //                createtime = item.update_time
        //            };
        //            addFaults.Add(addFault);
        //        }           
        //    }

        //    var faultReq = await FaultSetHttpPost(addFaults, new List<FaultOrWarn>());

        //    if (faultReq != null && faultReq.result_code == "200")
        //    {
        //        _logger.LogInformation($"冷凝进出风短路预警推送成功，新增了{addFaults.Count}条预警");

        //        for (int i = 0; i < addFaults.Count; i++)
        //        {
        //            addFaults[i].SendRepId = faultReq.result_data.new_faults[i];
        //        }
        //    }

        //    if (addFaults.Count > 0)
        //    {
        //        var addnum = _db.Insertable(addFaults).ExecuteCommand();
        //        _logger.LogInformation($"冷凝进出风短路预警同步完成，新增了{addnum}条预警");
        //    }
        //}

        //#endregion

        /// <summary>
        /// 获取实时数据
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private async Task<List<KAFKA_DATA>> GetNewData(int? time)
        {
            //var config = _db.Queryable<SYS_Config>().ToList();
            //获取故障时间
            int sj = 0;
            //config.Where(x => x.concode == "WarnSj").First()?.conval;

            string sql = @"SELECT * FROM dbo.TB_PARSING_DATAS" +
                $"_{DateTime.Now:yyyyMMdd} " +
                         @"WHERE CreateTime >= DATEADD(MINUTE,-{0},GETDATE())";

            var faults_sql = string.Format(sql, time);
            //获取实时故障数据
            var faults = await _db.SqlQueryable<KAFKA_DATA>(faults_sql).ToListAsync();

            return faults;
        }

        ///// <summary>
        ///// 预警初始化
        ///// </summary>
        ///// <param name="dev"></param>
        ///// <param name="fault"></param>
        ///// <returns></returns>
        //private FaultOrWarn GetFAULTWARN(TB_PARSING_DATAS_NEWCS dev, OVERHAULIDEA fault)
        //{


        //    var addFault = new FaultOrWarn
        //    {

        //    };

        //    return addFault;
        //}

        //#endregion

        //#region 物模型上传

        //public async Task XieruKafkaAsync()
        //{
        //    await Console.Out.WriteLineAsync($"物模型上传中......");
        //    try
        //    {

        //        var trainSta = await GetTrainSta();
        //        var onlien = trainSta?.result_data.FirstOrDefault()?.online_trains?.ToList();
        //        var depot = trainSta?.result_data.FirstOrDefault()?.depot_trains?.ToList();
        //        onlien.AddRange(depot);

        //        // 获取实时信息
        //        var dt = _db.Queryable<TB_PARSING_NEWDATAS>().Where(x => onlien.Contains(x.lch)).ToList();

        //        var trains = new TrainData();
        //        // 按列车分组
        //        var groupedByTrain = dt.GroupBy(row => row.lch);
        //        foreach (var trainGroup in groupedByTrain)
        //        {
        //            var trainData = new TrainData
        //            {
        //                lineCode = "GZML7S",
        //                trainCode = trainGroup.Key,
        //                systemCode = "HVAC",
        //                createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        //                data = new Dictionary<string, Car>()
        //            };
        //            // 按车厢分组
        //            var groupedByCar = trainGroup.GroupBy(row => row.cxh);
        //            foreach (var carGroup in groupedByCar)
        //            {
        //                string device_id = carGroup.Key;
        //                var car = new Car(); // 初始化车厢
        //                foreach (var item in carGroup)
        //                {
        //                    var HVAC = new dynamicProperties
        //                    {
        //                        freshairTemp = item.jz1swwd,
        //                        supplyairTemp = item.jz1sfcgq1wd,
        //                        returnairTemp = item.jz1kswd,
        //                        carairTemp = item.jz1kswd,
        //                        airqualitycollectionmoduleTemp = item.jz1kqzljcmkwd,
        //                        airqualitycollectionmoduleRH = item.kssdz,
        //                        airqualitycollectionmoduleCO2 = item.jz1co2nd,
        //                        airqualitycollectionmoduleTVOC = item.jz1tvocnd,
        //                        airqualitycollectionmodulePM = item.jz1pm2d5nd,
        //                        airPressureDifference = 1,
        //                        targetTemp = item.jz1mbwd,
        //                        voltage = 220,
        //                        Maincircuitbreaker = item.jz1zhl1dlqgz,
        //                        compressorcircuitbreaker = item.jz1ysj1dlqgz,
        //                        inverter = item.jz1bpq1gz,
        //                        UVlampmalfunction = item.jz1zwxd1gz,
        //                        returnairdamper = item.jz1hff1gz,
        //                        freshairdamper = item.jz1xff1gz,
        //                        aircleaner = item.jz1kqjhqgz,
        //                        lowpressureswitch = item.jz1dyylcgq1gz,
        //                        highpressureswitch = item.jz1gyylcgq1gz,
        //                        severeFault = item.jz1yzgz,
        //                        midFault = item.jz1zdgz,
        //                        minorFault = item.jz1qwgz
        //                    };

        //                    var COM01 = new CompressordynamicProperties
        //                    {
        //                        exhaustTemp = item.jz1ysj1pqwd,
        //                        suctionTemp = item.jz1ysj1xqwd,
        //                        highPressure = item.jz1ysj1gyyl,
        //                        lowPressure = item.jz1ysj1dyyl,
        //                        compressorId = item.jz1ysj1vxdlz,
        //                        contactorStatus = item.jz1ysj1jcqgz
        //                    };

        //                    var COM02 = new CompressordynamicProperties
        //                    {
        //                        exhaustTemp = item.jz1ysj2pqwd,
        //                        suctionTemp = item.jz1ysj2xqwd,
        //                        highPressure = item.jz1ysj2gyyl,
        //                        lowPressure = item.jz1ysj2dyyl,
        //                        compressorId = item.jz1ysj2vxdlz,
        //                        contactorStatus = item.jz1ysj2jcqgz
        //                    };

        //                    var EVP01 = new VentilationdynamicProperties
        //                    {
        //                        blowerId = item.jz1tfj1uxdlz,
        //                        ventilationcontactorStatus = item.jz1tfj1gzgz,
        //                        emergencyventilationcontactorStatus = item.jz1tfjjjtfjcqgz,
        //                    };

        //                    var EVP02 = new VentilationdynamicProperties
        //                    {
        //                        blowerId = item.jz1tfj2uxdlz,
        //                        ventilationcontactorStatus = item.jz1tfj2gzgz,
        //                        emergencyventilationcontactorStatus = item.jz1tfjjjtfjcqgz,
        //                    };

        //                    var WEX01 = new ExhaustdynamicProperties
        //                    {
        //                        fanId = item.jz1lnfj1uxdlz,
        //                        emergencyventilationcontactorStatus = item.jz1lnfj1gzgz,
        //                    };

        //                    var WEX02 = new ExhaustdynamicProperties
        //                    {
        //                        fanId = item.jz1lnfj2uxdlz,
        //                        emergencyventilationcontactorStatus = item.jz1lnfj2gzgz,
        //                    };

        //                    if (item.yxtzjid == 1)
        //                    {
        //                        car.HVAC01 = new HVACUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = HVAC
        //                        };

        //                        // 填充HVAC01COM02
        //                        car.HVAC01COM01 = new CompressorUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = COM01
        //                        };

        //                        // 填充HVAC02COM01
        //                        car.HVAC01COM02 = new CompressorUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = COM02
        //                        };

        //                        // 填充HVAC01EVP01
        //                        car.HVAC01EVP01 = new VentilationUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = EVP01
        //                        };

        //                        // 填充HVAC01EVP02
        //                        car.HVAC01EVP02 = new VentilationUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = EVP02
        //                        };

        //                        // 填充HVAC01WEX01
        //                        car.HVAC01WEX01 = new ExhaustUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = WEX01
        //                        };

        //                        // 填充HVAC01WEX02
        //                        car.HVAC01WEX02 = new ExhaustUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = WEX02
        //                        };
        //                    }

        //                    if (item.yxtzjid == 2)
        //                    {
        //                        car.HVAC02 = new HVACUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = HVAC
        //                        };

        //                        car.HVAC02COM01 = new CompressorUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = COM01
        //                        };

        //                        car.HVAC02COM02 = new CompressorUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = COM02
        //                        };

        //                        car.HVAC02EVP01 = new VentilationUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = EVP01
        //                        };

        //                        car.HVAC02EVP02 = new VentilationUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = EVP02
        //                        };

        //                        car.HVAC02WEX01 = new ExhaustUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = WEX01
        //                        };

        //                        car.HVAC02WEX02 = new ExhaustUnit
        //                        {
        //                            designProperties = new Dictionary<string, string>(),
        //                            dynamicProperties = WEX02
        //                        };
        //                    }
        //                }

        //                trainData.data.Add(device_id, car);
        //            }
        //            var aa = JsonConvert.SerializeObject(trainData);
        //            var config = new ProducerConfig
        //            {
        //                BootstrapServers = _appSettings.KafkaConfig.bootstrapServers,
        //                ClientId = Dns.GetHostName()// 客户端ID
        //            };
        //            CancellationTokenSource cts = new CancellationTokenSource();
        //            // 获取 CancellationToken
        //            CancellationToken ct = cts.Token;
        //            var producerBuilder = new ProducerBuilder<Null, string>(config);
        //            IProducer<Null, string> producer = producerBuilder.Build();
        //            string topic = "gzml7s-hvacmodel"; // 设置目标主题 gzml7s-hvacmodel   test_0524
        //            var deliveryResult = await producer.ProduceAsync(topic, new Message<Null, string> { Value = aa }, ct);
        //            await Console.Out.WriteLineAsync($"物模型上传成功");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await Console.Out.WriteLineAsync($"物模型上传失败：" + ex.Message);
        //        _logger.LogDebug($"物模型上传失败" + ex.Message);
        //        //  return JsonConvert.SerializeObject(new { error = ex.Message });
        //    }
        //}

        ///// <summary>
        ///// 获取车辆在线信息
        ///// </summary>
        ///// <returns></returns>
        //public async Task<HttpTrainStaDTO?> GetTrainSta()
        //{
        //    string urlType = "line-statistics";
        //    // 构建app_token
        //    string appToken = $"app_id={_appId}&app_key={_appKey}&date=" + DateTime.Now.ToString("yyyy-MM-dd");
        //    string tokenMd5 = Helper.GetMD5String(appToken).ToUpper();
        //    string url = $"{_baseUrl}{urlType}?app_id={_appId}&app_token={tokenMd5}&line_code={_lineCode}";
        //    var trainSta = await HttpClientExample.SendGetRequestAsync<HttpTrainStaDTO>(url);
        //    return trainSta;
        //}

        //#endregion
    }
}





