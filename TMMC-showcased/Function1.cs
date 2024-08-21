/*
 * Author: Affan Khan
 * Last Edited: 2024-08-19
 * Description: Azure Function that counts the number of vertical black lines in a black and white image created using MS Paint.
 * This function accepts both image URLs and local file paths passed as a query parameter when running the function.
 */

using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ImageProcessingFunction
{
    public static class Function1
    {
        [Function("Function1")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("Function1");
            logger.LogInformation("C# HTTP trigger function processed a request.");

            // Retrieve the image path from the query string
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string imagePath = query["imagePath"];
            logger.LogInformation($"Image path received: {imagePath}");

            // Make sure there is a valid image URL or file path provided
            if (string.IsNullOrEmpty(imagePath))
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await response.WriteStringAsync("Error: Invalid number of arguments. Please input the file address as an argument as so: https://tmmc-test-affan-khan.azurewebsites.net/api/Function1?imagePath=online-path-to-image");
                return response;
            }

            try
            {
                Bitmap img;

                // Check if the provided path is a URL
                if (Uri.IsWellFormedUriString(imagePath, UriKind.Absolute))
                {
                    // Treat as URL and download the image
                    HttpClient client = new HttpClient();
                    logger.LogInformation("Attempting to download the image...");
                    byte[] bytes = await client.GetByteArrayAsync(imagePath);
                    logger.LogInformation("Image downloaded successfully.");

                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        img = new Bitmap(ms);
                    }
                }
                // Check if the provided path is a valid local file path
                else if (File.Exists(imagePath))
                {
                    // Treat as local file path and load the image
                    logger.LogInformation("Loading the image from a local file...");
                    img = new Bitmap(imagePath);
                    logger.LogInformation("Image loaded successfully.");
                }
                else
                {
                    throw new Exception("The provided path is neither a valid URL nor a valid local file path.");
                }

                // Convert the image to a byte matrix representing grayscale values
                byte[] matrix = ImageToByteMatrix(img);

                // Count and output the number of columns
                int columnCount = CountColumns(matrix, img.Width, img.Height);
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync($"The number of columns are {columnCount}");
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred: {ex.Message}");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                return response;
            }
        }

        /*
         * Converts a Bitmap image to a byte matrix representing grayscale values.
         * Each pixel is converted to a grayscale value using the luminosity method.
         */
        static byte[] ImageToByteMatrix(Bitmap img)
        {
            // Variables
            int index = 0;

            // Byte matrix calculated from (width x height)
            byte[] matrix = new byte[img.Width * img.Height];

            // Iterate through each pixel in the image
            for (int i = 0; i < img.Height; i++)
            {
                for (int j = 0; j < img.Width; j++)
                {
                    // Retrieve the color of each iterated pixel and convert to grayscale using the most common luminosity method
                    // Resource used: https://insider.kelbyone.com/how-photoshop-translates-rgb-color-to-gray-by-scott-valentine/ 
                    Color pixelColor = img.GetPixel(j, i);
                    byte grayscale = (byte)((pixelColor.R * 0.3) + (pixelColor.G * 0.59) + (pixelColor.B * 0.11));
                    matrix[index++] = grayscale;
                }
            }
            return matrix;
        }

        /*
         * Counts the number of vertical black lines (columns) in the middle row of the image.
         * A column is counted as "black" if it contains a black pixel and the previous column was not black.
         */
        static int CountColumns(byte[] matrix, int width, int height)
        {
            // Variables
            int count = 0;
            bool isblack = false;

            // Iterate through the columns of the middle row
            for (int i = 0; i < width; i++)
            {
                // Get index value of middle pixels
                int index = i + (height / 2 * width);

                // If the value is black and its previous value wasn't black, increment the count
                if (matrix[index] == 0 && !isblack)
                {
                    isblack = true;
                    count++;
                }

                // If the value isn't black, reset the flag
                if (matrix[index] != 0)
                {
                    isblack = false;
                }
            }
            return count;
        }
    }
}
