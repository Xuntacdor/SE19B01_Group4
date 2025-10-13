using System;
using System.Net;

namespace WebAPI.ExternalServices
{
    public static class ImageConverter
    {
        public static (string? base64, string mimeType) GetBase64FromUrl(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return (null, "image/png");

            try
            {
                // ✅ Ép Cloudinary link về PNG format
                if (imageUrl.Contains("res.cloudinary.com") && imageUrl.Contains("/image/upload/"))
                {
                    if (!imageUrl.Contains("/f_png/"))
                    {
                        imageUrl = imageUrl.Replace("/upload/", "/upload/f_png/");
                        Console.WriteLine($"[ImageHelper] Force converted Cloudinary image to PNG: {imageUrl}");
                    }
                }

                using (var webClient = new WebClient())
                {
                    webClient.Headers.Add("User-Agent", "Mozilla/5.0");
                    var bytes = webClient.DownloadData(imageUrl);

                    // ✅ MIME mặc định là PNG (đảm bảo OpenAI chấp nhận)
                    string mimeType = "image/png";
                    try
                    {
                        var headerType = webClient.ResponseHeaders["Content-Type"];
                        if (!string.IsNullOrEmpty(headerType))
                            mimeType = headerType;
                    }
                    catch { }

                    // ✅ Nếu vẫn không phải định dạng hợp lệ, fallback PNG
                    if (!mimeType.Contains("png") && !mimeType.Contains("jpeg") &&
                        !mimeType.Contains("gif") && !mimeType.Contains("webp"))
                    {
                        mimeType = "image/png";
                    }

                    string base64 = Convert.ToBase64String(bytes);
                    Console.WriteLine($"[ImageHelper] Downloaded {bytes.Length} bytes, mime={mimeType}");
                    return (base64, mimeType);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageHelper] Failed to convert image: {ex.Message}");
                return (null, "image/png");
            }
        }
    }
}
