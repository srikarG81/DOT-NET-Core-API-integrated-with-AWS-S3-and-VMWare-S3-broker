using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Amazon.S3;
using Amazon.S3.Transfer;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Amazon.DocSamples.S3;
using Amazon;
using Amazon.Runtime.SharedInterfaces;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Microsoft.Extensions.Configuration;
using Amazon.S3.Encryption;
using System.Security.Cryptography;
using APItoUploadDocuments.Infrastructure;
using Amazon.S3.Model;

namespace APItoUploadDocuments.Controllers
{
    [ApiController]
    [Route("document")]
    public class AttachmentController : ControllerBase
    {
        private readonly ILogger<AttachmentController> _logger;
        private IConfiguration configuration;
        private IAmazonS3 amazonS3Clinet;
        public AttachmentController(ILogger<AttachmentController> logger, IConfiguration configuration, IAmazonS3 amazonS3Clinet)
        {
            _logger = logger;
            this.configuration = configuration;
            this.amazonS3Clinet = amazonS3Clinet;
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            Stream target = new MemoryStream();
            if (file == null)
            {
                throw new Exception("File can't be null");
            }
            List<KeyValuePair<string, string>> tags = new List<KeyValuePair<string, string>>();
            await file.CopyToAsync(target);
            var uploadUtil = new UploadFileMPUHighLevelAPITest();
            uploadUtil.KeyName =file.FileName;
            await uploadUtil.UploadFileAsync(target, amazonS3Clinet, tags, configuration.GetSection("AWSConfig:bucketName").Value);
            return Ok();
        }

        [HttpGet("{key}/{version}")]
        public async Task<IActionResult> GetFileByKey(string key, string version)
        {
            var objectKeys = await amazonS3Clinet.GetObjectAsync(configuration.GetSection("AWSConfig:bucketName").Value, key, version);
              if( objectKeys.Metadata!=null)
            {
                foreach(var item in objectKeys.Metadata.Keys)
                {
                    Console.WriteLine("S3 MetaKeys {0}", item);

                    try
                    {
                        Console.WriteLine("S3 MetaKeys {0}", objectKeys.Metadata[item]);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("S3 MetaKeys exception: {0}", ex.ToString());
                    }
                }
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(objectKeys.ResponseStream);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return File(objectKeys.ResponseStream, "application/octet-stream"); ;
        }



        [HttpGet("list")]
        public async Task<IActionResult> Get()
        {
            var s3Client = AWSS3ClientFactory.Create(this.configuration);
            var objectKeys = await amazonS3Clinet.GetAllObjectKeysAsync(configuration.GetSection("AWSConfig:bucketName").Value, string.Empty, null);

            try
            {
                ListVersionsRequest request = new ListVersionsRequest();
                request.BucketName = configuration.GetSection("AWSConfig:bucketName").Value;
                var versionListing = await s3Client.ListVersionsAsync(request);
            }
            catch (Exception e)
            {
                throw;
            }
            return Ok(objectKeys);
        }



        [HttpGet("listversions/{prefix}", Name = "listversions")]
        public async Task<IActionResult> Get(string prefix)
        {
            try
            {
                ListVersionsRequest request = new ListVersionsRequest();
                if (string.IsNullOrEmpty(prefix))
                    return BadRequest("object prefix is required");
                    request.Prefix =  prefix;
                request.BucketName = configuration.GetSection("AWSConfig:bucketName").Value;
                var versionListing = await amazonS3Clinet.ListVersionsAsync(request);
                return Ok(versionListing);
            }
            catch (Exception e)
            {
                throw;
            }
           
        }

        [HttpDelete("{key}")]
        public async Task<IActionResult> Delete(string key)
        {
            var s3Client = AWSS3ClientFactory.Create(this.configuration);
            await amazonS3Clinet.DeleteAsync(configuration.GetSection("AWSConfig:bucketName").Value, key, null);
            return Ok();
        }

    }
}

namespace Amazon.DocSamples.S3
{
    public class UploadFileMPUHighLevelAPITest
    {
        private static IAmazonS3 s3Client;
        public string KeyName { get; set; }
        public async Task UploadFileAsync(Stream stream, IAmazonS3 s3Client, IEnumerable<KeyValuePair<string, string>> tags, string bucketName)
        {
            try
            {

                var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = bucketName,
                    InputStream= stream,
                    PartSize = 6291456, // 6 MB.
                    Key = KeyName,
                };

                if (tags?.Any() ?? false)
                {
                    foreach (KeyValuePair<string, string> tag in tags)
                    {
                        fileTransferUtilityRequest.Metadata.Add(tag.Key, tag.Value);
                    }
                }

                var fileTransferUtility =
                    new TransferUtility(s3Client);

                await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
                throw;
            }

        }
    }
}

