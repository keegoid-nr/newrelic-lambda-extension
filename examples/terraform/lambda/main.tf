terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 4.8"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

data "aws_caller_identity" "current" {}

resource "aws_iam_policy" "newrelic_license_key_policy" {
  name        = "lambda-policy"
  description = "A policy to allow Lambda to get secret values from Secrets Manager"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = [
          "secretsmanager:GetSecretValue",
        ]
        Effect   = "Allow"
        Resource = "*" # You can replace this with the ARN of a specific secret if needed
      }
    ]
  })
}

resource "aws_iam_role" "newrelic_terraform_example_role" {
  name               = "${var.lambda_function_name}_example_role"
  assume_role_policy = file("./lambda-assume-role-policy.json")
}

resource "aws_iam_role_policy" "newrelic_terraform_example_role_policy" {
  name   = "${var.lambda_function_name}_example_role_policy"
  role   = aws_iam_role.newrelic_terraform_example_role.id
  policy = file("./lambda-policy.json")
}

resource "aws_iam_role_policy_attachment" "newrelic_license_key_policy_attachment" {
  role       = aws_iam_role.newrelic_terraform_example_role.name
  policy_arn = aws_iam_policy.newrelic_license_key_policy.arn
}

resource "aws_lambda_function" "newrelic_terraform_example_function" {
  description = "A simple Lambda function, with New Relic telemetry"
  depends_on = [
    aws_cloudwatch_log_group.newrelic_terraform_example_log_group,
    aws_iam_role.newrelic_terraform_example_role,
    aws_iam_role_policy_attachment.newrelic_license_key_policy_attachment
  ]
  memory_size   = 256
  filename      = var.lambda_zip_filename
  function_name = var.lambda_function_name
  # The handler for your function needs to be the one provided by the instrumentation layer, below.
  handler = var.wrapper_handler
  role    = aws_iam_role.newrelic_terraform_example_role.arn
  runtime = var.lambda_runtime
  environment {
    variables = {
      # For the instrumentation handler to invoke your real handler, we need this value
      NEW_RELIC_LAMBDA_HANDLER = var.lambda_function_handler
      NEW_RELIC_ACCOUNT_ID     = var.newrelic_account_id
      # Enable NR Lambda extension if the telemetry data are ingested via lambda extension
      NEW_RELIC_LAMBDA_EXTENSION_ENABLED = true
      # Enable Distributed tracing for in-depth monitoring of transactions in lambda (Optional)
      NEW_RELIC_DISTRIBUTED_TRACING_ENABLED = true
      # Set agent trace level logging
      NEW_RELIC_LOG_LEVEL = "debug"
      NEW_RELIC_LOG = "stderr"
      # License key
      # NEW_RELIC_LICENSE_KEY = var.newrelic_license_key
    }
  }
  # This layer includes the New Relic Lambda Extension, a sidecar process that sends telemetry,
  # as well as the New Relic Agent, and a handler wrapper that makes integration easy.
  layers = [var.newrelic_layer]
}

resource "aws_cloudwatch_log_group" "newrelic_terraform_example_log_group" {
  name = "/aws/lambda/${var.lambda_function_name}"
  # Lambda functions will auto-create their log group on first execution, but it retains logs forever, which can get expensive.
  retention_in_days = 3
}
