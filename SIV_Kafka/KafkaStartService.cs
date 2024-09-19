using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIV_Kafka
{
    public class KafkaStartService : BackgroundService
    {
        private readonly KafkaService _kafkaService;

        public KafkaStartService(KafkaService kafkaService)
        {
            _kafkaService = kafkaService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ThreadPool.QueueUserWorkItem(async (stateInfo) =>
            {
                await _kafkaService.Start();
            });
            return Task.CompletedTask;
        }
    }
}
