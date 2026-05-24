using System.Text;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using ecom_ef_devices_api.Dtos;
using ecom_ef_devices_api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ecom_ef_devices_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly IDynamoDBContext _context;

        public DeviceController(IDynamoDBContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetDevices(
           [FromQuery] string? deviceType,
           [FromQuery] string? deviceId, 
           [FromQuery] string? sortBy = "createdAt",
           [FromQuery] bool isDesc = true)
        {
            // Scan conditions
            var conditions = new List<ScanCondition>();

            if (!string.IsNullOrWhiteSpace(deviceType))
            {
                conditions.Add(
                    new ScanCondition(
                        "DeviceType",
                        ScanOperator.Equal,
                        deviceType));
            }

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                conditions.Add(
                    new ScanCondition(
                        "DeviceId",
                        ScanOperator.Equal,
                        deviceId));
            }

            // Scan DynamoDB
            var search = _context.ScanAsync<Device>(conditions);

            var devices = await search.GetRemainingAsync();

            // Normalize
            sortBy = sortBy?.ToLower();

            // Sorting
            devices = sortBy switch
            {
                "deviceid" => isDesc
                    ? devices.OrderByDescending(x => x.DeviceId).ToList()
                    : devices.OrderBy(x => x.DeviceId).ToList(),

                "devicetype" => isDesc
                    ? devices.OrderByDescending(x => x.DeviceType).ToList()
                    : devices.OrderBy(x => x.DeviceType).ToList(),

                "updatedat" => isDesc
                    ? devices.OrderByDescending(x => x.UpdatedAt).ToList()
                    : devices.OrderBy(x => x.UpdatedAt).ToList(),

                _ => isDesc
                    ? devices.OrderByDescending(x => x.CreatedAt).ToList()
                    : devices.OrderBy(x => x.CreatedAt).ToList(),
            };


            return Ok(devices);
        }

        [HttpGet("export")]
        public async Task<IActionResult> DownloadDevicesCsv(
        [FromQuery] string? deviceType,
        [FromQuery] string? deviceId,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool isDesc = true)
        {
            // Scan conditions
            var conditions = new List<ScanCondition>();

            if (!string.IsNullOrWhiteSpace(deviceType))
            {
                conditions.Add(
                    new ScanCondition(
                        "DeviceType",
                        ScanOperator.Equal,
                        deviceType));
            }

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                conditions.Add(
                    new ScanCondition(
                        "DeviceId",
                        ScanOperator.Equal,
                        deviceId));
            }

            // Get data
            var search = _context.ScanAsync<Device>(conditions);

            var devices = await search.GetRemainingAsync();

            // Sorting
            sortBy = sortBy?.ToLower();

            devices = sortBy switch
            {
                "deviceid" => isDesc
                    ? devices.OrderByDescending(x => x.DeviceId).ToList()
                    : devices.OrderBy(x => x.DeviceId).ToList(),

                "devicetype" => isDesc
                    ? devices.OrderByDescending(x => x.DeviceType).ToList()
                    : devices.OrderBy(x => x.DeviceType).ToList(),

                "updatedat" => isDesc
                    ? devices.OrderByDescending(x => x.UpdatedAt).ToList()
                    : devices.OrderBy(x => x.UpdatedAt).ToList(),

                _ => isDesc
                    ? devices.OrderByDescending(x => x.CreatedAt).ToList()
                    : devices.OrderBy(x => x.CreatedAt).ToList(),
            };

            // CSV Builder
            var csv = new StringBuilder();

            // Header
            csv.AppendLine(
                "Id,DeviceId,DeviceType,CreatedAt,UpdatedAt,Payload");

            // Rows
            foreach (var device in devices)
            {
                var payloadJson =
                    System.Text.Json.JsonSerializer.Serialize(
                        device.Payload);

                csv.AppendLine(string.Join(",",
                    EscapeCsv(device.Id),
                    EscapeCsv(device.DeviceId),
                    EscapeCsv(device.DeviceType),
                    EscapeCsv(device.CreatedAt.ToString("o")),
                    EscapeCsv(device.UpdatedAt.ToString("o")),
                    EscapeCsv(payloadJson)
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());

            return File(
                bytes,
                "text/csv",
                $"devices-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        private string EscapeCsv(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "\"\"";

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDeviceRequest request)
        {
            var device = new Device
            {
                DeviceId = request.DeviceId,
                DeviceType = request.DeviceType,
                Payload = request.Payload
            };

            await _context.SaveAsync(device);

            return Ok(device);
        }
    }
}

