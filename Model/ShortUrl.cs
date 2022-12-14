using Microsoft.AspNetCore.WebUtilities;
using System;

namespace UrlShortener.Model
{
    public class ShortUrl
    {
        public int Id { get; protected set; }
        public string Url { get; protected set; }
        public string UrlChunk => WebEncoders.Base64UrlEncode(BitConverter.GetBytes(Id));

        public ShortUrl(Uri url)
        {
            Url = url.ToString();
        }
    }
}
