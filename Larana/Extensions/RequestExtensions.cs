using System;
using System.Web;

namespace System.Web
{
    public static class RequestExtensions
    {
        /// <summary>
        /// Belirtilen HTTP isteğinin AJAX isteği olup olmadığını belirler.
        /// </summary>
        /// <param name="request">Kontrol edilecek HTTP isteği.</param>
        /// <returns>İstek bir AJAX isteği ise true, değilse false.</returns>
        public static bool IsAjaxRequest(this HttpRequestBase request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return true;

            return false;
        }
    }
} 