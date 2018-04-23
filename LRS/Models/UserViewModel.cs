namespace bracken_lrs.Model
{
    public class UserViewModel : IViewModel
    {
        public string ToJson()
        {
            return @"{ ""name"": ""fred"" }";
        }
    }
}