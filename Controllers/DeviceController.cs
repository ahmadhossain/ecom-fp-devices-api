using Amazon.DynamoDBv2.DataModel;
using ecom_ef_devices_api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ecom_ef_devices_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        // private readonly IMongoCollection<Device>? _devices;
        private readonly IDynamoDBContext _context;

        public DeviceController(IDynamoDBContext context) => _context = context;

        // [HttpGet]
        // public async Task<IEnumerable<Device>> Get()
        // {
        //     return await _devices.Find(FilterDefinition<Device>.Empty).ToListAsync();
        // }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var device = await _context.LoadAsync<Device>(id);
            if (device == null)
                return NotFound();
            return Ok(device);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Device request)
        {
            var device = await _context.LoadAsync<Device>(request.Id);
            if (device != null)
                return BadRequest("Device with the same ID already exists.");
            await _context.SaveAsync(request);
            return Ok(request);
        }
    }
}
