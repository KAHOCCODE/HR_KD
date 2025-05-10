using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace HR_KD.Services
{
    public class HolidayNotificationService : BackgroundService
    {
        private readonly ILogger<HolidayNotificationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly HttpClient _httpClient;

        public HolidayNotificationService(
            ILogger<HolidayNotificationService> logger,
            IServiceProvider serviceProvider,
            HttpClient httpClient)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _httpClient = httpClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var nextRun = new DateTime(now.Year + 1, 1, 1, 0, 0, 0);
                    var delay = nextRun - now;

                    if (delay.TotalMilliseconds > 0)
                    {
                        await Task.Delay(delay, stoppingToken);
                    }

                    // Kiểm tra xem có phải ngày 1 tháng 1 không
                    if (now.Day == 1 && now.Month == 1)
                    {
                        _logger.LogInformation("Đang gửi thông báo ngày lễ cho năm {Year}", now.Year);

                        // Gọi API để gửi thông báo
                        var response = await _httpClient.PostAsync(
                            "api/holidaymanager/send-yearly-notification",
                            null,
                            stoppingToken);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Đã gửi thông báo ngày lễ thành công cho năm {Year}", now.Year);
                        }
                        else
                        {
                            _logger.LogError("Lỗi khi gửi thông báo ngày lễ: {StatusCode}", response.StatusCode);
                        }
                    }

                    // Đợi đến ngày tiếp theo
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi thực hiện gửi thông báo ngày lễ");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }
    }
} 