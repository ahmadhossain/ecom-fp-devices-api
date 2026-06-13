resource "null_resource" "build_dotnet_lambda" {
  provisioner "local-exec" {
    command     = <<EOT
      dotnet restore ../ecom_ef_devices_api.csproj
      dotnet publish ../ecom_ef_devices_api.csproj -c Release -r linux-x64 --self-contained false -o ../publish
    EOT
    interpreter = ["PowerShell", "-Command"]
  }
  triggers = {
    always_run = "${timestamp()}"
  }
}