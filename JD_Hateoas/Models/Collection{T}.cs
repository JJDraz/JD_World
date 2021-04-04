namespace JD_Hateoas.Models
{
    public class Collection<T> : Resource
    {
        public T[] Value { get; set; }
    }
}
