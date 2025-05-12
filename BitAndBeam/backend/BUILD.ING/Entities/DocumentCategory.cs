using System;
using System.Collections.Generic;

namespace BUILD.ING.Entities
{
    public class DocumentCategory
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public DateTime CreatedAt { get; set; }

        public DocumentCategory ParentCategory { get; set; }
        public ICollection<DocumentCategory> SubCategories { get; set; }
        public ICollection<Document> Documents { get; set; }
    }
}
