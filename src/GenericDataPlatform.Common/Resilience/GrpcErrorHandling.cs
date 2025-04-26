using System;
using System.Net;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Errors;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Common.Resilience
{
    /// <summary>
    /// Utility for handling gRPC errors consistently
    /// </summary>
    public static class GrpcErrorHandling
    {
        /// <summary>
        /// Maps gRPC status codes to HTTP status codes
        /// </summary>
        public static HttpStatusCode MapStatusCodeToHttpStatus(StatusCode statusCode)
        {
            return statusCode switch
            {
                StatusCode.OK => HttpStatusCode.OK,
                StatusCode.Cancelled => HttpStatusCode.RequestTimeout,
                StatusCode.Unknown => HttpStatusCode.InternalServerError,
                StatusCode.InvalidArgument => HttpStatusCode.BadRequest,
                StatusCode.DeadlineExceeded => HttpStatusCode.GatewayTimeout,
                StatusCode.NotFound => HttpStatusCode.NotFound,
                StatusCode.AlreadyExists => HttpStatusCode.Conflict,
                StatusCode.PermissionDenied => HttpStatusCode.Forbidden,
                StatusCode.ResourceExhausted => HttpStatusCode.TooManyRequests,
                StatusCode.FailedPrecondition => HttpStatusCode.BadRequest,
                StatusCode.Aborted => HttpStatusCode.Conflict,
                StatusCode.OutOfRange => HttpStatusCode.BadRequest,
                StatusCode.Unimplemented => HttpStatusCode.NotImplemented,
                StatusCode.Internal => HttpStatusCode.InternalServerError,
                StatusCode.Unavailable => HttpStatusCode.ServiceUnavailable,
                StatusCode.DataLoss => HttpStatusCode.InternalServerError,
                StatusCode.Unauthenticated => HttpStatusCode.Unauthorized,
                _ => HttpStatusCode.InternalServerError
            };
        }

        /// <summary>
        /// Maps gRPC exceptions to application exceptions
        /// </summary>
        public static Exception MapRpcExceptionToApplicationException(RpcException ex)
        {
            var httpStatus = MapStatusCodeToHttpStatus(ex.StatusCode);

            return ex.StatusCode switch
            {
                StatusCode.InvalidArgument => new ApiException(httpStatus, "InvalidArgument", ex.Status.Detail, ex),
                StatusCode.NotFound => new ApiException(httpStatus, "NotFound", ex.Status.Detail, ex),
                StatusCode.AlreadyExists => new ApiException(httpStatus, "AlreadyExists", ex.Status.Detail, ex),
                StatusCode.PermissionDenied => new ApiException(httpStatus, "PermissionDenied", ex.Status.Detail, ex),
                StatusCode.Unauthenticated => new ApiException(httpStatus, "Unauthenticated", ex.Status.Detail, ex),
                _ => new ApiException(httpStatus, "GrpcError", ex.Status.Detail, ex)
            };
        }

        /// <summary>
        /// Executes a gRPC call and handles exceptions consistently
        /// </summary>
        public static async Task<T> ExecuteGrpcCallAsync<T>(
            Func<Task<T>> grpcCall,
            ILogger logger,
            string serviceName,
            string methodName)
        {
            try
            {
                return await grpcCall();
            }
            catch (RpcException ex)
            {
                logger.LogError(ex, "gRPC error calling {Service}.{Method}. Status: {Status}, Detail: {Detail}",
                    serviceName, methodName, ex.StatusCode, ex.Status.Detail);

                throw MapRpcExceptionToApplicationException(ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error calling gRPC service {Service}.{Method}",
                    serviceName, methodName);

                throw new ApiException(HttpStatusCode.InternalServerError, "UnexpectedError",
                    $"Unexpected error calling {serviceName}.{methodName}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extension method to handle gRPC exceptions and convert to application exceptions
        /// </summary>
        public static async Task<T> HandleGrpcExceptionsAsync<T>(
            this Task<T> task,
            ILogger logger,
            string serviceName,
            string methodName)
        {
            try
            {
                return await task;
            }
            catch (RpcException ex)
            {
                logger.LogError(ex, "gRPC error calling {Service}.{Method}. Status: {Status}, Detail: {Detail}",
                    serviceName, methodName, ex.StatusCode, ex.Status.Detail);

                throw MapRpcExceptionToApplicationException(ex);
            }
            catch (Exception ex) when (!(ex is ApiException))
            {
                logger.LogError(ex, "Unexpected error calling gRPC service {Service}.{Method}",
                    serviceName, methodName);

                throw new ApiException(HttpStatusCode.InternalServerError, "UnexpectedError",
                    $"Unexpected error calling {serviceName}.{methodName}: {ex.Message}", ex);
            }
        }
    }
}
