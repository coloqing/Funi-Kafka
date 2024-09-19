using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Util
{
    /// <summary>
    /// 拓展类
    /// </summary>
    public static partial class Extention
    {
        /// <summary>  
        /// 使用AutoMapper自动映射拥有MapAttribute的类  
        /// </summary>  
        /// <param name="services">服务集合</param>  
        public static IServiceCollection AddAutoMapperExt(this IServiceCollection services)
        {
            try
            {
                var assembly = Assembly.LoadFrom("DataBase.dll");
                var typesWithMapAttribute = assembly.GetTypes()
                    .Where(x => x.GetCustomAttribute<MapAttribute>() != null)
                    .Select(x => (TypeFrom: x, TargetTypes: x.GetCustomAttribute<MapAttribute>().TargetTypes))
                    .ToList();

                var configuration = new MapperConfiguration(cfg =>
                {
                    foreach (var typeMapping in typesWithMapAttribute)
                    {
                        foreach (var targetType in typeMapping.TargetTypes)
                        {
                            // 假设你只需要单向映射，从 typeMapping.TypeFrom 到 targetType  
                            cfg.CreateMap(typeMapping.TypeFrom, targetType);

                            // 如果需要双向映射，则取消注释下一行  
                            cfg.CreateMap(targetType, typeMapping.TypeFrom);  
                        }
                    }
                });

                services.AddSingleton(configuration.CreateMapper());
            }
            catch (Exception ex)
            {
                // 处理加载程序集或映射配置中的异常  
                Console.WriteLine($"Error configuring AutoMapper: {ex.Message}");
                // 可以选择抛出异常或记录日志等  
            }

            return services;
        }
    }
}