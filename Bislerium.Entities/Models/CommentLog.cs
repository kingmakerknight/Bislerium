using System.ComponentModel.DataAnnotations.Schema;
using Bislerium.Entities.Constants;

namespace Bislerium.Entities.Models;

public class CommentLog : BaseEntity<int>
{
    public int CommentId { get; set; }
    
    public string Text { get; set; }
    
    [ForeignKey("CommentId")]
    public virtual Comment? Comment { get; set; }
}