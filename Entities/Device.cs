using Amazon.DynamoDBv2.DataModel;

namespace ecom_ef_devices_api.Entities
{
    [DynamoDBTable("devices")]
    public class Device
    {
        [DynamoDBHashKey("id")]
        public string? Id { get; set; } = Guid.NewGuid().ToString();

        [DynamoDBProperty("deviceId")]
        public string? DeviceId { get; set; }

        [DynamoDBProperty("deviceType")]
        public string? DeviceType { get; set; }

        [DynamoDBProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [DynamoDBProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [DynamoDBProperty]
        public Dictionary<string, object> ExtraFields { get; set; } = new();
    }
}
