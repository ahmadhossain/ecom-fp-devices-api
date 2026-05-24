using System.ComponentModel.DataAnnotations;

namespace ecom_ef_devices_api.Dtos
{
    public class CreateDeviceRequest
    {
        [Required]
        public string? DeviceId { get; set; }

        [Required]
        public string? DeviceType { get; set; }

        [Required]
        public Dictionary<string, object?>? Payload { get; set; }
    }
}
