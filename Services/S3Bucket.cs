using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System.IO;
using System.Windows.Navigation;

namespace CameraUploaderApp.Services
{
    public class S3Bucket
    {
        private readonly IAmazonS3 _amazonS3;
        private RegionEndpoint tokyoRegion = RegionEndpoint.APNortheast1;

        public S3Bucket() 
        {
            IAmazonS3 client = new AmazonS3Client(tokyoRegion);
            _amazonS3 = client;
        }

        public async Task<IAmazonS3> GetClient()
        {
            return _amazonS3;
        }

        /// <summary>
        /// すべてのバケット取得
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetAllBucketsAsync()
        {
            var response = await _amazonS3.ListBucketsAsync();
            return response.Buckets.Select(b => b.BucketName).ToList();
        }

        /// <summary>
        /// バケット内のオブジェクト情報取得
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        public async Task<List<string>> GetFilesInBucketAsync(string bucketName)
        {
            var request = new ListObjectsV2Request { BucketName = bucketName };
            var response = await _amazonS3.ListObjectsV2Async(request);
            return response.S3Objects.Select(obj => obj.Key).ToList();
        }

        /// <summary>
        /// Shows how to upload a file from the local computer to an Amazon S3
        /// bucket.
        /// </summary>
        /// <param name="client">An initialized Amazon S3 client object.</param>
        /// <param name="bucketName">The Amazon S3 bucket to which the object
        /// will be uploaded.</param>
        /// <param name="objectName">The object to upload.</param>
        /// <param name="filePath">The path, including file name, of the object
        /// on the local computer to upload.</param>
        /// <param name="progress">progressver</param>
        /// <returns>A boolean value indicating the success or failure of the
        /// upload procedure.</returns>
        public async Task<bool> UploadFileAsync(
            string bucketName,
            string objectName,
            string filePath,
            Action<int> progress)
        {
            try
            {
                // プログレスバー仕様の処理
                var request = new TransferUtilityUploadRequest
                {
                    BucketName = bucketName,
                    Key = objectName,
                    FilePath = filePath,
                };

                request.UploadProgressEvent += (sender, e) =>
                {
                    int percent = (int)(e.TransferredBytes / e.TotalBytes * 100);
                    progress(percent);
                };

                var transferUtility = new TransferUtility(_amazonS3);
                await transferUtility.UploadAsync(request);
                return true;

            }
            catch (AmazonS3Exception ex)
            {
                //Console.WriteLine($"Could not upload {objectName} to {bucketName}: '{ex.Message}'");
                return false;
            }
        }


        /// <summary>
        /// Shows how to download an object from an Amazon S3 bucket to the
        /// local computer.
        /// </summary>
        /// <param name="client">An initialized Amazon S3 client object.</param>
        /// <param name="bucketName">The name of the bucket where the object is
        /// currently stored.</param>
        /// <param name="objectName">The name of the object to download.</param>
        /// <param name="filePath">The path, including filename, where the
        /// downloaded object will be stored.</param>
        /// <returns>A boolean value indicating the success or failure of the
        /// download process.</returns>
        public async Task<bool> DownloadObjectFromBucketAsync(
            //IAmazonS3 client,
            string bucketName,
            string objectName,
            string filePath,
            Action<int> progressCallback)
        {
            try
            {
                // プログレスバー仕様の処理
                // Create a GetObject request
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectName,
                };

                IAmazonS3 client = await GetClient();
                // Issue request and remember to dispose of the response
                using GetObjectResponse response = await client.GetObjectAsync(request);
                long totalBytes = response.ContentLength;

                // ダウンロードストリームの読み取りを監視することでプログレスバーを表示する
                const int bufferSize = 81920; // 80KB
                var buffer = new byte[bufferSize];
                long totalRead = 0;

                // TODO:AWSのオブジェクトキーが/ありなので含まれている場合例外になる
                string fullPath = string.Format(@"{0}\\{1}", filePath, objectName);

                using var stream = response.ResponseStream;
                using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.ReadWrite);

                int bytesRead;
                while((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    int progress = (int)(totalRead / totalBytes * 100);
                    progressCallback(progress);    
                }


                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;

            }
            catch (AmazonS3Exception ex)
            {
                //Console.WriteLine($"Error saving {objectName}: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// This method creates and then deletes a versioned object.
        /// </summary>
        /// <param name="client">The initialized Amazon S3 client object used to
        /// create and delete the object.</param>
        /// <param name="bucketName">The name of the Amazon S3 bucket where the
        /// object will be created and deleted.</param>
        /// <param name="keyName">The key name of the object to create.</param>
        public async Task CreateAndDeleteObjectVersionAsync(
            //IAmazonS3 client,
            string bucketName,
            string keyName)
        {
            try
            {
                //// Add a sample object.
                //string versionID = await PutAnObject(client, bucketName, keyName);

                IAmazonS3 client = await GetClient();

                DeleteObjectRequest request = new DeleteObjectRequest()
                {
                    BucketName = bucketName,
                    Key = keyName,
                    //VersionId = versionID,　// バージョニング非対応の場合
                };


                await client.DeleteObjectAsync(request);
            }
            catch (AmazonS3Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
            }
        }


        ///// <summary>
        ///// Shows how to create a new Amazon S3 bucket.
        ///// </summary>
        ///// <param name="client">An initialized Amazon S3 client object.</param>
        ///// <param name="bucketName">The name of the bucket to create.</param>
        ///// <returns>A boolean value representing the success or failure of
        ///// the bucket creation process.</returns>
        //public static async Task<bool> CreateBucketAsync(IAmazonS3 client, string bucketName)
        //{
        //    try
        //    {
        //        var request = new PutBucketRequest
        //        {
        //            BucketName = bucketName,
        //            UseClientRegion = true,
        //        };

        //        var response = await client.PutBucketAsync(request);
        //        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        //    }
        //    catch (AmazonS3Exception ex)
        //    {
        //        Console.WriteLine($"Error creating bucket: '{ex.Message}'");
        //        return false;
        //    }
        //}


        ///// <summary>
        ///// Shows how to upload a file from the local computer to an Amazon S3
        ///// bucket.
        ///// </summary>
        ///// <param name="client">An initialized Amazon S3 client object.</param>
        ///// <param name="bucketName">The Amazon S3 bucket to which the object
        ///// will be uploaded.</param>
        ///// <param name="objectName">The object to upload.</param>
        ///// <param name="filePath">The path, including file name, of the object
        ///// on the local computer to upload.</param>
        ///// <returns>A boolean value indicating the success or failure of the
        ///// upload procedure.</returns>
        //public static async Task<bool> UploadFileAsync(
        //    IAmazonS3 client,
        //    string bucketName,
        //    string objectName,
        //    string filePath)
        //{
        //    try
        //    {
        //        var request = new PutObjectRequest
        //        {
        //            BucketName = bucketName,
        //            Key = objectName,
        //            FilePath = filePath,
        //        };

        //        await client.PutObjectAsync(request);
        //        Console.WriteLine($"Successfully uploaded {objectName} to {bucketName}.");
        //        return true;
        //    }
        //    catch (AmazonS3Exception ex)
        //    {
        //        Console.WriteLine($"Could not upload {objectName} to {bucketName}: '{ex.Message}'");
        //        return false;
        //    }
        //}

        ///// <summary>
        ///// Shows how to download an object from an Amazon S3 bucket to the
        ///// local computer.
        ///// </summary>
        ///// <param name="client">An initialized Amazon S3 client object.</param>
        ///// <param name="bucketName">The name of the bucket where the object is
        ///// currently stored.</param>
        ///// <param name="objectName">The name of the object to download.</param>
        ///// <param name="filePath">The path, including filename, where the
        ///// downloaded object will be stored.</param>
        ///// <returns>A boolean value indicating the success or failure of the
        ///// download process.</returns>
        //public static async Task<bool> DownloadObjectFromBucketAsync(
        //    IAmazonS3 client,
        //    string bucketName,
        //    string objectName,
        //    string filePath)
        //{
        //    // Create a GetObject request
        //    var request = new GetObjectRequest
        //    {
        //        BucketName = bucketName,
        //        Key = objectName,
        //    };

        //    // Issue request and remember to dispose of the response
        //    using GetObjectResponse response = await client.GetObjectAsync(request);

        //    try
        //    {
        //        // Save object to local file
        //        await response.WriteResponseStreamToFileAsync($"{filePath}\\{objectName}", true, CancellationToken.None);
        //        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        //    }
        //    catch (AmazonS3Exception ex)
        //    {
        //        Console.WriteLine($"Error saving {objectName}: {ex.Message}");
        //        return false;
        //    }
        //}

        ///// <summary>
        ///// Copies an object in an Amazon S3 bucket to a folder within the
        ///// same bucket.
        ///// </summary>
        ///// <param name="client">An initialized Amazon S3 client object.</param>
        ///// <param name="bucketName">The name of the Amazon S3 bucket where the
        ///// object to copy is located.</param>
        ///// <param name="objectName">The object to be copied.</param>
        ///// <param name="folderName">The folder to which the object will
        ///// be copied.</param>
        ///// <returns>A boolean value that indicates the success or failure of
        ///// the copy operation.</returns>
        //public static async Task<bool> CopyObjectInBucketAsync(
        //    IAmazonS3 client,
        //    string bucketName,
        //    string objectName,
        //    string folderName)
        //{
        //    try
        //    {
        //        var request = new CopyObjectRequest
        //        {
        //            SourceBucket = bucketName,
        //            SourceKey = objectName,
        //            DestinationBucket = bucketName,
        //            DestinationKey = $"{folderName}\\{objectName}",
        //        };
        //        var response = await client.CopyObjectAsync(request);
        //        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        //    }
        //    catch (AmazonS3Exception ex)
        //    {
        //        Console.WriteLine($"Error copying object: '{ex.Message}'");
        //        return false;
        //    }
        //}

        ///// <summary>
        ///// Shows how to list the objects in an Amazon S3 bucket.
        ///// </summary>
        ///// <param name="client">An initialized Amazon S3 client object.</param>
        ///// <param name="bucketName">The name of the bucket for which to list
        ///// the contents.</param>
        ///// <returns>A boolean value indicating the success or failure of the
        ///// copy operation.</returns>
        //public static async Task<bool> ListBucketContentsAsync(IAmazonS3 client, string bucketName)
        //{
        //    try
        //    {
        //        var request = new ListObjectsV2Request
        //        {
        //            BucketName = bucketName,
        //            MaxKeys = 5,
        //        };

        //        Console.WriteLine("--------------------------------------");
        //        Console.WriteLine($"Listing the contents of {bucketName}:");
        //        Console.WriteLine("--------------------------------------");

        //        ListObjectsV2Response response;

        //        do
        //        {
        //            response = await client.ListObjectsV2Async(request);

        //            response.S3Objects
        //                .ForEach(obj => Console.WriteLine($"{obj.Key,-35}{obj.LastModified?.ToShortDateString(),10}{obj.Size,10}"));

        //            // If the response is truncated, set the request ContinuationToken
        //            // from the NextContinuationToken property of the response.
        //            request.ContinuationToken = response.NextContinuationToken;
        //        }
        //        while (response.IsTruncated ?? false);

        //        return true;
        //    }
        //    catch (AmazonS3Exception ex)
        //    {
        //        Console.WriteLine($"Error encountered on server. Message:'{ex.Message}' getting list of objects.");
        //        return false;
        //    }
        //}


        ///// <summary>
        ///// Delete all of the objects stored in an existing Amazon S3 bucket.
        ///// </summary>
        ///// <param name="client">An initialized Amazon S3 client object.</param>
        ///// <param name="bucketName">The name of the bucket from which the
        ///// contents will be deleted.</param>
        ///// <returns>A boolean value that represents the success or failure of
        ///// deleting all of the objects in the bucket.</returns>
        //public static async Task<bool> DeleteBucketContentsAsync(IAmazonS3 client, string bucketName)
        //{
        //    // Iterate over the contents of the bucket and delete all objects.
        //    var request = new ListObjectsV2Request
        //    {
        //        BucketName = bucketName,
        //    };

        //    try
        //    {
        //        ListObjectsV2Response response;

        //        do
        //        {
        //            response = await client.ListObjectsV2Async(request);
        //            response.S3Objects
        //                .ForEach(async obj => await client.DeleteObjectAsync(bucketName, obj.Key));

        //            // If the response is truncated, set the request ContinuationToken
        //            // from the NextContinuationToken property of the response.
        //            request.ContinuationToken = response.NextContinuationToken;
        //        }
        //        while (response.IsTruncated ?? false);

        //        return true;
        //    }
        //    catch (AmazonS3Exception ex)
        //    {
        //        Console.WriteLine($"Error deleting objects: {ex.Message}");
        //        return false;
        //    }
        //}

        ///// <summary>
        ///// Shows how to delete an Amazon S3 bucket.
        ///// </summary>
        ///// <param name="client">An initialized Amazon S3 client object.</param>
        ///// <param name="bucketName">The name of the Amazon S3 bucket to delete.</param>
        ///// <returns>A boolean value that represents the success or failure of
        ///// the delete operation.</returns>
        //public static async Task<bool> DeleteBucketAsync(IAmazonS3 client, string bucketName)
        //{
        //    try
        //    {
        //        var request = new DeleteBucketRequest { BucketName = bucketName, };

        //        await client.DeleteBucketAsync(request);
        //        return true;
        //    }
        //    catch (AmazonS3Exception ex)
        //    {
        //        Console.WriteLine($"Error deleting bucket: {ex.Message}");
        //        return false;
        //    }
        //}

    }
}
