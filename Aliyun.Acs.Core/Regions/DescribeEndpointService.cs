﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aliyun.Acs.Core.Regions.Location.Model;
using Aliyun.Acs.Core.Auth;
using Aliyun.Acs.Core.Http;
using Aliyun.Acs.Core.Regions.Location;
using Aliyun.Acs.Core.Transform;
using Aliyun.Acs.Core.Reader;
using Aliyun.Acs.Core.Exceptions;
using System.Threading.Tasks;

namespace Aliyun.Acs.Core.Regions
{
    class DescribeEndpointService
    {
        private static String DEFAULT_ENDPOINT_TYPE = "openAPI";

        private ISigner signer = new ShaHmac1();

        private bool IsEmpty(String str) => (str == null) || (str.Length == 0);
        

        public DescribeEndpointResponse DescribeEndpoint(String regionId, String locationProduct,
                                                         Credential credential, LocationConfig locationConfig)
        {
            if (IsEmpty(locationProduct))
            {
                return null;
            }

            var request = new DescribeEndpointRequest
            {
                AcceptFormat = FormatType.JSON,
                Id = regionId,
                RegionId = locationConfig.RegionId,
                LocationProduct = locationProduct,
                EndpointType = DEFAULT_ENDPOINT_TYPE
            };

            ProductDomain domain = new ProductDomain(locationConfig.Product, locationConfig.Endpoint);

            try
            {
                HttpRequest httpRequest = request.SignRequest(signer, credential, FormatType.JSON, domain);
                HttpResponse httpResponse = HttpResponse.GetResponse(httpRequest);
                if (httpResponse.IsSuccess())
                {
                    String data = System.Text.Encoding.UTF8.GetString(httpResponse.Content);
                    DescribeEndpointResponse response = GetEndpointResponse(data, DEFAULT_ENDPOINT_TYPE);
                    if (null == response || IsEmpty(response.Endpoint))
                    {
                        return null;
                    }
                    return response;
                }
                AcsError error = ReadError(httpResponse, FormatType.JSON);
                if (500 <= httpResponse.Status)
                {
                    Console.WriteLine("Invoke_Error, requestId:" + error.RequestId + "; code:" + error.ErrorCode
                            + "; Msg" + error.ErrorMessage);
                    return null;
                }
                Console.WriteLine("Invoke_Error, requestId:" + error.RequestId + "; code:" + error.ErrorCode
                        + "; Msg" + error.ErrorMessage);
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine("Invoke Remote Error,Msg" + e.Message);
                return null;
            }
        }

        public async Task<DescribeEndpointResponse> DescribeEndpointAsync(String regionId, String locationProduct,
                                                         Credential credential, LocationConfig locationConfig)
        {
            if (IsEmpty(locationProduct))
            {
                return null;
            }

            var request = new DescribeEndpointRequest
            {
                AcceptFormat = FormatType.JSON,
                Id = regionId,
                RegionId = locationConfig.RegionId,
                LocationProduct = locationProduct,
                EndpointType = DEFAULT_ENDPOINT_TYPE
            };

            var domain = new ProductDomain(locationConfig.Product, locationConfig.Endpoint);

            try
            {
                var httpRequest = request.SignRequest(signer, credential, FormatType.JSON, domain);
                var httpResponse = await HttpResponse.GetResponseAsync(httpRequest);
                if (httpResponse.IsSuccess())
                {
                    var data = Encoding.UTF8.GetString(httpResponse.Content);
                    DescribeEndpointResponse response = GetEndpointResponse(data, DEFAULT_ENDPOINT_TYPE);
                    if (null == response || IsEmpty(response.Endpoint))
                    {
                        return null;
                    }

                    return response;
                }

                var error = ReadError(httpResponse, FormatType.JSON);
                if (500 <= httpResponse.Status)
                {
                    Console.WriteLine("Invoke_Error, requestId:" + error.RequestId + "; code:" + error.ErrorCode
                            + "; Msg" + error.ErrorMessage);
                    return null;
                }

                Console.WriteLine("Invoke_Error, requestId:" + error.RequestId + "; code:" + error.ErrorCode
                        + "; Msg" + error.ErrorMessage);

                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine("Invoke Remote Error,Msg" + e.Message);
                return null;
            }
        }

        private DescribeEndpointResponse GetEndpointResponse(String data, String endpointType)
        {
            IReader reader = ReaderFactory.CreateInstance(FormatType.JSON);
            var context = new UnmarshallerContext
            {
                ResponseDictionary = reader.Read(data, "DescribeEndpointsResponse")
            };

            int endpointsLength = context.Length("DescribeEndpointsResponse.Endpoints.Length");
            for (int i = 0; i < endpointsLength; i++)
            {
                if (endpointType.Equals(context
                        .StringValue("DescribeEndpointsResponse.Endpoints[" + i + "].Type")))
                {
                    var response = new DescribeEndpointResponse
                    {
                        RequestId = context.StringValue("DescribeEndpointsResponse.RequestId"),
                        Product = context.StringValue("DescribeEndpointsResponse.Endpoints[" + i + "].SerivceCode"),
                        Endpoint = context.StringValue("DescribeEndpointsResponse.Endpoints[" + i + "].Endpoint"),
                        RegionId = context.StringValue("DescribeEndpointsResponse.Endpoints[" + i + "].Id")
                    };

                    return response;
                }
            }
            return null;
        }

        private AcsError ReadError(HttpResponse httpResponse, FormatType format)
        {
            String responseEndpoint = "Error";
            IReader reader = ReaderFactory.CreateInstance(format);
            UnmarshallerContext context = new UnmarshallerContext();
            String stringContent = GetResponseContent(httpResponse);
            context.ResponseDictionary = reader.Read(stringContent, responseEndpoint);

            return AcsErrorUnmarshaller.Unmarshall(context);
        }

        private String GetResponseContent(HttpResponse httpResponse)
        {
            String stringContent = null;
            try
            {
                if (null == httpResponse.Encoding)
                {
                    stringContent = System.Text.Encoding.Default.GetString(httpResponse.Content);
                }
                else
                {
                    stringContent = System.Text.Encoding.GetEncoding(httpResponse.Encoding).GetString(httpResponse.Content);
                }
            }
            catch (Exception exp)
            {
                throw new ClientException("SDK.UnsupportedEncoding", "Can not parse response due to un supported encoding." + exp.Message);
            }
            return stringContent;
        }
    }
}
