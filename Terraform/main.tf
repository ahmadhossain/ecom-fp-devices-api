data "archive_file" "lambda" {
  type        = "zip"
  source_dir  = "../publish/"
  output_path = "./ecom_ef_devices_api.zip"
  depends_on  = [null_resource.build_dotnet_lambda]
}

resource "aws_dynamodb_table" "devices" {
  name         = var.table_name
  billing_mode = "PAY_PER_REQUEST"

  hash_key = "id"

  attribute {
    name = "id"
    type = "S"
  }
}

# IAM Role
resource "aws_iam_role" "lambda_role" {
  name = "${var.project_name}-lambda-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"

    Statement = [{
      Effect = "Allow"

      Principal = {
        Service = "lambda.amazonaws.com"
      }

      Action = "sts:AssumeRole"
    }]
  })
}

# CloudWatch Logs
resource "aws_iam_role_policy_attachment" "logs" {
  role       = aws_iam_role.lambda_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

# DynamoDB Policy
resource "aws_iam_policy" "dynamodb_access" {
  name = "${var.project_name}-ddb-policy"

  policy = jsonencode({
    Version = "2012-10-17"

    Statement = [{
      Effect = "Allow"

      Action = "dynamodb:*",

      Resource = "*"
    }]
  })
}

resource "aws_iam_role_policy_attachment" "dynamodb_attach" {
  role       = aws_iam_role.lambda_role.name
  policy_arn = aws_iam_policy.dynamodb_access.arn
}

# AWS Lambda Resources
resource "aws_lambda_function" "ecom_ef_devices_api" {
  filename         = "ecom_ef_devices_api.zip"
  function_name    = "EcomEfDevicesApi"
  role             = aws_iam_role.lambda_role.arn
  handler          = "ecom_ef_devices_api"
  source_code_hash = data.archive_file.lambda.output_base64sha256
  runtime          = "dotnet8"
  memory_size      = 256
  timeout          = 30
  depends_on       = [data.archive_file.lambda,aws_iam_role.lambda_role]
  environment {
    variables = {
      ASPNETCORE_ENVIRONMENT = var.environment
    }
  }
}

# API Gateway
resource "aws_apigatewayv2_api" "ecom_ef_devices_api" {
  name          = "${var.project_name}-api"
  protocol_type = "HTTP"

  cors_configuration {
    allow_origins = ["*"]
    allow_methods = ["GET", "POST", "PUT", "DELETE"]
    allow_headers = ["content-type", "authorization"]
    max_age       = 86400
  }
}

resource "aws_apigatewayv2_integration" "ecom_ef_devices_api" {
  api_id                 = aws_apigatewayv2_api.ecom_ef_devices_api.id
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.ecom_ef_devices_api.invoke_arn
  payload_format_version = "2.0"
}

# GET /api/Device?deviceType=&deviceId=&sortBy=&isDesc=
resource "aws_apigatewayv2_route" "get_devices" {
  api_id    = aws_apigatewayv2_api.ecom_ef_devices_api.id
  route_key = "GET /api/Device"
  target    = "integrations/${aws_apigatewayv2_integration.ecom_ef_devices_api.id}"
}

# GET /api/Device/export?deviceType=&deviceId=&sortBy=&isDesc=
resource "aws_apigatewayv2_route" "export_devices" {
  api_id    = aws_apigatewayv2_api.ecom_ef_devices_api.id
  route_key = "GET /api/Device/export"
  target    = "integrations/${aws_apigatewayv2_integration.ecom_ef_devices_api.id}"
}

# POST /api/Device
resource "aws_apigatewayv2_route" "create_device" {
  api_id    = aws_apigatewayv2_api.ecom_ef_devices_api.id
  route_key = "POST /api/Device"
  target    = "integrations/${aws_apigatewayv2_integration.ecom_ef_devices_api.id}"
}

# Swagger UI — GET /swagger
resource "aws_apigatewayv2_route" "swagger_root" {
  api_id    = aws_apigatewayv2_api.ecom_ef_devices_api.id
  route_key = "GET /swagger"
  target    = "integrations/${aws_apigatewayv2_integration.ecom_ef_devices_api.id}"
}

# Swagger UI — GET /swagger/{proxy+} (index.html, swagger.json, static assets)
resource "aws_apigatewayv2_route" "swagger_proxy" {
  api_id    = aws_apigatewayv2_api.ecom_ef_devices_api.id
  route_key = "GET /swagger/{proxy+}"
  target    = "integrations/${aws_apigatewayv2_integration.ecom_ef_devices_api.id}"
}

resource "aws_apigatewayv2_stage" "ecom_ef_devices_api" {
  api_id      = aws_apigatewayv2_api.ecom_ef_devices_api.id
  name        = "$default"
  auto_deploy = true
}

resource "aws_lambda_permission" "allow_api_gateway" {
  statement_id  = "AllowAPIGatewayInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.ecom_ef_devices_api.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.ecom_ef_devices_api.execution_arn}/*/*"
}
