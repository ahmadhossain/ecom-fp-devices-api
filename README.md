# Ecommerce FP Devices API

A flexible ASP.NET Core Web API using Amazon DynamoDB for storing and managing dynamic IoT/eCommerce device data.

Supports:

- CSV export
- Filtering
- Sorting
- Dynamic payload fields
  - Nested objects
  - Arrays
- Auto-generated IDs
- DynamoDB integration

---

# Tech Stack

- ASP.NET Core
- Amazon DynamoDB
- Amazon Lambda
- Terraform

---

# Features

## Device Management

- Create devices
- Get all devices
- Filter devices
- Sort devices
- Download devices as CSV

## Dynamic Payload Support

Supports schema-less payloads:

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

# Project Structure

```text
Controllers/
 └── DeviceController.cs

Entities/
 └── Device.cs

Dtos/
 └── CreateDeviceRequest.cs
```

---

# Installation

## Clone Project

```bash
git clone <your-repository-url>
cd <project-folder>
```

---

## Install Packages

```bash
dotnet add package AWSSDK.DynamoDBv2
```
---

# DynamoDB Table

## Table Name

```text
devices
```

## Partition Key

```text
id (String)
```

---

# Create Device Request DTO

```csharp
using System.ComponentModel.DataAnnotations;

public class CreateDeviceRequest
{
    [Required]
    public string DeviceId { get; set; } = default!;

    [Required]
    public string DeviceType { get; set; } = default!;

    [Required]
    public Dictionary<string, object?> Payload
    {
        get;
        set;
    } = [];
}
```

---

# DynamicFieldsConverter

The custom converter supports:

- Nested objects
- Arrays
- Numbers
- Booleans
- Strings
- Null values

This allows DynamoDB to store fully dynamic payloads.

---

# API Endpoints

# Create Device

## Endpoint

```http
POST /api/device
```

## Request

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

## Response

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
    "location": {
      "room": "Kitchen-A"
    }
  }
}
```

---

# Get Devices

## Endpoint

```http
GET /api/device
```

## Query Parameters

| Parameter | Type | Description |
|---|---|---|
| deviceId | string | Filter by device ID |
| deviceType | string | Filter by device type |
| sortBy | string | createdAt, updatedAt, deviceId, deviceType |
| isDesc | bool | true = DESC, false = ASC |

---

## Example

```http
GET /api/device?deviceType=oven&sortBy=createdAt&isDesc=false
```

---

# Download CSV

## Endpoint

```http
GET /api/device/export
```

## Example

```http
GET /api/device/export?deviceType=oven&sortBy=deviceId&isDesc=true
```

Downloads:

```text
devices-20260524120000.csv
```

---

# Example Device Payloads

## Oven

```json
{
  "deviceId": "oven-001",
  "deviceType": "oven",
  "payload": {
    "temperature": 220,
    "mode": "bake",
    "timerMinutes": 45
  }
}
```

---

## Fridge

```json
{
  "deviceId": "fridge-001",
  "deviceType": "fridge",
  "payload": {
    "currentTemperature": 3,
    "doorOpen": false
  }
}
```

---

## Dishwasher

```json
{
  "deviceId": "dishwasher-001",
  "deviceType": "dishwasher",
  "payload": {
    "cycle": "eco",
    "remainingTimeMinutes": 30
  }
}
```

---

# Running the Project

```bash
dotnet restore
```

```bash
dotnet build
```

```bash
dotnet run
```

---

# Swagger

Swagger is available at:

```text
https://localhost:<port>/swagger
```

---

# License

MIT License
