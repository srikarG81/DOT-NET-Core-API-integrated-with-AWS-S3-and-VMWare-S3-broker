using Amazon;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.Runtime;
using Amazon.S3.Encryption;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace APItoUploadDocuments.Infrastructure
{
    public static class AWSS3ClientFactory
    {
        public static AmazonS3EncryptionClient Create(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (configuration.GetSection("AWSConfig:UseKMSClient").Value == "true")
            {
                return CreateKMSKeyClient(configuration);
            }
            else
            {
                return CreateRSAKeyClient(configuration);
            }
        }

        private static AmazonS3EncryptionClient CreateRSAKeyClient(IConfiguration configuration)
        {
            var rsaObject = RSA.Create();
            rsaObject.FromXmlString(configuration.GetSection("AWSConfig:privateKey").Value);
            var region = RegionEndpoint.GetBySystemName(configuration.GetSection("AWSConfig:bucketRegion").Value);
            EncryptionMaterials encryptionMaterials = new EncryptionMaterials(rsaObject);
            return new AmazonS3EncryptionClient(configuration.GetSection("AWSConfig:accessKeyId").Value,
                configuration.GetSection("AWSConfig:secretAccessKey").Value, region, encryptionMaterials);
        }


        public static AmazonS3EncryptionClient CreateKMSKeyClient(IConfiguration configuration)
        {
            BasicAWSCredentials basicCredentials = new BasicAWSCredentials(configuration.GetSection("AWSConfig:accessKeyId").Value
                , configuration.GetSection("AWSConfig:secretAccessKey").Value);
            var kmsClient = new AmazonKeyManagementServiceClient(basicCredentials,
        RegionEndpoint.GetBySystemName(configuration.GetSection("AWSConfig:bucketRegion").Value));
            var createKeyResponse = kmsClient.CreateKeyAsync(new CreateKeyRequest()).Result;
            var kmsEncryptionContext = new Dictionary<string, string>();
            var kmsEncryptionMaterials = new EncryptionMaterials(
              createKeyResponse.KeyMetadata.KeyId);
           
            var region = RegionEndpoint.GetBySystemName(configuration.GetSection("AWSConfig:bucketRegion").Value);
            return new AmazonS3EncryptionClient(configuration.GetSection("AWSConfig:accessKeyId").Value,
                configuration.GetSection("AWSConfig:secretAccessKey").Value, region, kmsEncryptionMaterials);
        }
    }

}
