using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HR_KD.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HR_KD.Services
{
    public class YearlyTaskService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public YearlyTaskService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Tạo scope mới để sử dụng dịch vụ Scoped
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var phepNamService = scope.ServiceProvider.GetRequiredService<PhepNamService>();

                // Logic để tự động chạy vào đầu năm mới (1/1)
                if (DateTime.Now.Month == 1 && DateTime.Now.Day == 1)
                {
                    await phepNamService.AutoResetAndCalculatePhepNamAsync(DateTime.Now.Year);
                }
            }
        }
    }
}
