using System;
using System.Collections.Generic;

namespace BUILD.ING.Entities
{
    public class DocumentTag
    {
        public int TagId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<DocumentTagRelation> DocumentTagRelations { get; set; }
    }
}
