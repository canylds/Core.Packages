using Core.CrossCuttingConcerns.Logging.Serilog.ConfigurationModels;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Serilog;

namespace Core.CrossCuttingConcerns.Logging.Serilog.Loggers;

public class MongoDbLogger : LoggerServiceBase
{
    public MongoDbLogger(IConfiguration configuration)
    {
        MongoDbConfiguration? logConfiguration = configuration
            .GetSection("SeriLogConfigurations:MongoDbConfiguration")
            .Get<MongoDbConfiguration>();

        Logger = new LoggerConfiguration().WriteTo.MongoDBBson(configuration =>
        {
            MongoClient client = new(logConfiguration.ConnectionString);
            IMongoDatabase? mongoDbInstance = client.GetDatabase(logConfiguration.Collection);
            configuration.SetMongoDatabase(mongoDbInstance);
        }).CreateLogger();
    }
}
