using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Encryption;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace APItoUploadDocuments.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AttachmentDetailsController : ControllerBase
    {
        private readonly ILogger<AttachmentDetailsController> _logger;
        private IConfiguration configuration;
        private IAmazonS3 amazonS3Clinet;
        public AttachmentDetailsController(ILogger<AttachmentDetailsController> logger, IConfiguration configuration, IAmazonS3 amazonS3Clinet)
        {
            _logger = logger;
            this.configuration = configuration;
            this.amazonS3Clinet = amazonS3Clinet;
        }

        [HttpGet("{key}", Name = "GetFileByKeypattern")]
        public async Task<IActionResult> GetFileByKeypattern(string key)
        { List<OrderReferenceAttachmentDetails> response = new List<OrderReferenceAttachmentDetails>();
           
                var list = await amazonS3Clinet.ListObjectsAsync(configuration.GetSection("AWSConfig:bucketName").Value, key);
                foreach (var item in list.S3Objects)
                {
                    var objectKey = item.Key;
                    var objectKeys = await amazonS3Clinet.GetObjectAsync(configuration.GetSection("AWSConfig:bucketName").Value, objectKey);
                    if (objectKeys.Metadata?.Keys.Count > 2)
                    {
                        response.Add(new OrderReferenceAttachmentDetails()
                        {
                            Date = objectKeys.Metadata["x-amz-meta-date"],
                            Ein = objectKeys.Metadata["x-amz-meta-ein"],
                            IsVoid = objectKeys.Metadata["x-amz-meta-isvoid"],
                            Reason = objectKeys.Metadata["x-amz-meta-reason"],
                            Name = objectKeys.Key.Split('_')[1]
                        });
                    }
                }

                return Ok(response);
            }
        }
    }


    public class OrderReferenceAttachmentDetails
    {
        public string Name { get; set; }

        public string Date { get; set; }

        public string IsVoid { get; set; }

        public string Ein { get; set; }

        public string Reason { get; set; }
    }
