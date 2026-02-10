using System;
using Blazing.Mvvm.ComponentModel;

namespace ToolBox.Components.Pages
{
    public class Base64ImageVM : ViewModelBase
    {
        private string _base64Input ="data:image/jpeg;base64,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";
        public string Base64Input
        {
            get => _base64Input;
            set { SetProperty(ref _base64Input, value); }
        }

        private string _imageUrl = string.Empty;
        public string ImageUrl
        {
            get => _imageUrl;
            set { SetProperty(ref _imageUrl, value); }
        }
        
        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { SetProperty(ref _errorMessage, value); }
        }

        public void UpdateImage()
        {
            ErrorMessage = string.Empty;
            ImageUrl = string.Empty;
            if (string.IsNullOrWhiteSpace(Base64Input))
            {
                return;
            }

            try
            {
                var input = Base64Input.Trim();
                if (TryParseDataUrl(input, out var dataUrl, out var base64Part, out var mimeType))
                {
                    if (!IsValidBase64(base64Part))
                    {
                        ErrorMessage = "无效的 Base64 字符串。";
                        return;
                    }

                    ImageUrl = dataUrl;
                    return;
                }

                if (!IsValidBase64(input))
                {
                    ErrorMessage = "无效的 Base64 字符串。";
                    return;
                }

                var imageType = GetImageType(input);
                if (string.IsNullOrEmpty(imageType))
                {
                     ErrorMessage = "无法识别图片类型。";
                    return;
                }

                ImageUrl = $"data:{imageType};base64,{input}";
            }
            catch (FormatException)
            {
                ErrorMessage = "无效的 Base64 字符串。";
            }
        }
        
        private string GetImageType(string base64)
        {
            if (base64.StartsWith("iVBORw0KGgo")) return "image/png";
            if (base64.StartsWith("/9j/")) return "image/jpeg";
            if (base64.StartsWith("R0lGODlh")) return "image/gif";
            if (base64.StartsWith("PHN2Zy")) return "image/svg+xml";
            if (base64.StartsWith("Qk0")) return "image/bmp";
            if (base64.StartsWith("UklGR")) return "image/webp";
            
            // Add more checks for other image types if needed
            
            return string.Empty;
        }

        private static bool IsValidBase64(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var buffer = new Span<byte>(new byte[value.Length]);
            return Convert.TryFromBase64String(value, buffer, out _);
        }

        private static bool TryParseDataUrl(string input, out string dataUrl, out string base64Part, out string mimeType)
        {
            dataUrl = string.Empty;
            base64Part = string.Empty;
            mimeType = string.Empty;

            if (!input.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var commaIndex = input.IndexOf(',');
            if (commaIndex <= 5)
            {
                return false;
            }

            var metadata = input.Substring(5, commaIndex - 5);
            if (!metadata.Contains(";base64", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            mimeType = metadata.Split(';')[0];
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                mimeType = "image/*";
            }

            base64Part = input[(commaIndex + 1)..];
            dataUrl = $"data:{mimeType};base64,{base64Part}";
            return true;
        }
    }
}
