using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using static Confluent.Kafka.ConfigPropertyNames;

namespace SIV_Kafka
{
    public class KafkaConsumerHelper<TKey, TValue> : IDisposable
    {
        private readonly ConsumerConfig _consumerConfig;
        //private List<IConsumer<TKey, TValue>> _consumers=new List<IConsumer<TKey, TValue>>();
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;
        private string topic = "";
        private ConcurrentBag<IConsumer<TKey, TValue>> _consumers = new ConcurrentBag<IConsumer<TKey, TValue>>();


        public KafkaConsumerHelper(string bootstrapServers, string groupId, string topic)
        {
            this.topic = topic;
            _consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false,

            };
            _cancellationTokenSource = new CancellationTokenSource();
            //InitializeConsumer();
        }
        public KafkaConsumerHelper(string bootstrapServers, string groupId, string username, string password, string topic)
        {
            this.topic = topic;
            _consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                SaslUsername = username,
                SaslPassword = password,
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false,
            };
            _cancellationTokenSource = new CancellationTokenSource();
        }
      
        /// <summary>
        /// 异步方法，用于启动多个线程来消费 Kafka 主题中的消息
        /// </summary>
        /// <param name="consumeCallback"></param>
        /// <param name="numThreads">启动的并行消费线程的数量,默认5</param>
        /// <returns></returns>
        public async Task StartConsumingWithMultipleThreadsAsync(Func<ConsumeResult<TKey, TValue>, Task<bool>> consumeCallback, int numThreads = 5)
        {
            // 创建一个任务列表
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < numThreads; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    // 创建一个新的消费者实例
                    var _consumer = new ConsumerBuilder<TKey, TValue>(_consumerConfig).Build();
                    // 订阅主题
                    _consumer.Subscribe(topic);
                    // 将消费者实例添加到消费者列表中
                    _consumers.Add(_consumer);
                    // 当取消标记未被请求时
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // 从主题中消费消息
                            var consumeResult = _consumer.Consume(_cancellationTokenSource.Token);
                            // 处理消费的消息
                            bool processingSuccess = await consumeCallback(consumeResult);
                            // 如果处理成功
                            if (processingSuccess)
                            {
                                // 提交消费的消息
                                //_consumer.Commit(consumeResult);
                            }
                            else
                            {
                                // 如果处理失败，将消息重新放回主题
                            }
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"捕获 Exception occurred: {ex}");
                        }
                    }
                }));
            }

            // 等待所有任务完成
            await Task.WhenAll(tasks);
        }
        public async Task StartConsumingWithMultipleThreadsAsyncV1(Func<ConsumeResult<TKey, TValue>, Task<bool>> consumeCallback, int numThreads = 5)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < numThreads; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using (var consumer = new ConsumerBuilder<TKey, TValue>(_consumerConfig).Build())
                    {
                        consumer.Subscribe(topic);

                        // 使用ConcurrentBag添加消费者实例  
                        _consumers.Add(consumer);

                        try
                        {
                            while (!_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                var consumeResult = consumer.Consume(_cancellationTokenSource.Token);
                                bool processingSuccess = await consumeCallback(consumeResult);

                                if (processingSuccess)
                                {
                                    consumer.Commit(consumeResult);
                                }
                                else
                                {
                                    // 处理失败逻辑  
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // 取消逻辑  
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"An exception occurred: {ex}");
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // 清理逻辑，如果需要的话可以在这里关闭所有消费者等  
        }

        public async Task StartConsumingWithMultipleThreadsAsyncV2(Func<ConsumeResult<TKey, TValue>, Task<bool>> consumeCallback, int numThreads = 5)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < numThreads; i++)
            {
                tasks.Add(ConsumeAsync(consumeCallback));
            }

            await Task.WhenAll(tasks);

            // 清理逻辑，比如关闭所有消费者等  
            foreach (var consumer in _consumers)
            {
                consumer.Close();
            }
            _consumers.Clear();
        }

        private async Task ConsumeAsync(Func<ConsumeResult<TKey, TValue>, Task<bool>> consumeCallback)
        {
            using var consumer = new ConsumerBuilder<TKey, TValue>(_consumerConfig).Build();
            consumer.Subscribe(topic);
            _consumers.Add(consumer);

            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(_cancellationTokenSource.Token);
                    bool processingSuccess = await consumeCallback(consumeResult);

                    if (processingSuccess)
                    {
                        consumer.Commit(consumeResult);
                    }
                    else
                    {
                        // 处理失败逻辑  
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 取消逻辑  
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred: {ex}");
            }
        }


        public async Task StartConsumingWithMultipleThreadsAsyncV1(Func<ConsumeResult<TKey, TValue>, Task<bool>> consumeCallback, int numThreads = 5, int batchSize = 100)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < numThreads; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var consumer = new ConsumerBuilder<TKey, TValue>(_consumerConfig).Build();
                    consumer.Subscribe(topic);

                    // 使用ConcurrentBag添加消费者实例    
                    _consumers.Add(consumer);

                    List<ConsumeResult<TKey, TValue>> successfulResults = new List<ConsumeResult<TKey, TValue>>();

                    try
                    {
                        while (!_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            var consumeResult = consumer.Consume(_cancellationTokenSource.Token); // 假设Consume方法是同步的  
                            bool processingSuccess = await consumeCallback(consumeResult);

                            if (processingSuccess)
                            {
                                successfulResults.Add(consumeResult);

                                // 当达到批量大小或满足其他条件时，提交偏移量  
                                if (successfulResults.Count >= batchSize)
                                {
                                    await Task.Run(() =>
                                    {
                                        foreach (var result in successfulResults)
                                        {
                                            consumer.Commit(result); // 同步的Commit方法  
                                        }
                                    });
                                    successfulResults.Clear();
                                }
                            }
                            else
                            {
                                // 处理失败逻辑    
                            }
                        }

                        // 提交剩余的偏移量  
                        if (successfulResults.Count > 0)
                        {
                            await Task.Run(() =>
                            {
                                foreach (var result in successfulResults)
                                {
                                    consumer.Commit(result); // 同步的Commit方法  
                                }
                            });
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 取消逻辑    
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An exception occurred: {ex}");
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // 清理逻辑，如果需要的话可以在这里关闭所有消费者等    
        }


        public void StopConsuming()
        {
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var _consumer in _consumers)
                        _consumer?.Dispose();
                    _cancellationTokenSource?.Dispose();
                }
                _disposed = true;
            }
        }
    }

}

