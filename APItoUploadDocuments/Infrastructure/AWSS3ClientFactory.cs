using Amazon;
using Amazon.S3.Encryption;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

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

            var rsaObject = RSA.Create();
            rsaObject.FromXmlString(configuration.GetSection("AWSConfig:privateKey").Value);
            var region = RegionEndpoint.GetBySystemName(configuration.GetSection("AWSConfig:bucketRegion").Value);
            EncryptionMaterials encryptionMaterials = new EncryptionMaterials(rsaObject);
            return new AmazonS3EncryptionClient(configuration.GetSection("AWSConfig:accessKeyId").Value, configuration.GetSection("AWSConfig:secretAccessKey").Value, region, encryptionMaterials);
        }
    }

}
