﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jose;
using Soa.Protocols.Communication;
using Soa.Serializer;
using Soa.Server.TransportServer;

namespace Soa.Server.OAuth.Middlewares
{
    /// <summary>
    /// support jwt middleware
    /// </summary>
    public class JwtAuthorizationMiddleware
    {
        private readonly RequestDel _next;
        private readonly JwtAuthorizationOptions _options;
        private readonly ISerializer _serializer;
        public JwtAuthorizationMiddleware(RequestDel next, JwtAuthorizationOptions options, ISerializer serializer)
        {
            _options = options;
            _serializer = serializer;
            _next = next;
        }

        public Task Invoke(RemoteCallerContext context)
        {
            // get jwt token 
            if (!string.IsNullOrEmpty(_options.TokenEndpointPath)
                && context.ServiceEntry == null
                && context.RemoteInvokeMessage.ServiceId == _options.GetServiceId())
            {
                if (_options.CheckCredential == null)
                    throw new Exception("JwtAuthorizationOptions.CheckCredential must be provided");
                JwtAuthorizationContext jwtAuthorizationContext = new JwtAuthorizationContext(_options, context.RemoteInvokeMessage);

                _options.CheckCredential(jwtAuthorizationContext);
                if (jwtAuthorizationContext.IsRejected)
                {
                    return context.Response.WriteAsync(context.TransportMessage.Id, new SoaRemoteCallResultData
                    {
                        ErrorMsg = $"{jwtAuthorizationContext.Error}, {jwtAuthorizationContext.ErrorDescription}",
                        ErrorCode = "400"
                    });
                }

                var payload = jwtAuthorizationContext.GetPayload();
                var token = JWT.Encode(payload, Encoding.ASCII.GetBytes(_options.SecretKey), JwsAlgorithm.HS256);

                var result = new ExpandoObject() as IDictionary<string, object>;
                result["access_token"] = token;
                if (_options.ValidateLifetime)
                {
                    result["expired_in"] = payload["exp"];
                }

                return context.Response.WriteAsync(context.TransportMessage.Id, new SoaRemoteCallResultData
                {
                    Result = result
                });
            }
            // jwt authentication, alse authentication the role

            if (context.ServiceEntry != null && context.ServiceEntry.Descriptor.EnableAuthorization)
            {

                try
                {
                    var payload = JWT.Decode(context.RemoteInvokeMessage.Token, Encoding.ASCII.GetBytes(
                        _options.SecretKey));
                    var payloadObj = _serializer.Deserialize(payload, typeof(IDictionary<string, object>)) as IDictionary<string, object>;
                    if (_options.ValidateLifetime)
                    {
                        //var exp = payloadObj["exp"];
                        if (payloadObj == null || ((Int64)payloadObj["exp"]).ToDate() < DateTime.Now)
                        {
                            var result = new SoaRemoteCallResultData
                            {
                                ErrorMsg = "Token is Expired",
                                ErrorCode = "401"
                            };
                            return context.Response.WriteAsync(context.TransportMessage.Id, result);

                        }
                    }
                    var serviceRoles = context.ServiceEntry.Descriptor.Roles;
                    if (!string.IsNullOrEmpty(serviceRoles))
                    {
                        var serviceRoleArr = serviceRoles.Split(',');
                        var roles = payloadObj != null && payloadObj.ContainsKey("roles") ? payloadObj["roles"] + "" : "";
                        var authorize = roles.Split(',').Any(role => serviceRoleArr.Any(x => x.Equals(role, StringComparison.InvariantCultureIgnoreCase)));
                        if (!authorize)
                        {
                            var result = new SoaRemoteCallResultData
                            {
                                ErrorMsg = "Unauthorized",
                                ErrorCode = "401"
                            };
                            return context.Response.WriteAsync(context.TransportMessage.Id, result);
                        }
                    }
                    context.RemoteInvokeMessage.Payload = new SoaPayload { Items = payloadObj };
                }
                catch (Exception ex)
                {
                    var result = new SoaRemoteCallResultData
                    {
                        ErrorMsg = $"Token is incorrect, exception is { ex.Message}",
                        ErrorCode = "401"
                    };
                    return context.Response.WriteAsync(context.TransportMessage.Id, result);
                }
                return _next(context);
            }
            // service can be annoymouse request

            return _next(context);
        }
    }
}
