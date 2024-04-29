using Bislerium.Entities.Constants;

namespace Bislerium.Entities.Models;

public class Blog : BaseEntity<int>
{
    public string Title { get; set; }
    
    public string Body { get; set; }
    
    public string Location { get; set; }
    
    public string Reaction { get; set; }            // User is feeling excited, sad, nervous, happy, exotic and so on.
    
    public virtual ICollection<BlogImage> BlogImages { get; set; }
}