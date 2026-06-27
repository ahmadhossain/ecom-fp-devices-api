# ecom-ef-devices-api

An ASP.NET Core 8 Web API for storing and managing dynamic IoT/eCommerce device data, backed by Amazon DynamoDB and deployed as an AWS Lambda function behind API Gateway HTTP API (v2), provisioned with Terraform.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 (`net8.0`) |
| Database | Amazon DynamoDB |
| Runtime | AWS Lambda (dotnet8) |
| API Gateway | Amazon API Gateway HTTP API (v2, payload format 2.0) |
| API Docs | Swagger / Swashbuckle |
| Infrastructure | Terraform >= 1.6, AWS Provider ~> 5.0 |

### NuGet Packages

| Package | Version |
|---|---|
| `Amazon.Lambda.AspNetCoreServer.Hosting` | 1.10.0 |
| `AWSSDK.DynamoDBv2` | 4.0.17.1 |
| `AWSSDK.Extensions.NETCore.Setup` | 4.0.3.26 |
| `Swashbuckle.AspNetCore` | 6.6.2 |

---

## Project Structure

```
ecom-ef-devices-api/
├── Controllers/
│   └── DeviceController.cs       # API endpoints
├── Dtos/
│   └── CreateDeviceRequest.cs    # Request DTO with validation
├── Entities/
│   └── Device.cs                 # DynamoDB-mapped entity
├── Terraform/
│   ├── providers.tf              # AWS provider (us-east-1, ~> 5.0)
│   ├── variables.tf              # Input variables
│   ├── main.tf                   # DynamoDB, Lambda, API Gateway, IAM
│   ├── build.tf                  # dotnet publish + zip via null_resource
│   └── output.tf                 # API Gateway invoke URL output
├── DictionaryConverter.cs        # Custom IPropertyConverter for dynamic payloads
├── Program.cs                    # App entry point, DI, Lambda hosting
├── appsettings.json
├── appsettings.Development.json
├── aws-lambda-tools-defaults.json
└── ecom_ef_devices_api.csproj
```

---

## DynamoDB Table

| Property | Value |
|---|---|
| Table name | `devices` |
| Partition key | `id` (String) |
| Billing mode | PAY_PER_REQUEST |

### Device Schema

| Field | Type | Notes |
|---|---|---|
| `id` | String | Auto-generated UUID (partition key) |
| `deviceId` | String | Caller-supplied device identifier |
| `deviceType` | String | e.g. `oven`, `fridge`, `dishwasher` |
| `createdAt` | DateTime | Set on creation |
| `updatedAt` | DateTime | Set on creation |
| `payload` | Map | Fully dynamic — see below |

---

## Dynamic Payload Support

The `payload` field accepts any schema-less JSON object, including nested objects, arrays, numbers, booleans, strings, and nulls. This is handled by the custom `DictionaryConverter` (`IPropertyConverter`) which bi-directionally maps between `Dictionary<string, object?>` and DynamoDB native types.

```json
{
  "temperature": 25.5,
  "active": true,
  "location": {
    "room": "A1",
    "floor": 2
  },
  "tags": ["iot", "lab"]
}
```

---

## API Endpoints

### `POST /api/Device` — Create a Device

**Request body:**

```json
{
  "deviceId": "oven-001",
  "deviceType": "oven",
  "payload": {
    "temperature": 220,
    "mode": "bake",
    "active": true,
    "location": {
      "room": "Kitchen-A"
    }
  }
}
```

**Response `200 OK`:**

```json
{
  "id": "219a4425-24fa-4e95-9b41-575b7652944a",
  "deviceId": "oven-001",
  "deviceType": "oven",
  "createdAt": "2026-05-22T15:22:02.54Z",
  "updatedAt": "2026-05-22T15:22:02.54Z",
  "payload": {
    "temperature": 220,
    "mode": "bake",
    "active": true,
    "location": { "room": "Kitchen-A" }
  }
}
```

---

### `GET /api/Device` — List Devices

| Query Parameter | Type | Default | Description |
|---|---|---|---|
| `deviceType` | string | — | Filter by device type |
| `deviceId` | string | — | Filter by device ID |
| `sortBy` | string | `createdAt` | `createdAt` \| `updatedAt` \| `deviceId` \| `deviceType` |
| `isDesc` | bool | `true` | `true` = descending, `false` = ascending |

**Example:**

```http
GET /api/Device?deviceType=oven&sortBy=createdAt&isDesc=false
```

---

### `GET /api/Device/export` — Export as CSV

Accepts the same query parameters as `GET /api/Device`. Returns a timestamped `.csv` file download.

**Example:**

```http
GET /api/Device/export?deviceType=oven&sortBy=deviceId&isDesc=true
```

**Downloaded file:** `devices-20260524120000.csv`

**CSV columns:** `Id`, `DeviceId`, `DeviceType`, `CreatedAt`, `UpdatedAt`, `Payload`

---

### Swagger UI

```
GET /swagger
GET /swagger/{proxy+}
```

Available at:

```
https://<id>.execute-api.us-east-1.amazonaws.com/swagger
```

---

## Example Device Payloads

### Oven

```json
{ "deviceId": "oven-001", "deviceType": "oven", "payload": { "temperature": 220, "mode": "bake", "timerMinutes": 45 } }
```

### Fridge

```json
{ "deviceId": "fridge-001", "deviceType": "fridge", "payload": { "currentTemperature": 3, "doorOpen": false } }
```

### Dishwasher

```json
{ "deviceId": "dishwasher-001", "deviceType": "dishwasher", "payload": { "cycle": "eco", "remainingTimeMinutes": 30 } }
```

---

## Running Locally

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- AWS credentials configured (`~/.aws/credentials` or environment variables) with DynamoDB access
- A `devices` DynamoDB table in your target region

### Steps

```bash
git clone <your-repository-url>
cd ecom-ef-devices-api

dotnet restore
dotnet build
dotnet run
```

Swagger UI locally:

```
https://localhost:7207/swagger
http://localhost:5041/swagger
```

---

## Infrastructure (Terraform)

The `Terraform/` directory provisions all required AWS resources.

### Resources

| Resource | Description |
|---|---|
| `aws_dynamodb_table` | `devices` table with `id` partition key, PAY_PER_REQUEST |
| `aws_lambda_function` | `EcomEfDevicesApi` — dotnet8, 256 MB, 30s timeout |
| `aws_apigatewayv2_api` | HTTP API with CORS (all origins) |
| `aws_apigatewayv2_integration` | AWS_PROXY → Lambda, payload format 2.0 |
| `aws_apigatewayv2_route` (×5) | GET /api/Device, GET /api/Device/export, POST /api/Device, GET /swagger, GET /swagger/{proxy+} |
| `aws_apigatewayv2_stage` | `$default` stage with auto-deploy |
| `aws_iam_role` | Lambda execution role |
| `aws_iam_role_policy_attachment` | CloudWatch Logs policy |
| `aws_iam_policy` | Full DynamoDB access |
| `aws_lambda_permission` | Grants API Gateway permission to invoke Lambda |
| `null_resource` | Runs `dotnet publish` and zips output before upload |

### Lambda Configuration

| Property | Value |
|---|---|
| Function name | `EcomEfDevicesApi` |
| Runtime | `dotnet8` |
| Handler | `ecom_ef_devices_api` |
| Memory | 256 MB |
| Timeout | 30 s |
| Event source | `LambdaEventSource.HttpApi` (payload format 2.0) |

### Deploy

```bash
cd Terraform

terraform init
terraform plan
terraform apply
```

The API Gateway invoke URL is printed after `apply`:

```
ecom_ef_devices_api_url = "https://<id>.execute-api.us-east-1.amazonaws.com"
```

### Terraform Variables (`variables.tf`)

| Variable | Default |
|---|---|
| `aws_region` | `us-east-1` |
| `project_name` | `ecom_ef_devices_api` |
| `environment` | `Development` |
| `table_name` | `devices` |

### Build Process (`build.tf`)

Terraform automatically runs `dotnet publish` before packaging the Lambda zip:

```powershell
dotnet restore ../ecom_ef_devices_api.csproj
dotnet publish ../ecom_ef_devices_api.csproj -c Release -r linux-x64 --self-contained false -o ../publish
```

---

## License

MIT
