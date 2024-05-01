using System.ComponentModel.DataAnnotations.Schema;
using Bislerium.Entities.Constants;

namespace Bislerium.Entities.Models;

public class BlogImage : BaseEntity<int>
{
    public string ImageURL { get; set; }
    
    public int BlogId { get; set; }
    
    [ForeignKey("BlogId")]
    public virtual Blog Blog { get; set; }
}