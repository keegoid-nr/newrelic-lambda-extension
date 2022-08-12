#!/bin/bash

accountId="${1}"
licenseKey="${2}"
region="${3}"

echo "accountId set to ${accountId}"
echo "licenseKey set to ${licenseKey}"
echo "region set to ${region}"

sam build

bucket="kmullaney-dotnet-00054687-${region}-${accountId}"

aws s3 mb --region "${region}" "s3://${bucket}"

sam package --region "${region}" --s3-bucket "${bucket}" --output-template-file packaged.yaml

aws cloudformation deploy \
	--region "${region}" \
	--template-file packaged.yaml \
	--stack-name "${bucket}" \
	--capabilities CAPABILITY_IAM \
	--parameter-overrides "NRAccountId=${accountId}" "NRLicenseKey=${licenseKey}"
