﻿using Microsoft.AspNetCore.StaticFiles;
using System;
using System.IO;
using System.Text;

namespace Microsoft.MobileBlazorBindings.HostingNew
{
    /// <summary>
    /// Platform-agnostic parts of BlazorWebView
    /// </summary>
    public sealed class BlazorWebViewCore : IDisposable
    {
        private static readonly FileExtensionContentTypeProvider FileExtensionContentTypeProvider = new();
        private bool _hasStarted;

        public string ContentHost { get; private set; }
        public string ContentRootPath { get; private set; }
        public string HostPageRelativeUrl { get; private set; }

        public event EventHandler<Uri> OnNavigate;

        public BlazorWebViewCore(string hostPageFilePath)
        {
            var hostPageAbsolute = Path.GetFullPath(hostPageFilePath);
            ContentHost = "0.0.0.0";
            ContentRootPath = Path.GetDirectoryName(hostPageAbsolute);
            HostPageRelativeUrl = Path.GetRelativePath(ContentRootPath, hostPageAbsolute).Replace(Path.DirectorySeparatorChar, '/');
        }

        public void Start()
        {
            if (_hasStarted)
            {
                throw new InvalidOperationException("Can only start once");
            }

            _hasStarted = true;
            var startUri = new Uri(new Uri($"https://{ContentHost}/"), HostPageRelativeUrl);
            OnNavigate?.Invoke(this, startUri);
        }

        public bool TryGetResponseContent(Uri requestUri, out int statusCode, out string statusMessage, out Stream content, out string headers)
        {
            if (requestUri is null)
            {
                throw new ArgumentNullException(nameof(requestUri));
            }

            if (string.Equals(requestUri.Host, ContentHost, StringComparison.Ordinal))
            {
                var filePath = Path.GetFullPath(Path.Combine(ContentRootPath, requestUri.GetComponents(UriComponents.Path, UriFormat.Unescaped)));
                if (filePath.StartsWith(ContentRootPath, StringComparison.Ordinal)
                    && File.Exists(filePath))
                {
                    var responseContentType = FileExtensionContentTypeProvider.TryGetContentType(filePath, out var matchedContentType)
                        ? matchedContentType
                        : "application/octet-stream";

                    statusCode = 200;
                    statusMessage = "OK";
                    headers = $"Content-Type: {responseContentType}{Environment.NewLine}Cache-Control: no-cache, max-age=0, must-revalidate, no-store";
                    content = File.OpenRead(filePath);
                }
                else
                {
                    // Always provide a response to requests on the virtual domain, even if no file matches
                    var message = $"There is no file at {filePath}";
                    statusCode = 404;
                    statusMessage = "Not found";
                    headers = "Content-Type: text/plain";
#pragma warning disable CA2000 // Dispose objects before losing scope
                    content = new MemoryStream(Encoding.UTF8.GetBytes(message));
#pragma warning restore CA2000 // Dispose objects before losing scope
                }

                return true;
            }

            statusCode = default;
            statusMessage = default;
            headers = default;
            content = default;
            return false;
        }

        public void Dispose()
        {
            // Nothing needed yet
        }
    }
}