namespace Hesapix.Models.Common
{
    /// <summary>
    /// Standart API yanıt wrapper'ı
    /// </summary>
    /// <typeparam name="T">Dönen data tipi</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Yanıt mesajı
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Dönen data
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Hata mesajları
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Başarılı response oluştur
        /// </summary>
        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Hata response oluştur
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }

        /// <summary>
        /// Validation hatası response oluştur
        /// </summary>
        public static ApiResponse<T> ValidationErrorResponse(List<string> errors)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Sayfalı data için response
    /// </summary>
    public class PagedResponse<T> : ApiResponse<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public static PagedResponse<T> CreatePagedResponse(
            T data,
            int pageNumber,
            int pageSize,
            int totalRecords)
        {
            return new PagedResponse<T>
            {
                Success = true,
                Message = "Success",
                Data = data,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
            };
        }
    }
}