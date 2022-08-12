using System;
using System.Collections.Generic;

using Amazon.Lambda.Core;

using OpenTracing.Util;
using OpenTracing;
using NewRelic.OpenTracing.AmazonLambda;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace KmullaneyDotNet00054687
{
    public class Function
    {
        static Function()
        {
            // Register the New Relic OpenTracing LambdaTracer as the Global Tracer
            GlobalTracer.Register(LambdaTracer.Instance);
        }

        public string FunctionHandler(IDictionary<string, object> invocationEvent, ILambdaContext context)
        {
            return new TracingRequestHandler().LambdaWrapper(ActualFunctionHandler, invocationEvent, context);
        }

        public string ActualFunctionHandler(IDictionary<string, object> invocationEvent, ILambdaContext context)
        {
            ITracer tracer = GlobalTracer.Instance;

            // This is an example of a custom span. `FROM Span SELECT * WHERE name='MyDotnetSpan'` in New Relic will find this event.
            using (IScope scope = tracer.BuildSpan("MyDotnetSpan")
                    .StartActive(finishSpanOnDispose:true)) 
            {
                // Here, we add a tag to our custom span
                // XXX: Note that due to a known issue with dotnet tracer, this does not work.
                scope.Span.SetTag("zip", "zap");
            }

            // This tag gets added to the function invocation's root span, since it's active.
            // XXX: Note that due to a known issue with dotnet tracer, this results in a custom attribute on
            // the synthesized AwsLambdaInvocation event, but no custom attribute on the root Span.
            tracer.ActiveSpan.SetTag("customAttribute", "customAttributeValue");

            // As normal, anything you write to stdout ends up in CloudWatch
            Console.WriteLine("Hello, world");

            return "Success!";
        }
    }
}
