using AutoMapper;
using DataBase;
using DataBase.Entity;
using Microsoft.Extensions.Hosting;
using SqlSugar;
using SqlSugar.SplitTableExtensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIV_Kafka
{
    public class DataCacheService : BackgroundService
    {
        private Timer _deleteTimer;
        private Timer _faultTimer;

        private readonly MyDbContext dbContext;   
        static ConcurrentQueue<KAFKA_DATA> _KAFKA_DATA = new ConcurrentQueue<KAFKA_DATA>();
        static ConcurrentQueue<TB_YSBW> _TB_YSBW = new ConcurrentQueue<TB_YSBW>();

        public DataCacheService(MyDbContext dbContext)
        {
            this.dbContext = dbContext;
            SnowFlakeSingle.WorkId = 0;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {

                var addKafkaList = DequeueAll(_KAFKA_DATA);
                var ysbw = DequeueAll(_TB_YSBW);

                Stopwatch stopwatch = Stopwatch.StartNew();
                try
                {
                    //dbContext.Fastest<PartsLife>().PageSize(plList.Count).BulkCopy(plList);
                    dbContext.Fastest<KAFKA_DATA>().PageSize(addKafkaList.Count).SplitTable().BulkCopy(addKafkaList);               
                    dbContext.Fastest<TB_YSBW>().PageSize(ysbw.Count).SplitTable().BulkCopy(ysbw);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                stopwatch.Stop();
                Console.WriteLine("====================================");
                //Console.WriteLine($"{plList.Count}  {tpdList.Count} {tpnList.Count} {tpnUpdateList.Count} {ysbw.Count}");
                Console.WriteLine($"插入耗时 == {stopwatch.ElapsedMilliseconds}");
                Console.WriteLine("====================================");

                await Task.Delay(5000, stoppingToken);
            }
        }

        private static List<T> DequeueAll<T>(ConcurrentQueue<T> queue)
        {
            var list = new List<T>();
            while (queue.TryDequeue(out var item))
            {
                list.Add(item);
            }
            return list;
        }


        public static void AddKAFKA_DATA(List<KAFKA_DATA> list)
        {
            foreach (var item in list)
            {
                _KAFKA_DATA.Enqueue(item);
            }
        }

        public static void AddTB_YSBW(TB_YSBW data)
        {
            _TB_YSBW.Enqueue(data);
        }

    }
}
