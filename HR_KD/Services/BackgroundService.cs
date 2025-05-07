using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HR_KD.Services
{
    public class YearlyTaskService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<YearlyTaskService> _logger;
        private DateTime _lastRunDate;

        public YearlyTaskService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<YearlyTaskService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _lastRunDate = DateTime.MinValue; // Khởi tạo để đảm bảo chạy lần đầu
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var today = DateTime.Today;
                // Kiểm tra xem có phải là ngày 1/1 và chưa chạy cho ngày hôm nay
                if (today.Month == 1 && today.Day == 1 && today != _lastRunDate.Date)
                {
                    _logger.LogInformation("Bắt đầu quy trình reset phép năm cho năm {Year}", today.Year);

                    try
                    {
                        // Tạo scope mới để sử dụng dịch vụ Scoped
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var phepNamService = scope.ServiceProvider.GetRequiredService<PhepNamService>();

                            // Bước 1: Reset IsReset flag cho các năm trước
                            _logger.LogInformation("Đang reset IsReset flag cho các năm trước {Year}", today.Year);
                            await phepNamService.ResetIsResetFlagForPreviousYearsAsync(today.Year);

                            // Bước 2: Thực hiện tính toán và reset phép năm cho năm mới
                            _logger.LogInformation("Đang tính toán và reset phép năm cho năm {Year}", today.Year);
                            await phepNamService.AutoResetAndCalculatePhepNamAsync(today.Year);

                            _lastRunDate = today; // Cập nhật ngày đã chạy
                            _logger.LogInformation("Đã hoàn thành quy trình reset phép năm cho năm {Year}", today.Year);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Ghi log lỗi thay vì sử dụng Console.WriteLine
                        _logger.LogError(ex, "Lỗi khi thực hiện reset phép năm cho năm {Year}: {Message}",
                            today.Year, ex.Message);
                    }
                }

                // Kiểm tra lại sau 6 tiếng
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
    }
}