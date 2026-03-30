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
        public async Task<IActionResult> GetAll()
        {
            var conditions = new List<ScanCondition>();
            var devices = await _context.ScanAsync<Device>(conditions).GetRemainingAsync();
            return Ok(devices);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var device = await _context.LoadAsync<Device>(id);

            if (device == null)
                return NotFound(new { message = $"Device with ID '{id}' not found." });

            return Ok(device);
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
