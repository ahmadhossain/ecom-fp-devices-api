using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
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
        public async Task<IActionResult> GetAll(
            [FromQuery] string? deviceType,
            [FromQuery] string? deviceId,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? sortOrder = "desc"
        )
        {
            var allDevices = await _context
                .ScanAsync<Device>(new List<ScanCondition>())
                .GetRemainingAsync();

            var filtered = allDevices.AsQueryable();

            if (!string.IsNullOrWhiteSpace(deviceId))
                filtered = filtered.Where(x => x.DeviceId == deviceId);

            if (!string.IsNullOrWhiteSpace(deviceType))
                filtered = filtered.Where(x => x.DeviceType == deviceType);

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                bool isDesc = sortOrder?.ToLower() == "desc";

                filtered = sortBy switch
                {
                    "createdAt" => isDesc
                        ? filtered.OrderByDescending(x => x.CreatedAt)
                        : filtered.OrderBy(x => x.CreatedAt),

                    "updatedAt" => isDesc
                        ? filtered.OrderByDescending(x => x.UpdatedAt)
                        : filtered.OrderBy(x => x.UpdatedAt),

                    "deviceId" => isDesc
                        ? filtered.OrderByDescending(x => x.DeviceId)
                        : filtered.OrderBy(x => x.DeviceId),

                    "deviceType" => isDesc
                        ? filtered.OrderByDescending(x => x.DeviceType)
                        : filtered.OrderBy(x => x.DeviceType),

                    _ => filtered,
                };
            }
            var result = filtered.ToList();

            var sb = new StringBuilder();

            sb.AppendLine("DeviceId,DeviceType,CreatedAt,UpdatedAt,Payload");

            foreach (var d in result)
            {
                var payloadJson = System.Text.Json.JsonSerializer.Serialize(d.Payload);

                sb.AppendLine(
                    $"{d.DeviceId},"
                        + $"{d.DeviceType},"
                        + $"{d.CreatedAt:o},"
                        + $"{d.UpdatedAt:o},"
                        + $"\"{payloadJson.Replace("\"", "\"\"")}\""
                );
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());

            return File(bytes, "text/csv", "devices.csv");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JsonElement request)
        {
            // Convert entire incoming JSON to Dictionary
            var payload = JsonElementConverter.ToDictionary(request);

            if (payload == null)
                return BadRequest("Invalid payload.");

            var missingFields = new List<string>();

            if (
                !payload.ContainsKey("deviceId")
                || string.IsNullOrWhiteSpace(payload["deviceId"]?.ToString())
            )
                missingFields.Add("deviceId");

            if (
                !payload.ContainsKey("deviceType")
                || string.IsNullOrWhiteSpace(payload["deviceType"]?.ToString())
            )
                missingFields.Add("deviceType");

            if (missingFields.Any())
                return BadRequest(
                    new
                    {
                        message = "The following required fields are missing or empty.",
                        fields = missingFields,
                    }
                );

            var now = DateTime.UtcNow;
            var deviceId = payload["deviceId"]?.ToString();
            var deviceType = payload["deviceType"]?.ToString();

            var device = new Device
            {
                CreatedAt = now,
                UpdatedAt = now,
                DeviceId = deviceId!,
                DeviceType = deviceType!,
                Payload = payload!,
            };

            await _context.SaveAsync(device);

            return Ok(device);
        }
    }
}
