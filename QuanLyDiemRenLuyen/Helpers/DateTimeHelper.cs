using System;

namespace QuanLyDiemRenLuyen.Helpers
{
    /// <summary>
    /// Helper class để chuyển đổi DateTime an toàn từ database Oracle.
    /// Xử lý cả TIMESTAMP (trả về DateTime) và TIMESTAMP WITH TIME ZONE (trả về DateTimeOffset).
    /// </summary>
    public static class DateTimeHelper
    {
        /// <summary>
        /// Chuyển đổi giá trị từ database sang DateTime một cách an toàn.
        /// Xử lý cả DateTime và DateTimeOffset từ Oracle.
        /// </summary>
        /// <param name="value">Giá trị từ DataRow</param>
        /// <returns>Giá trị DateTime</returns>
        public static DateTime ToDateTime(object value)
        {
            if (value == null || value == DBNull.Value)
                return DateTime.MinValue;

            if (value is DateTimeOffset dto)
                return dto.DateTime;

            if (value is DateTime dt)
                return dt;

            // Fallback: thử Convert.ToDateTime
            return Convert.ToDateTime(value);
        }

        /// <summary>
        /// Chuyển đổi giá trị từ database sang DateTime nullable một cách an toàn.
        /// Trả về null nếu giá trị là DBNull.
        /// </summary>
        /// <param name="value">Giá trị từ DataRow</param>
        /// <returns>Giá trị DateTime nullable</returns>
        public static DateTime? ToNullableDateTime(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (value is DateTimeOffset dto)
                return dto.DateTime;

            if (value is DateTime dt)
                return dt;

            // Fallback: thử Convert.ToDateTime
            return Convert.ToDateTime(value);
        }
    }
}

