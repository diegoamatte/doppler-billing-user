using System.Data.Common;
using Doppler.BillingUser.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Doppler.BillingUser.Test.Utils
{
    public static class ServiceCollectionExtensions
    {
        public static void SetupConnectionFactory(this IServiceCollection services, DbConnection dbConnection)
        {
            var mockDatabaseConnectionFactory = new Mock<IDatabaseConnectionFactory>();
            mockDatabaseConnectionFactory.Setup(a => a.GetConnection()).Returns(dbConnection);
            services.AddSingleton(mockDatabaseConnectionFactory.Object);
        }
    }
}
