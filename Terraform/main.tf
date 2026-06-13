data "archive_file" "lambda" {
  type        = "zip"
  source_dir  = "../publish/"
  output_path = "./ecom_ef_devices_api.zip"
  depends_on  = [null_resource.build_dotnet_lambda]
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

## AWS Lambda Resources
resource "aws_lambda_function" "ecom_ef_devices_api" {
  filename         = "ecom_ef_devices_api.zip"
  function_name    = "EcomEfDevicesApi"
  role             = aws_iam_role.lambda_role.arn
  handler          = "ecom_ef_devices_api"
  source_code_hash = data.archive_file.lambda.output_base64sha256
  runtime          = "dotnet8"
  depends_on       = [data.archive_file.lambda,aws_iam_role.lambda_role]
  environment {
    variables = {
      ASPNETCORE_ENVIRONMENT = var.environment
    }
  }
}

resource "aws_lambda_function_url" "ecom_ef_devices_api_url" {
  function_name      = aws_lambda_function.ecom_ef_devices_api.function_name
  authorization_type = "NONE"

  cors {
    allow_credentials = true
    allow_origins     = ["*"]
    allow_methods     = ["GET", "POST", "PUT", "DELETE"]
    allow_headers     = ["content-type", "authorization"]
    max_age           = 86400
  }
}

resource "aws_lambda_permission" "allow_public_invoke" {
  statement_id  = "AllowPublicInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.ecom_ef_devices_api.function_name
  principal     = "*"
}

resource "aws_lambda_permission" "allow_public_invoke_url" {
  statement_id           = "AllowPublicInvokeUrl"
  action                 = "lambda:InvokeFunctionUrl"
  function_name          = aws_lambda_function.ecom_ef_devices_api.function_name
  principal              = "*"
  function_url_auth_type = "NONE"
}