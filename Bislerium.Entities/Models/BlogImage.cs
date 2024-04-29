using System.ComponentModel.DataAnnotations.Schema;

namespace Bislerium.Entities.Models;

public class BlogImage
{
    public string ImageURL { get; set; }
    
    public int BlogId { get; set; }
    
    [ForeignKey("BlogId")]
    public virtual Blog Blog { get; set; }
}