using Microsoft.Extensions.Configuration;
using SqlSugar;
using System.Configuration;
using System.Reflection;

namespace DataBase
{

    public class MyDbContext : SqlSugarScope
    {
        private readonly IConfiguration _Config;
        public MyDbContext(IConfiguration config) :
            base(new ConnectionConfig()
            {
                DbType = DbType.SqlServer,
                ConnectionString = config.GetConnectionString("DB"),
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute,
                ConfigureExternalServices = new ConfigureExternalServices()
                {
                    EntityNameService = (type, entity) =>
                    {

                    }
                }
            })
        {
            _Config = config;
        }
    }
}