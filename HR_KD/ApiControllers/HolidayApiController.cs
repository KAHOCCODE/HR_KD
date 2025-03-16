using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HR_KD.Controllers
{
    [Route("api/Holidays")]
    [ApiController]
    public class HolidaysController : ControllerBase
    {
        private static List<NgayLe> holidays = new List<NgayLe>();

        [HttpPost("Add")]
        public IActionResult AddHoliday([FromBody] NgayLe holiday)
        {
            if (holiday == null || string.IsNullOrEmpty(holiday.TenNgayLe) || holiday.NgayLe1 == default)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            holiday.MaNgayLe = holidays.Count + 1;
            holidays.Add(holiday);

            return Ok(new { success = true, message = "Ngày lễ đã được thêm thành công!" });
        }

        [HttpGet("GetAll")]
        public IActionResult GetAllHolidays()
        {
            return Ok(holidays);
        }
    }

    public class NgayLe
    {
        public int MaNgayLe { get; set; }
        public string TenNgayLe { get; set; } = null!;
        public DateTime NgayLe1 { get; set; }
        public int? SoNgayNghi { get; set; }
        public string? MoTa { get; set; }
    }
}
