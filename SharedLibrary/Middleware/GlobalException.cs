﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace SharedLibrary.Middleware;

public class GlobalException(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Declare default variables
        string message = "sorry, internal server error occurred. Kindly try again";
        int statusCode = (int)HttpStatusCode.InternalServerError;
        string title = "Error";
        try
        {
            await next(context);
            // check if Response is Too Many Request // 429 Status Code
            if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
            {
                title = "Warning";
                message = "Too many request made.";
                statusCode = StatusCodes.Status429TooManyRequests;
                await ModifyHeader(context, title, message, statusCode);
            }
            // If Response is UnAuthorized // 401 status code
            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
            {
                title = "Alert";
                message = "You are not authorized to access.";
                statusCode = StatusCodes.Status401Unauthorized;
                await ModifyHeader(context, title, message, statusCode);
            }

            // If Response is Forbidden // 403 status code
            if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
            {
                title = "Out of Access";
                message = "You are not allowed/required to access.";
                statusCode = StatusCodes.Status403Forbidden;
                await ModifyHeader(context, title, message, statusCode);
            }
        }
        catch (Exception ex)
        {
            // Log original Exceptions / File, Debugger, Console
            LogException.LogExceptions(ex);
            // check if Exception is TimeOut / 408 Request Timeout
            if (ex is TaskCanceledException || ex is TimeoutException)
            {
                title = "Out of time";
                message = "Request timeout ... Try again!";
                statusCode = StatusCodes.Status408RequestTimeout;
            }
            // If Exception is caught
            // if none of the Exceptions then do the default
            await ModifyHeader(context, title, message, statusCode);
        }
    }
    private static async Task ModifyHeader(HttpContext context, string title, string message, int statusCode)
    {
        // display scare-free message to client
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails()
        {
            Detail = message,
            Status = statusCode,
            Title = title
        }), CancellationToken.None);
        return;
    }
}