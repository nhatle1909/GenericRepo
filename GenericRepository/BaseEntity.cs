using System.Globalization;

namespace GenericRepository
{
    public class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CreatedAt { get; set; } = DateTime.Now.ToString("d", new CultureInfo("vi-VN"));
        public string UpdatedAt { get; set; } = DateTime.Now.ToString("d", new CultureInfo("vi-VN"));
        public string DeletedAt { get; set; } = DateTime.Now.ToString("d", new CultureInfo("vi-VN"));
        public bool IsDeleted { get; set; } = false;
    }
}
