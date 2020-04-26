using AutoMapper;
using WebApi.Entities;
using WebApi.Models.Users;

namespace WebApi.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Auth, UserModel>();
            CreateMap<RegisterModel, Auth>();
            CreateMap<UpdateModel, Auth>();
        }
    }
}