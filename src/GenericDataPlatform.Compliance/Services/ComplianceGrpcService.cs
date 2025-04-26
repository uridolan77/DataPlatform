using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Compliance.AccessControl;
using GenericDataPlatform.Compliance.Auditing;
using GenericDataPlatform.Compliance.Models;
using GenericDataPlatform.Compliance.Privacy;
using GenericDataPlatform.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Compliance.Services
{
    /// <summary>
    /// gRPC service for compliance functionality
    /// </summary>
    public class ComplianceGrpcService : DataService.DataServiceBase
    {
        private readonly IAuditService _auditService;
        private readonly IAccessControlService _accessControlService;
        private readonly IPIIDetectionService _piiDetectionService;
        private readonly ILogger<ComplianceGrpcService> _logger;

        public ComplianceGrpcService(
            IAuditService auditService,
            IAccessControlService accessControlService,
            IPIIDetectionService piiDetectionService,
            ILogger<ComplianceGrpcService> logger)
        {
            _auditService = auditService;
            _accessControlService = accessControlService;
            _piiDetectionService = piiDetectionService;
            _logger = logger;
        }

        /// <summary>
        /// Gets data by ID with compliance checks
        /// </summary>
        public override async Task<GetDataResponse> GetData(GetDataRequest request, ServerCallContext context)
        {
            try
            {
                // Extract user ID from context
                var userId = context.GetHttpContext().User.Identity?.Name ?? "anonymous";
                
                // Check permission
                var hasPermission = await _accessControlService.CheckPermissionAsync(
                    userId,
                    request.SourceId,
                    "Read",
                    "DataSource",
                    new Dictionary<string, object>
                    {
                        ["recordId"] = request.RecordId,
                        ["clientIp"] = context.GetHttpContext().Connection.RemoteIpAddress?.ToString()
                    });
                
                if (!hasPermission)
                {
                    throw new RpcException(new Status(StatusCode.PermissionDenied, "User does not have permission to access this data"));
                }
                
                // In a real implementation, we would fetch the data from a repository
                // For this example, we'll create a mock record
                var record = new DataRecord
                {
                    Id = request.RecordId,
                    SourceId = request.SourceId
                };
                
                // Add some mock data
                record.Data.Add("name", new Value { StringValue = "John Doe" });
                record.Data.Add("email", new Value { StringValue = "john.doe@example.com" });
                record.Data.Add("phone", new Value { StringValue = "555-123-4567" });
                record.Data.Add("ssn", new Value { StringValue = "123-45-6789" });
                
                // Set timestamps
                record.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-30));
                record.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
                
                // Detect and mask PII
                var dataDict = new Dictionary<string, object>();
                foreach (var item in record.Data)
                {
                    if (item.Value.HasStringValue)
                    {
                        dataDict[item.Key] = item.Value.StringValue;
                    }
                    else if (item.Value.HasIntValue)
                    {
                        dataDict[item.Key] = item.Value.IntValue;
                    }
                    else if (item.Value.HasDoubleValue)
                    {
                        dataDict[item.Key] = item.Value.DoubleValue;
                    }
                    else if (item.Value.HasBoolValue)
                    {
                        dataDict[item.Key] = item.Value.BoolValue;
                    }
                }
                
                var maskedData = _piiDetectionService.MaskPII(dataDict);
                
                // Replace the data with masked values
                foreach (var item in maskedData)
                {
                    if (record.Data.ContainsKey(item.Key) && item.Value != null)
                    {
                        if (item.Value is string stringValue)
                        {
                            record.Data[item.Key] = new Value { StringValue = stringValue };
                        }
                        else if (item.Value is long longValue)
                        {
                            record.Data[item.Key] = new Value { IntValue = longValue };
                        }
                        else if (item.Value is int intValue)
                        {
                            record.Data[item.Key] = new Value { IntValue = intValue };
                        }
                        else if (item.Value is double doubleValue)
                        {
                            record.Data[item.Key] = new Value { DoubleValue = doubleValue };
                        }
                        else if (item.Value is bool boolValue)
                        {
                            record.Data[item.Key] = new Value { BoolValue = boolValue };
                        }
                        else
                        {
                            record.Data[item.Key] = new Value { StringValue = item.Value.ToString() };
                        }
                    }
                }
                
                return new GetDataResponse { Record = record };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data for source {SourceId}, record {RecordId}", 
                    request.SourceId, request.RecordId);
                
                throw new RpcException(new Status(StatusCode.Internal, "Error getting data"));
            }
        }

        /// <summary>
        /// Queries data with compliance checks
        /// </summary>
        public override async Task<QueryDataResponse> QueryData(QueryDataRequest request, ServerCallContext context)
        {
            try
            {
                // Extract user ID from context
                var userId = context.GetHttpContext().User.Identity?.Name ?? "anonymous";
                
                // Check permission
                var hasPermission = await _accessControlService.CheckPermissionAsync(
                    userId,
                    request.SourceId,
                    "Read",
                    "DataSource",
                    new Dictionary<string, object>
                    {
                        ["filters"] = request.Filters.Count,
                        ["clientIp"] = context.GetHttpContext().Connection.RemoteIpAddress?.ToString()
                    });
                
                if (!hasPermission)
                {
                    throw new RpcException(new Status(StatusCode.PermissionDenied, "User does not have permission to query this data"));
                }
                
                // In a real implementation, we would query the data from a repository
                // For this example, we'll create mock records
                var response = new QueryDataResponse
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalCount = 10,
                    TotalPages = 1
                };
                
                // Create mock records
                for (int i = 1; i <= 5; i++)
                {
                    var record = new DataRecord
                    {
                        Id = $"record-{i}",
                        SourceId = request.SourceId,
                        CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                        UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
                    };
                    
                    // Add some mock data
                    record.Data.Add("name", new Value { StringValue = $"User {i}" });
                    record.Data.Add("email", new Value { StringValue = $"user{i}@example.com" });
                    record.Data.Add("age", new Value { IntValue = 20 + i });
                    
                    // Detect and mask PII
                    var dataDict = new Dictionary<string, object>();
                    foreach (var item in record.Data)
                    {
                        if (item.Value.HasStringValue)
                        {
                            dataDict[item.Key] = item.Value.StringValue;
                        }
                        else if (item.Value.HasIntValue)
                        {
                            dataDict[item.Key] = item.Value.IntValue;
                        }
                    }
                    
                    var maskedData = _piiDetectionService.MaskPII(dataDict);
                    
                    // Replace the data with masked values
                    foreach (var item in maskedData)
                    {
                        if (record.Data.ContainsKey(item.Key) && item.Value != null)
                        {
                            if (item.Value is string stringValue)
                            {
                                record.Data[item.Key] = new Value { StringValue = stringValue };
                            }
                            else if (item.Value is long longValue)
                            {
                                record.Data[item.Key] = new Value { IntValue = longValue };
                            }
                            else if (item.Value is int intValue)
                            {
                                record.Data[item.Key] = new Value { IntValue = intValue };
                            }
                            else
                            {
                                record.Data[item.Key] = new Value { StringValue = item.Value.ToString() };
                            }
                        }
                    }
                    
                    response.Records.Add(record);
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying data for source {SourceId}", request.SourceId);
                throw new RpcException(new Status(StatusCode.Internal, "Error querying data"));
            }
        }

        /// <summary>
        /// Streams data changes with compliance checks
        /// </summary>
        public override async Task StreamData(StreamDataRequest request, IServerStreamWriter<DataRecord> responseStream, ServerCallContext context)
        {
            try
            {
                // Extract user ID from context
                var userId = context.GetHttpContext().User.Identity?.Name ?? "anonymous";
                
                // Check permission
                var hasPermission = await _accessControlService.CheckPermissionAsync(
                    userId,
                    request.SourceId,
                    "Read",
                    "DataSource",
                    new Dictionary<string, object>
                    {
                        ["streaming"] = true,
                        ["clientIp"] = context.GetHttpContext().Connection.RemoteIpAddress?.ToString()
                    });
                
                if (!hasPermission)
                {
                    throw new RpcException(new Status(StatusCode.PermissionDenied, "User does not have permission to stream this data"));
                }
                
                // In a real implementation, we would stream data from a repository or message queue
                // For this example, we'll send mock records periodically
                for (int i = 1; i <= 5; i++)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    
                    var record = new DataRecord
                    {
                        Id = $"stream-record-{i}",
                        SourceId = request.SourceId,
                        CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
                        UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
                    };
                    
                    // Add some mock data
                    record.Data.Add("name", new Value { StringValue = $"Stream User {i}" });
                    record.Data.Add("email", new Value { StringValue = $"stream{i}@example.com" });
                    record.Data.Add("timestamp", new Value { StringValue = DateTime.UtcNow.ToString("o") });
                    
                    // Detect and mask PII
                    var dataDict = new Dictionary<string, object>();
                    foreach (var item in record.Data)
                    {
                        if (item.Value.HasStringValue)
                        {
                            dataDict[item.Key] = item.Value.StringValue;
                        }
                    }
                    
                    var maskedData = _piiDetectionService.MaskPII(dataDict);
                    
                    // Replace the data with masked values
                    foreach (var item in maskedData)
                    {
                        if (record.Data.ContainsKey(item.Key) && item.Value != null)
                        {
                            record.Data[item.Key] = new Value { StringValue = item.Value.ToString() };
                        }
                    }
                    
                    await responseStream.WriteAsync(record);
                    await Task.Delay(1000, context.CancellationToken); // Simulate delay between records
                }
            }
            catch (Exception ex) when (ex is not RpcException && !context.CancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error streaming data for source {SourceId}", request.SourceId);
                throw new RpcException(new Status(StatusCode.Internal, "Error streaming data"));
            }
        }
    }
}
