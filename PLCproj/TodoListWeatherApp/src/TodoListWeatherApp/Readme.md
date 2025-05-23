# Giraffe Serverless Application

This project shows how to run a [Giraffe](https://github.com/giraffe-fsharp/Giraffe) project as an AWS Lambda exposed through Amazon API Gateway. The NuGet package [Amazon.Lambda.AspNetCoreServer](https://www.nuget.org/packages/Amazon.Lambda.AspNetCoreServer) contains a Lambda function that is used to translate requests from API Gateway into the ASP.NET Core framework and then the responses from ASP.NET Core back to API Gateway.


### Project Files ###

* serverless.template - an AWS CloudFormation Serverless Application Model template file for declaring your Serverless functions and other AWS resources
* aws-lambda-tools-defaults.json - default argument settings for use with Visual Studio and command line deployment tools for AWS
* Setup.fs - Code file that contains the bootstrap for configuring ASP.NET Core and Giraffe. It contains a main function for local development and the LambdaEntryPoint type for executing from Lambda. The LambdaEntryPoint type inherits from **Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction** which contains the logic of converting requests and response back and forth between API Gateway and ASP.NET Core.
* AppHandlers.fs - Code file that defines the HTTP handler functions and routing function.
* web.config - used for local development.

You may also have a test project depending on the options selected.

### Configuring for API Gateway HTTP API ###

API Gateway supports the original REST API and the new HTTP API. In addition HTTP API supports 2 different
payload formats. When using the 2.0 format the base type of `LambdaEntryPoint` must be `Amazon.Lambda.AspNetCoreServer.APIGatewayHttpApiV2ProxyFunction`.
For the 1.0 payload format the base type is the same as REST API which is `Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction`.
**Note:** when using the `AWS::Serverless::Function` CloudFormation resource with an event type of `HttpApi` the default payload
format is 2.0 so the base type of `LambdaEntryPoint` must be `Amazon.Lambda.AspNetCoreServer.APIGatewayHttpApiV2ProxyFunction`.

### Configuring for Application Load Balancer ###

To configure this project to handle requests from an Application Load Balancer instead of API Gateway change
the base type of `LambdaEntryPoint` from `Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction` to 
`Amazon.Lambda.AspNetCoreServer.ApplicationLoadBalancerFunction`.

## Here are some steps to follow from Visual Studio:

To deploy your Serverless application, right click the project in Solution Explorer and select *Publish to AWS Lambda*.

To view your deployed application open the Stack View window by double-clicking the stack name shown beneath the AWS CloudFormation node in the AWS Explorer tree. The Stack View also displays the root URL to your published application.

## Here are some steps to follow to get started from the command line:

Once you have edited your template and code you can deploy your application using the [Amazon.Lambda.Tools Global Tool](https://github.com/aws/aws-extensions-for-dotnet-cli#aws-lambda-amazonlambdatools) from the command line.

Install Amazon.Lambda.Tools Global Tools if not already installed.
```
    dotnet tool install -g Amazon.Lambda.Tools
```

If already installed check if new version is available.
```
    dotnet tool update -g Amazon.Lambda.Tools
```

Execute unit tests
```
    cd "TodoListWeatherApp/test/TodoListWeatherApp.Tests"
    dotnet test
```

Deploy application
```
    cd "TodoListWeatherApp/src/TodoListWeatherApp"
    dotnet lambda deploy-serverless
```
