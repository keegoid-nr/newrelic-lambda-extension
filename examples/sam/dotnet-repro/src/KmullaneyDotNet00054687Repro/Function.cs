using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

using OpenTracing.Util;
using OpenTracing;
using NewRelic.OpenTracing.AmazonLambda;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
// [assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace KmullaneyDotNet00054687Repro
{
	[ExcludeFromCodeCoverage]
    public class APIGatewayLambdaEntryPoint<TStartup> : Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
         where TStartup : class
    {
	    static APIGatewayLambdaEntryPoint()
	    {
		    // Register the New Relic OpenTracing LambdaTracer as the Global Tracer
		    GlobalTracer.Register(LambdaTracer.Instance);
	    }

	    protected override void Init(IHostBuilder builder)
            => builder.UseSerilog();

        protected override void Init(IWebHostBuilder builder)
            => builder
                .UseStartup<TStartup>();

        public override async Task<APIGatewayProxyResponse> FunctionHandlerAsync(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
        {
	        // Resource will be null/empty when invoked directly,
	        // e.g. a CloudWatch scheduled keep-alive invocation
	        if (!string.IsNullOrWhiteSpace(request.Resource))
	        {
		        return await new TracingRequestHandler().LambdaWrapper(ActualFunctionHandlerAsync, request, lambdaContext);
	        }

	        Console.WriteLine("Ping");
	        return new APIGatewayProxyResponse { StatusCode = 200, Body = "" };
        }

        private async Task<APIGatewayProxyResponse> ActualFunctionHandlerAsync(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
        {
            // Resource will be null/empty when invoked directly,
            // e.g. a CloudWatch scheduled keep-alive invocation
            if (!string.IsNullOrWhiteSpace(request.Resource))
            {
	            return await base.FunctionHandlerAsync(request, lambdaContext);
            }

            Console.WriteLine("Ping");
            return new APIGatewayProxyResponse { StatusCode = 200, Body = "" };
        }
    }
}
