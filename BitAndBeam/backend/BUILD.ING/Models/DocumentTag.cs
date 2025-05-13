using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BUILD.ING.Models
{
    public class DocumentTag
    {
        [Key]
        public int TagId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<DocumentTagRelation> DocumentTagRelations { get; set; }
    }
}