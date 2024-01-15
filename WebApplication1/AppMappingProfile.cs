using AutoMapper;
using WebApplication1.News;
using WebApplication1.Users;

namespace WebApplication1
{
    public class AppMappingProfile: Profile
    {
        public AppMappingProfile() 
        {
            CreateMap<UserEntity, UserDTO>();
            CreateMap<NewsEntity, NewsDTO>();
        }
    }
}
